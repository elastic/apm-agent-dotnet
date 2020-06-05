#!/usr/bin/env groovy

@Library('apm@current') _

pipeline {
  agent { label 'linux && immutable' }
  environment {
    REPO = 'apm-agent-dotnet'
    // keep it short to avoid the 248 characters PATH limit in Windows
    BASE_DIR = "apm-agent-dotnet"
    NOTIFY_TO = credentials('notify-to')
    JOB_GCS_BUCKET = credentials('gcs-bucket')
    CODECOV_SECRET = 'secret/apm-team/ci/apm-agent-dotnet-codecov'
    GITHUB_CHECK_ITS_NAME = 'Integration Tests'
    ITS_PIPELINE = 'apm-integration-tests-selector-mbp/master'
    OPBEANS_REPO = 'opbeans-dotnet'
    BENCHMARK_SECRET  = 'secret/apm-team/ci/benchmark-cloud'
  }
  options {
    timeout(time: 2, unit: 'HOURS')
    buildDiscarder(logRotator(numToKeepStr: '20', artifactNumToKeepStr: '20', daysToKeepStr: '30'))
    timestamps()
    ansiColor('xterm')
    disableResume()
    durabilityHint('PERFORMANCE_OPTIMIZED')
    rateLimitBuilds(throttle: [count: 60, durationName: 'hour', userBoost: true])
    quietPeriod(10)
  }
  triggers {
    issueCommentTrigger('(?i).*(?:jenkins\\W+)?run\\W+(?:the\\W+)?(?:benchmark\\W+)?tests(?:\\W+please)?.*')
  }
  parameters {
    booleanParam(name: 'Run_As_Master_Branch', defaultValue: false, description: 'Allow to run any steps on a PR, some steps normally only run on master branch.')
  }
  stages {
    stage('Initializing'){
      options { timeout(time: 75, unit: 'MINUTES') }
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
                  '^test/Elastic.Apm.PerfTests/.*'
                ]
                env.BENCHMARK_UPDATED = isGitRegionMatch(patterns: patternList)
              }
            }
          }
        }
        stage('Parallel'){
          when {
            beforeAgent true
            expression { return env.ONLY_DOCS == "false" }
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
                // Disable until https://github.com/elastic/apm-agent-dotnet/issues/563
                // stage('CodeStyleCheck') {
                //   steps {
                //     withGithubNotify(context: 'CodeStyle check') {
                //       deleteDir()
                //       unstash 'source'
                //       dir("${BASE_DIR}"){
                //         dotnet(){
                //           sh label: 'Install and run dotnet/format', script: '.ci/linux/codestyle.sh'
                //         }
                //       }
                //     }
                //   }
                // }
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
                        }
                      }
                    }
                  }
                  post {
                    unsuccessful {
                      archiveArtifacts(allowEmptyArchive: true,
                        artifacts: "${MSBUILDDEBUGPATH}/**/MSBuild_*.failure.txt")
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
                      dir("${BASE_DIR}"){
                        dotnet(){
                          sh label: 'Test & coverage', script: '.ci/linux/test.sh'
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
            stage('Windows .NET Framework'){
              agent { label 'windows-2019-immutable' }
              options { skipDefaultCheckout() }
              environment {
                HOME = "${env.WORKSPACE}"
                DOTNET_ROOT = "${env.WORKSPACE}\\dotnet"
                PATH = "${env.DOTNET_ROOT};${env.DOTNET_ROOT}\\tools;${env.PATH};${env.HOME}\\bin"
                MSBUILDDEBUGPATH = "${env.WORKSPACE}"
              }
              stages{
                /**
                Install the required tools
                */
                stage('Install tools') {
                  steps {
                    cleanDir("${WORKSPACE}/*")
                    unstash 'source'
                    dir("${HOME}"){
                      powershell label: 'Install tools', script: "${BASE_DIR}\\.ci\\windows\\tools.ps1"
                      powershell label: 'Install msbuild tools', script: "${BASE_DIR}\\.ci\\windows\\msbuild-tools.ps1"
                    }
                  }
                }
                /**
                Build the project from code..
                */
                stage('Build - MSBuild') {
                  steps {
                    withGithubNotify(context: 'Build MSBuild - Windows') {
                      cleanDir("${WORKSPACE}/${BASE_DIR}")
                      unstash 'source'
                      dir("${BASE_DIR}"){
                        bat '.ci/windows/msbuild.bat'
                      }
                    }
                  }
                  post {
                    unsuccessful {
                      archiveArtifacts(allowEmptyArchive: true,
                        artifacts: "${MSBUILDDEBUGPATH}/**/MSBuild_*.failure.txt")
                    }
                  }
                }
                /**
                Execute unit tests.
                */
                stage('Test') {
                  steps {
                    withGithubNotify(context: 'Test MSBuild - Windows', tab: 'tests') {
                      cleanDir("${WORKSPACE}/${BASE_DIR}")
                      unstash 'source'
                      dir("${BASE_DIR}"){
                        powershell label: 'Install test tools', script: '.ci\\windows\\test-tools.ps1'
                        bat label: 'Prepare solution', script: '.ci/windows/prepare-test.bat'
                        bat label: 'Build', script: '.ci/windows/msbuild.bat'
                        bat label: 'Test & coverage', script: '.ci/windows/testnet461.bat'
                      }
                    }
                  }
                  post {
                    always {
                      reportTests()
                    }
                    unsuccessful {
                      archiveArtifacts(allowEmptyArchive: true,
                        artifacts: "${MSBUILDDEBUGPATH}/**/MSBuild_*.failure.txt")
                    }
                  }
                }
                /**
                Execute IIS tests.
                */
                stage('IIS Tests') {
                  steps {
                    withGithubNotify(context: 'IIS Tests', tab: 'tests') {
                      cleanDir("${WORKSPACE}/${BASE_DIR}")
                      unstash 'source'
                      dir("${BASE_DIR}"){
                        powershell label: 'Install test tools', script: '.ci\\windows\\test-tools.ps1'
                        bat label: 'Build', script: '.ci/windows/msbuild.bat'
                        bat label: 'Test IIS', script: '.ci/windows/test-iis.bat'
                      }
                    }
                  }
                  post {
                    always {
                      reportTests()
                    }
                  }
                }
              }
              post {
                always {
                  cleanWs(disableDeferredWipeout: true, notFailBuild: true)
                }
              }
            }
            stage('Windows .NET Core'){
              agent { label 'windows-2019-immutable' }
              options { skipDefaultCheckout() }
              environment {
                HOME = "${env.WORKSPACE}"
                DOTNET_ROOT = "C:\\Program Files\\dotnet"
                PATH = "${env.DOTNET_ROOT};${env.DOTNET_ROOT}\\tools;${env.PATH};${env.HOME}\\bin"
                MSBUILDDEBUGPATH = "${env.WORKSPACE}"
              }
              stages{
                /**
                Install the required tools
                */
                stage('Install tools') {
                  steps {
                    cleanDir("${WORKSPACE}/*")
                    unstash 'source'
                    dir("${HOME}"){
                      powershell label: 'Install tools', script: "${BASE_DIR}\\.ci\\windows\\tools.ps1"
                    }
                  }
                }
                /**
                Build the project from code..
                */
                stage('Build - dotnet') {
                  steps {
                    withGithubNotify(context: 'Build dotnet - Windows') {
                      retry(3) {
                        cleanDir("${WORKSPACE}/${BASE_DIR}")
                        unstash 'source'
                        dir("${BASE_DIR}"){
                          bat label: 'Build', script: '.ci/windows/dotnet.bat'
                        }
                      }
                    }
                  }
                  post {
                    unsuccessful {
                      archiveArtifacts(allowEmptyArchive: true,
                        artifacts: "${MSBUILDDEBUGPATH}/**/MSBuild_*.failure.txt")
                    }
                  }
                }
                /**
                Execute unit tests.
                */
                stage('Test') {
                  steps {
                    withGithubNotify(context: 'Test dotnet - Windows', tab: 'tests') {
                      cleanDir("${WORKSPACE}/${BASE_DIR}")
                      unstash 'source'
                      dir("${BASE_DIR}"){
                        powershell label: 'Install test tools', script: '.ci\\windows\\test-tools.ps1'
                        retry(3) {
                          bat label: 'Build', script: '.ci/windows/dotnet.bat'
                        }
                        bat label: 'Test & coverage', script: '.ci/windows/test.bat'
                      }
                    }
                  }
                  post {
                    always {
                      reportTests()
                    }
                    unsuccessful {
                      archiveArtifacts(allowEmptyArchive: true, artifacts: "${MSBUILDDEBUGPATH}/**/MSBuild_*.failure.txt")
                    }
                  }
                }
              }
              post {
                always {
                  cleanWs(disableDeferredWipeout: true, notFailBuild: true)
                }
              }
            }
            stage('Integration Tests') {
              agent none
              when {
                anyOf {
                  changeRequest()
                  expression { return !params.Run_As_Master_Branch }
                }
              }
              steps {
                build(job: env.ITS_PIPELINE, propagate: false, wait: false,
                      parameters: [string(name: 'INTEGRATION_TEST', value: '.NET'),
                                    string(name: 'BUILD_OPTS', value: "--dotnet-agent-version ${env.GIT_BASE_COMMIT} --opbeans-dotnet-agent-branch ${env.GIT_BASE_COMMIT}"),
                                    string(name: 'GITHUB_CHECK_NAME', value: env.GITHUB_CHECK_ITS_NAME),
                                    string(name: 'GITHUB_CHECK_REPO', value: env.REPO),
                                    string(name: 'GITHUB_CHECK_SHA1', value: env.GIT_BASE_COMMIT)])
                githubNotify(context: "${env.GITHUB_CHECK_ITS_NAME}", description: "${env.GITHUB_CHECK_ITS_NAME} ...", status: 'PENDING', targetUrl: "${env.JENKINS_URL}search/?q=${env.ITS_PIPELINE.replaceAll('/','+')}")
              }
            }
            stage('Benchmarks') {
              agent { label 'metal' }
              environment {
                REPORT_FILE = 'apm-agent-benchmark-results.json'
                HOME = "${env.WORKSPACE}"
              }
              when {
                beforeAgent true
                allOf {
                  anyOf {
                    branch 'master'
                    tag pattern: 'v\\d+\\.\\d+\\.\\d+.*', comparator: 'REGEXP'
                    expression { return params.Run_As_Master_Branch }
                    expression { return env.BENCHMARK_UPDATED != "false" }
                    expression { return env.GITHUB_COMMENT?.contains('benchmark tests') }
                  }
                  expression { return env.ONLY_DOCS == "false" }
                }
              }
              options {
                warnError('Benchmark failed')
                timeout(time: 1, unit: 'HOURS')
              }
              steps {
                withGithubNotify(context: 'Benchmarks') {
                  deleteDir()
                  unstash 'source'
                  dir("${BASE_DIR}") {
                    script {
                      sendBenchmarks.prepareAndRun(secret: env.BENCHMARK_SECRET, url_var: 'ES_URL',
                                                   user_var: 'ES_USER', pass_var: 'ES_PASS') {
                        sh '.ci/linux/benchmark.sh'
                      }
                    }
                  }
                }
              }
              post {
                always {
                  catchError(message: 'deleteDir failed', buildResult: 'SUCCESS', stageResult: 'UNSTABLE') {
                    deleteDir()
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
          branch 'master'
          expression { return params.Run_As_Master_Branch }
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
            artifacts: "${BASE_DIR}/**/bin/Release/**/*.nupkg")
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
        tag pattern: '\\d+\\.\\d+\\.\\d+(-(alpha|beta|rc)\\d*)?', comparator: 'REGEXP'
      }
      stages {
        stage('Notify') {
          steps {
            emailext subject: '[apm-agent-dotnet] Release ready to be pushed',
                      to: "${NOTIFY_TO}",
                      body: "Please go to ${env.BUILD_URL}input to approve or reject within 12 hours."
          }
        }
        stage('Release to NuGet') {
          input {
            message 'Should we release a new version?'
            ok 'Yes, we should.'
          }
          steps {
            deleteDir()
            unstash 'source'
            dir("${BASE_DIR}") {
              release(secret: 'secret/apm-team/ci/elastic-observability-nuget')
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
          tag pattern: '\\d+\\.\\d+\\.\\d+', comparator: 'REGEXP'
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
              git credentialsId: 'f6c7695a-671e-4f4f-a331-acdce44ff9ba',
                  url: "git@github.com:elastic/${OPBEANS_REPO}.git"
              sh script: ".ci/bump-version.sh ${env.BRANCH_NAME}", label: 'Bump version'
              // The opbeans pipeline will trigger a release for the master branch
              gitPush()
              // The opbeans pipeline will trigger a release for the release tag
              gitCreateTag(tag: "${env.BRANCH_NAME}")
            }
          }
        }
      }
    }
  }
  post {
    cleanup {
      notifyBuildResult()
    }
  }
}

def cleanDir(path){
  powershell label: "Clean ${path}", script: "Remove-Item -Recurse -Force ${path}"
}

def dotnet(Closure body){
  def dockerTagName = 'docker.elastic.co/observability-ci/apm-agent-dotnet-sdk-linux:latest'
  sh label: 'Docker build', script: "docker build --tag ${dockerTagName} .ci/docker/sdk-linux"
  docker.image("${dockerTagName}").inside("-e HOME='${env.WORKSPACE}/${env.BASE_DIR}' -v /var/run/docker.sock:/var/run/docker.sock"){
    body()
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
        sh(label: 'Deploy', script: ".ci/linux/deploy.sh ${REPO_API_KEY} ${REPO_API_URL}")
      }
    }
  }
}

def reportTests() {
  dir("${BASE_DIR}"){
    archiveArtifacts(allowEmptyArchive: true, artifacts: 'target/diag-*.log,test/**/junit-*.xml,target/**/*coverage.cobertura.xml')
    junit(allowEmptyResults: true, keepLongStdio: true, testResults: 'test/**/junit-*.xml')
  }
}
