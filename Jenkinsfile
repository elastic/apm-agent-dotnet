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
  }
  options {
    timeout(time: 1, unit: 'HOURS')
    buildDiscarder(logRotator(numToKeepStr: '20', artifactNumToKeepStr: '20', daysToKeepStr: '30'))
    timestamps()
    ansiColor('xterm')
    disableResume()
    durabilityHint('PERFORMANCE_OPTIMIZED')
    rateLimitBuilds(throttle: [count: 60, durationName: 'hour', userBoost: true])
    quietPeriod(10)
  }
  triggers {
    issueCommentTrigger('(?i).*(?:jenkins\\W+)?run\\W+(?:the\\W+)?tests(?:\\W+please)?.*')
  }
  parameters {
    booleanParam(name: 'Run_As_Master_Branch', defaultValue: false, description: 'Allow to run any steps on a PR, some steps normally only run on master branch.')
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
          }
        }
        stage('Parallel'){
          parallel{
              stage('Docker .NET Framework'){
                agent { label 'windows-2019-docker-immutable' }
                options { skipDefaultCheckout() }
                stages {
                  stage('Build - Docker MSBuild') {
                    steps {
                      withGithubNotify(context: 'Build MSBuild - Docker') {
                        cleanDir("${WORKSPACE}/${BASE_DIR}")
                        unstash 'source'
                        sleep time: 10, unit: 'MINUTES'
                        dir("${BASE_DIR}") {
                          catchError(message: 'Beta stage', buildResult: 'SUCCESS', stageResult: 'UNSTABLE') {
                            dotnetWindows(){
                              bat 'msbuild'
                            }
                          }
                        }
                      }
                    }
                  }
                }
              }
             
            }
          }
          stage('Release to AppVeyor') {
            options { skipDefaultCheckout() }
            when {
              beforeAgent true
              anyOf {
                branch 'master'
                expression { return params.Run_As_Master_Branch }
              }
            }
            steps {
              withGithubNotify(context: 'Release AppVeyor', tab: 'artifacts') {
                deleteDir()
                unstash 'source'
                dir("${BASE_DIR}"){
                  release('secret/apm-team/ci/elastic-observability-appveyor')
                }
              }
            }
            post{
              success {
                archiveArtifacts(allowEmptyArchive: true,
                  artifacts: "${BASE_DIR}/**/bin/Release/**/*.nupkg")
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
                expression { return params.Run_As_Master_Branch }
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
          stage('Integration Tests') {
            agent none
            when {
              beforeAgent true
              allOf {
                anyOf {
                  environment name: 'GIT_BUILD_CAUSE', value: 'pr'
                  expression { return !params.Run_As_Master_Branch }
                }
              }
            }
            steps {
              log(level: 'INFO', text: 'Launching Async ITs')
              // TODO: use commit rather than branch to be reproducible.
              build(job: env.ITS_PIPELINE, propagate: false, wait: false,
                    parameters: [string(name: 'AGENT_INTEGRATION_TEST', value: '.NET'),
                                 string(name: 'BUILD_OPTS', value: "--dotnet-agent-version ${env.GIT_BASE_COMMIT}"),
                                 string(name: 'GITHUB_CHECK_NAME', value: env.GITHUB_CHECK_ITS_NAME),
                                 string(name: 'GITHUB_CHECK_REPO', value: env.REPO),
                                 string(name: 'GITHUB_CHECK_SHA1', value: env.GIT_BASE_COMMIT)])
              githubNotify(context: "${env.GITHUB_CHECK_ITS_NAME}", description: "${env.GITHUB_CHECK_ITS_NAME} ...", status: 'PENDING', targetUrl: "${env.JENKINS_URL}search/?q=${env.ITS_PIPELINE.replaceAll('/','+')}")
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
  docker.image("${dockerTagName}").inside("-e HOME='${env.WORKSPACE}/${env.BASE_DIR}'"){
    body()
  }
}

def dotnetWindows(Closure body){
  def dockerTagName = 'docker.elastic.co/observability-ci/apm-agent-dotnet-windows:latest'
  bat label: 'Docker Build', script: "docker build --tag ${dockerTagName}  -m 2GB .ci\\docker\\buildtools-windows"
  docker.image("${dockerTagName}").inside(){
    body()
  }
}

def release(secret){
  dotnet(){
    sh(label: 'Release', script: '.ci/linux/release.sh')
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
