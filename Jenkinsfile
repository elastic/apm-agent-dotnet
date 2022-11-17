#!/usr/bin/env groovy

@Library('apm@current') _

pipeline {
  agent { label 'linux && immutable && docker' }
  environment {
    REPO = 'apm-agent-dotnet'
    // keep it short to avoid the 248 characters PATH limit in Windows
    BASE_DIR = "apm-agent-dotnet"
    NOTIFY_TO = credentials('notify-to')
    JOB_GCS_BUCKET = credentials('gcs-bucket')
    CODECOV_SECRET = 'secret/apm-team/ci/apm-agent-dotnet-codecov'
    OPBEANS_REPO = 'opbeans-dotnet'
    BENCHMARK_SECRET  = 'secret/apm-team/ci/benchmark-cloud'
    SLACK_CHANNEL = '#apm-agent-dotnet'
    AZURE_RESOURCE_GROUP_PREFIX = "ci-dotnet-${env.BUILD_ID}"
  }
  options {
    timeout(time: 4, unit: 'HOURS')
    buildDiscarder(logRotator(numToKeepStr: '20', artifactNumToKeepStr: '10', daysToKeepStr: '30'))
    timestamps()
    ansiColor('xterm')
    disableResume()
    durabilityHint('PERFORMANCE_OPTIMIZED')
    rateLimitBuilds(throttle: [count: 60, durationName: 'hour', userBoost: true])
    quietPeriod(10)
  }
  triggers {
    issueCommentTrigger("(${obltGitHubComments()}|^run benchmark tests)")
  }
  parameters {
    booleanParam(name: 'Run_As_Main_Branch', defaultValue: false, description: 'Allow to run any steps on a PR, some steps normally only run on main branch.')
  }
  stages {
    stage('Initializing'){
      stages{
        stage('Checkout') {
          options { skipDefaultCheckout() }
          steps {
            pipelineManager([ cancelPreviousRunningBuilds: [ when: 'PR' ] ])
            deleteDir()
            gitCheckout(basedir: "${BASE_DIR}", githubNotifyFirstTimeContributor: true)
            stash allowEmpty: true, name: 'source', useDefaultExcludes: false
            script {
              dir("${BASE_DIR}"){
                // Skip all the stages for PRs with changes in the docs only
                env.ONLY_DOCS = isGitRegionMatch(patterns: [ '^docs/.*' ], shouldMatchAll: true)

                 // Look for changes related to the benchmark, if so then set the env variable.
                def patternList = [
                  '^test/Elastic.Apm.Benchmarks/.*'
                ]
                env.BENCHMARK_UPDATED = isGitRegionMatch(patterns: patternList)
              }
            }
          }
        }
        stage('Parallel'){
          when {
            beforeAgent true
            allOf {
              expression { return env.ONLY_DOCS == "false" }
              anyOf {
                changeRequest()
                branch '**/*'
              }
            }
          }
          parallel{
            stage('Linux'){
              options { skipDefaultCheckout() }
              environment {
                MSBUILDDEBUGPATH = "${env.WORKSPACE}"
              }
              /**
              Make sure there are no code style violation in the repo.
              */
              stages{
              //   Disable until https://github.com/elastic/apm-agent-dotnet/issues/563
              //   stage('CodeStyleCheck') {
              //     steps {
              //       withGithubNotify(context: 'CodeStyle check') {
              //         deleteDir()
              //         unstash 'source'
              //         dir("${BASE_DIR}"){
              //           dotnet(){
              //             sh label: 'Install and run dotnet/format', script: '.ci/linux/codestyle.sh'
              //           }
              //         }
              //       }
              //     }
              //   }
                /**
                Build the project from code..
                */
                stage('Build') {
                  steps {
                    withGithubNotify(context: 'Build - Linux') {
                      deleteDir()
                      unstash 'source'
                      dir("${BASE_DIR}"){
                        dotnet(){
                          sh '.ci/linux/build.sh'
                          // build nuget packages and profiler
                          sh(label: 'Package', script: '.ci/linux/release.sh true')
                          sh label: 'Rustup', script: 'rustup default 1.59.0'
                          sh label: 'Cargo make', script: 'cargo install --force cargo-make'
                          sh(label: 'Build profiler', script: './build.sh profiler-zip')
                        }
                      }
                    }
                  }
                  post {
                    unsuccessful {
                      archiveArtifacts(allowEmptyArchive: true,
                        artifacts: "${MSBUILDDEBUGPATH}/**/MSBuild_*.failure.txt")
                    }
                    success {
                      archiveArtifacts(allowEmptyArchive: true, artifacts: "${BASE_DIR}/build/output/_packages/*.nupkg,${BASE_DIR}/build/output/*.zip")
                    }
                  }
                }
                /**
                Execute unit tests.
                */
                stage('Test') {
                  steps {
                    withGithubNotify(context: 'Test - Linux', tab: 'tests') {
                      deleteDir()
                      unstash 'source'
                      filebeat(output: "docker.log"){
                        dir("${BASE_DIR}"){
                          testTools(){
                            dotnet(){
                              sh label: 'Test & coverage', script: '.ci/linux/test.sh'
                            }
                          }
                        }
                      }
                    }
                  }
                  post {
                    always {
                      reportTests()
                      publishCoverage(adapters: [coberturaAdapter("${BASE_DIR}/target/**/*coverage.cobertura.xml")],
                                      sourceFileResolver: sourceFiles('STORE_ALL_BUILD'))
                      codecov(repo: env.REPO, basedir: "${BASE_DIR}", secret: "${CODECOV_SECRET}")
                    }
                    unsuccessful {
                      archiveArtifacts(allowEmptyArchive: true,
                        artifacts: "${MSBUILDDEBUGPATH}/**/MSBuild_*.failure.txt")
                    }
                  }
                }
              }
            }
          }
        }
      }
    }
    stage('Release to feedz.io') {
      options { skipDefaultCheckout() }
      when {
        beforeAgent true
        anyOf {
          branch 'main'
          expression { return params.Run_As_Main_Branch }
        }
      }
      steps {
        deleteDir()
        unstash 'source'
        dir("${BASE_DIR}"){
          release(secret: 'secret/apm-team/ci/elastic-observability-feedz.io', withSuffix: true)
        }
      }
      post{
        success {
          archiveArtifacts(allowEmptyArchive: true,
            artifacts: "${BASE_DIR}/build/output/_packages/*.nupkg")
        }
      }
    }
    stage('Release') {
      options {
        skipDefaultCheckout()
      }
      when {
        beforeInput true
        beforeAgent true
        // Tagged release events ONLY
        tag pattern: 'v\\d+\\.\\d+\\.\\d+(-(alpha|beta|rc)\\d*)?', comparator: 'REGEXP'
      }
      stages {
        stage('Notify') {
          steps {
            notifyStatus(slackStatus: 'warning', subject: "[${env.REPO}] Release ready to be pushed",
                         body: "Please (<${env.BUILD_URL}input|approve>) it or reject within 12 hours.\n Changes: ${env.TAG_NAME}")
          }
        }
        stage('Release to NuGet') {
          input {
            message 'Should we release a new version?'
            ok 'Yes, we should.'
          }
          environment {
            RELEASE_URL_MESSAGE = "(<https://github.com/elastic/apm-agent-dotnet/releases/tag/${env.TAG_NAME}|${env.TAG_NAME}>)"
          }
          steps {
            deleteDir()
            unstash 'source'
            dir("${BASE_DIR}") {
              release(secret: 'secret/apm-team/ci/elastic-observability-nuget')
            }
          }
          post {
            failure {
              notifyStatus(slackStatus: 'danger', subject: "[${env.REPO}] Release *${env.TAG_NAME}* failed", body: "Build: (<${env.RUN_DISPLAY_URL}|here>)")
            }
            success {
              notifyStatus(slackStatus: 'good', subject: "[${env.REPO}] Release *${env.TAG_NAME}* published", body: "Build: (<${env.RUN_DISPLAY_URL}|here>)\nRelease URL: ${env.RELEASE_URL_MESSAGE}")
            }
          }
        }
      }
    }
    stage('AfterRelease') {
      options {
        skipDefaultCheckout()
      }
      when {
        anyOf {
          tag pattern: 'v\\d+\\.\\d+\\.\\d+', comparator: 'REGEXP'
        }
      }
      stages {
        stage('Opbeans') {
          environment {
            REPO_NAME = "${OPBEANS_REPO}"
          }
          steps {
            deleteDir()
            dir("${OPBEANS_REPO}"){
              git(credentialsId: 'f6c7695a-671e-4f4f-a331-acdce44ff9ba',
                  url: "git@github.com:elastic/${OPBEANS_REPO}.git",
                  branch: 'main')
              sh script: ".ci/bump-version.sh ${env.BRANCH_NAME.replaceAll('^v', '')}", label: 'Bump version'
              // The opbeans pipeline will trigger a release for the main branch
              gitPush()
              // The opbeans pipeline will trigger a release for the release tag with the format v<major>.<minor>.<patch>
              gitCreateTag(tag: "${env.BRANCH_NAME}")
            }
          }
        }
      }
    }
  }
  post {
    cleanup {
      cleanupAzureResources()
      notifyBuildResult()
    }
  }
}

