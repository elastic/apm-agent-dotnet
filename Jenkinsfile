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
            deleteDir()
            gitCheckout(basedir: "${BASE_DIR}", githubNotifyFirstTimeContributor: true)
            stash allowEmpty: true, name: 'source', useDefaultExcludes: false
          }
        }
        stage('Parallel'){
          parallel{
            stage('Linux'){
              options { skipDefaultCheckout() }
              environment {
                MSBUILDDEBUGPATH = "${env.WORKSPACE}"
              }
              stages{
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
                          sh label: 'Install test tools', script: '.ci/linux/test-tools.sh'
                          sh label: 'Build', script: '.ci/linux/build.sh'
                          sh label: 'Test & coverage', script: '.ci/linux/test.sh'
                          sh label: 'Convert Test Results to junit format', script: '.ci/linux/convert.sh'
                        }
                      }
                    }
                  }
                  post {
                    always {
                      sh label: 'debugging', script: 'find . -name *.pdb'
                      junit(allowEmptyResults: true,
                        keepLongStdio: true,
                        testResults: "${BASE_DIR}/**/junit-*.xml,${BASE_DIR}/target/**/TEST-*.xml")
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
                  VS_HOME = "C:\\Program Files (x86)\\Microsoft Visual Studio\\2017\\Enterprise"
                  PATH = "${env.DOTNET_ROOT};${env.DOTNET_ROOT}\\tools;${env.PATH};${env.HOME}\\bin;\"${env.VS_HOME}\\MSBuild\\15.0\\Bin\""
                  MSBUILDDEBUGPATH = "${env.WORKSPACE}"
                }
                stages{
                  /**
                  Checkout the code and stash it, to use it on other stages.
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
                          bat label: 'Build', script: '.ci/windows/msbuild.bat'
                          bat label: 'Test & coverage', script: '.ci/windows/test.bat'
                          powershell label: 'Convert Test Results to junit format', script: '.ci\\windows\\convert.ps1'
                        }
                      }
                    }
                    post {
                      always {
                        junit(allowEmptyResults: true,
                          keepLongStdio: true,
                          testResults: "${BASE_DIR}/**/junit-*.xml,${BASE_DIR}/target/**/TEST-*.xml")
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
                          bat label: 'Build', script: '.ci/windows/msbuild.bat'
                          bat label: 'Test IIS', script: '.ci/windows/test-iis.bat'
                          powershell label: 'Convert Test Results to junit format', script: '.ci\\windows\\convert.ps1'
                        }
                      }
                    }
                    post {
                      always {
                        junit(allowEmptyResults: true,
                          keepLongStdio: true,
                          testResults: "${BASE_DIR}/**/junit-*.xml,${BASE_DIR}/target/**/TEST-*.xml")
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
                  DOTNET_ROOT = "${env.WORKSPACE}\\dotnet"
                  VS_HOME = "C:\\Program Files (x86)\\Microsoft Visual Studio\\2017\\Enterprise"
                  PATH = "${env.DOTNET_ROOT};${env.DOTNET_ROOT}\\tools;${env.PATH};${env.HOME}\\bin;\"${env.VS_HOME}\\MSBuild\\15.0\\Bin\""
                  MSBUILDDEBUGPATH = "${env.WORKSPACE}"
                }
                stages{
                  /**
                  Checkout the code and stash it, to use it on other stages.
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
                        cleanDir("${WORKSPACE}/${BASE_DIR}")
                        unstash 'source'
                        dir("${BASE_DIR}"){
                          bat '.ci/windows/dotnet.bat'
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
                          bat label: 'Build', script: '.ci/windows/dotnet.bat'
                          bat label: 'Test & coverage', script: '.ci/windows/test.bat'
                          powershell label: 'Convert Test Results to junit format', script: '.ci\\windows\\convert.ps1'
                        }
                      }
                    }
                    post {
                      always {
                        junit(allowEmptyResults: true,
                          keepLongStdio: true,
                          testResults: "${BASE_DIR}/**/junit-*.xml,${BASE_DIR}/target/**/TEST-*.xml")
                      }
                      unsuccessful {
                        archiveArtifacts(allowEmptyArchive: true,
                          artifacts: "${MSBUILDDEBUGPATH}/**/MSBuild_*.failure.txt")
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
            }
          }
          stage('Release to AppVeyor') {
            options { skipDefaultCheckout() }
            steps {
              withGithubNotify(context: 'Release AppVeyor', tab: 'artifacts') {
                deleteDir()
                unstash 'source'
                dir("${BASE_DIR}"){
                  release('secret/apm-team/ci/elastic-observability-appveyor')
                }
              }
            }
          }
          stage('Release to NuGet') {
            options { skipDefaultCheckout() }
            steps {
              withGithubNotify(context: 'Release NuGet', tab: 'artifacts') {
                deleteDir()
                unstash 'source'
                dir("${BASE_DIR}"){
                  release('secret/apm-team/ci/elastic-observability-nuget')
                }
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
  def home = "/tmp"
  def dotnetRoot = "/${home}/.dotnet"
  def path = "/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin:/${home}/bin:${dotnetRoot}:${dotnetRoot}/bin:${dotnetRoot}/tools"
  docker.image('mcr.microsoft.com/dotnet/core/sdk:2.2').inside("-e HOME='${home}' -e PATH='${path}'"){
    body()
  }
}

def release(secret){
  dotnet(){
    echo 'pre-release'
    sh 'touch release'
    def repo = getVaultSecret(secret: secret)
    wrap([$class: 'MaskPasswordsBuildWrapper', varPasswordPairs: [
      [var: 'REPO_API_KEY', password: repo.apiKey],
      [var: 'REPO_API_URL', password: repo.url],
      ]]) {
        echo 'release'
        sh 'touch release'
    }
  }
}