def cleanDir(path){
  powershell label: "Clean ${path}", script: "Remove-Item -Recurse -Force ${path}"
}

def dotnet(Closure body){

  def homePath = "${env.WORKSPACE}/${env.BASE_DIR}"
  withEnv([
    "HOME=${homePath}",
    "DOTNET_ROOT=${homePath}/.dotnet",
    "PATH+DOTNET=${homePath}/.dotnet/tools:${homePath}/.dotnet",
    "PATH=${homePath}/azure-functions-cli:${PATH}"
    ]){
    sh(label: 'Install dotnet SDK', script: """
    mkdir -p \${DOTNET_ROOT}
    # Download .Net SDK installer script
    curl -s -O -L https://dot.net/v1/dotnet-install.sh
    chmod ugo+rx dotnet-install.sh

    # Install .Net SDKs
    ./dotnet-install.sh --install-dir "\${DOTNET_ROOT}" -version '3.1.100'
    ./dotnet-install.sh --install-dir "\${DOTNET_ROOT}" -version '5.0.100'
    ./dotnet-install.sh --install-dir "\${DOTNET_ROOT}" -version '6.0.100'
    """)
    withAzureCredentials(path: "${homePath}", credentialsFile: '.credentials.json') {
      withTerraformEnv(version: '0.15.3'){
        body()
      }
    }
  }
}

def testTools(Closure body){
  def homePath = "${env.WORKSPACE}/${env.BASE_DIR}"
  withEnv([
    "PATH=${homePath}/azure-functions-cli:${PATH}"
    ]){
    sh(label: 'Install Azure Functions Core Tools', script: """
      # See: https://github.com/Azure/azure-functions-core-tools#other-linux-distributions

    # Get the URL for the latest v4 linux-64 artifact
    latest_v4_release_url=\$(curl -s https://api.github.com/repos/Azure/azure-functions-core-tools/releases \
      | jq -r '.[].assets[].browser_download_url' \
      | grep 'Azure.Functions.Cli.linux-x64.4.*zip\$' \
      | head -n 1)
    
    # Preserve only the filename component of the URL
    latest_v4_release_file=\${latest_v4_release_url##*/}

    # Download the artifact
    curl -sLO "\${latest_v4_release_url}"

    # Unzip the artifact to ./azure-functions-cli
    unzip -d azure-functions-cli "\${latest_v4_release_file}"

    # Make required executables ... executable.
    chmod +x ./azure-functions-cli/func
    chmod +x ./azure-functions-cli/gozip
    """)
    body()
  }
}

def cleanupAzureResources(){
    def props = getVaultSecret(secret: 'secret/apm-team/ci/apm-agent-dotnet-azure')
    def authObj = props?.data
    def dockerCmd = "docker run --rm -i -v \$(pwd)/.azure:/root/.azure mcr.microsoft.com/azure-cli:latest"
    withEnvMask(vars: [
        [var: 'AZ_CLIENT_ID', password: "${authObj.client_id}"],
        [var: 'AZ_CLIENT_SECRET', password: "${authObj.client_secret}"],
        [var: 'AZ_SUBSCRIPTION_ID', password: "${authObj.subscription_id}"],
        [var: 'AZ_TENANT_ID', password: "${authObj.tenant_id}"]
    ]) {
        dir("${BASE_DIR}"){
            cmd label: "Create storage for Azure authentication",
                script: "mkdir -p ${BASE_DIR}/.azure || true"
            cmd label: "Logging into Azure",
                script: "${dockerCmd} " + 'az login --service-principal --username ${AZ_CLIENT_ID} --password ${AZ_CLIENT_SECRET} --tenant ${AZ_TENANT_ID}'
            cmd label: "Setting Azure subscription",
                script: "${dockerCmd} " + 'az account set --subscription ${AZ_SUBSCRIPTION_ID}'
            cmd label: "Checking and removing any Azure related resource groups",
                script: "for group in `${dockerCmd} az group list --query \"[?name | starts_with(@,'${AZURE_RESOURCE_GROUP_PREFIX}')]\" --out json|jq .[].name --raw-output`;do ${dockerCmd} az group delete --name \$group --no-wait --yes;done"
        }
    }
}

def release(Map args = [:]){
  def secret = args.secret
  def withSuffix = args.get('withSuffix', false)
  dotnet(){
    sh(label: 'Release', script: ".ci/linux/release.sh ${withSuffix}")
    def repo = getVaultSecret(secret: secret)
    wrap([$class: 'MaskPasswordsBuildWrapper', varPasswordPairs: [
      [var: 'REPO_API_KEY', password: repo.data.apiKey],
      [var: 'REPO_API_URL', password: repo.data.url],
      ]]) {
      withEnv(["REPO_API_KEY=${repo.data.apiKey}", "REPO_API_URL=${repo.data.url}"]) {
        sh(label: 'Deploy', script: '.ci/linux/deploy.sh ${REPO_API_KEY} ${REPO_API_URL}')
      }
    }
  }
}

def reportTests() {
  dir("${BASE_DIR}"){
    archiveArtifacts(allowEmptyArchive: true, artifacts: 'target/diag-*.log,test/**/junit-*.xml,target/**/Sequence_*.xml,target/**/testhost*.dmp')
    junit(allowEmptyResults: true, keepLongStdio: true, testResults: 'test/**/junit-*.xml')
  }
}

def notifyStatus(def args = [:]) {
  releaseNotification(slackChannel: "${env.SLACK_CHANNEL}",
                      slackColor: args.slackStatus,
                      slackCredentialsId: 'jenkins-slack-integration-token',
                      to: "${env.NOTIFY_TO}",
                      subject: args.subject,
                      body: args.body)
}
