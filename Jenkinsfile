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
            stage('Windows .NET Framework'){
              agent { label 'windows-2019-test-immutable' }
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
                        powershell label: 'Convert Test Results to junit format', script: '.ci\\windows\\convert.ps1'
                      }
                    }
                  }
                  post {
                    always {
                      archiveArtifacts(allowEmptyArchive: true, artifacts: "${BASE_DIR}/target/diag.log,${BASE_DIR}/target/TestResults.xml")
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
            stage('msbuild-tools-16.4.1.0'){
              agent { label 'windows-2019-test-immutable' }
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
                      powershell label: 'Install msbuild tools', script: "${BASE_DIR}\\.ci\\windows\\msbuild-tools-16.4.1.0.ps1"
                    }
                  }
                }
                /**
                Build the project from code..
                */
                stage('Build - MSBuild') {
                  steps {
                      cleanDir("${WORKSPACE}/${BASE_DIR}")
                      unstash 'source'
                      dir("${BASE_DIR}"){
                        bat '.ci/windows/msbuild.bat'
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
                      cleanDir("${WORKSPACE}/${BASE_DIR}")
                      unstash 'source'
                      dir("${BASE_DIR}"){
                        powershell label: 'Install test tools', script: '.ci\\windows\\test-tools.ps1'
                        bat label: 'Prepare solution', script: '.ci/windows/prepare-test.bat'
                        bat label: 'Build', script: '.ci/windows/msbuild.bat'
                        bat label: 'Test & coverage', script: '.ci/windows/testnet461.bat'
                        powershell label: 'Convert Test Results to junit format', script: '.ci\\windows\\convert.ps1'
                      }
                  }
                  post {
                    always {
                      archiveArtifacts(allowEmptyArchive: true, artifacts: "${BASE_DIR}/target/diag.log,${BASE_DIR}/target/TestResults.xml")
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
                      cleanDir("${WORKSPACE}/${BASE_DIR}")
                      unstash 'source'
                      dir("${BASE_DIR}"){
                        bat label: 'Build', script: '.ci/windows/msbuild.bat'
                        bat label: 'Test IIS', script: '.ci/windows/test-iis.bat'
                        powershell label: 'Convert Test Results to junit format', script: '.ci\\windows\\convert.ps1'
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
            stage('msbuild-tools-16.4.2.0'){
              agent { label 'windows-2019-test-immutable' }
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
                      powershell label: 'Install msbuild tools', script: "${BASE_DIR}\\.ci\\windows\\msbuild-tools-16.4.2.0.ps1"
                    }
                  }
                }
                /**
                Build the project from code..
                */
                stage('Build - MSBuild') {
                  steps {
                      cleanDir("${WORKSPACE}/${BASE_DIR}")
                      unstash 'source'
                      dir("${BASE_DIR}"){
                        bat '.ci/windows/msbuild.bat'
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
                      cleanDir("${WORKSPACE}/${BASE_DIR}")
                      unstash 'source'
                      dir("${BASE_DIR}"){
                        powershell label: 'Install test tools', script: '.ci\\windows\\test-tools.ps1'
                        bat label: 'Prepare solution', script: '.ci/windows/prepare-test.bat'
                        bat label: 'Build', script: '.ci/windows/msbuild.bat'
                        bat label: 'Test & coverage', script: '.ci/windows/testnet461.bat'
                        powershell label: 'Convert Test Results to junit format', script: '.ci\\windows\\convert.ps1'
                      }
                  }
                  post {
                    always {
                      archiveArtifacts(allowEmptyArchive: true, artifacts: "${BASE_DIR}/target/diag.log,${BASE_DIR}/target/TestResults.xml")
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
                      cleanDir("${WORKSPACE}/${BASE_DIR}")
                      unstash 'source'
                      dir("${BASE_DIR}"){
                        bat label: 'Build', script: '.ci/windows/msbuild.bat'
                        bat label: 'Test IIS', script: '.ci/windows/test-iis.bat'
                        powershell label: 'Convert Test Results to junit format', script: '.ci\\windows\\convert.ps1'
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
            stage('msbuild-tools-16.4.3.0'){
              agent { label 'windows-2019-test-immutable' }
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
                      powershell label: 'Install msbuild tools', script: "${BASE_DIR}\\.ci\\windows\\msbuild-tools-16.4.3.0.ps1"
                    }
                  }
                }
                /**
                Build the project from code..
                */
                stage('Build - MSBuild') {
                  steps {
                      cleanDir("${WORKSPACE}/${BASE_DIR}")
                      unstash 'source'
                      dir("${BASE_DIR}"){
                        bat '.ci/windows/msbuild.bat'
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
                      cleanDir("${WORKSPACE}/${BASE_DIR}")
                      unstash 'source'
                      dir("${BASE_DIR}"){
                        powershell label: 'Install test tools', script: '.ci\\windows\\test-tools.ps1'
                        bat label: 'Prepare solution', script: '.ci/windows/prepare-test.bat'
                        bat label: 'Build', script: '.ci/windows/msbuild.bat'
                        bat label: 'Test & coverage', script: '.ci/windows/testnet461.bat'
                        powershell label: 'Convert Test Results to junit format', script: '.ci\\windows\\convert.ps1'
                      }
                  }
                  post {
                    always {
                      archiveArtifacts(allowEmptyArchive: true, artifacts: "${BASE_DIR}/target/diag.log,${BASE_DIR}/target/TestResults.xml")
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
                      cleanDir("${WORKSPACE}/${BASE_DIR}")
                      unstash 'source'
                      dir("${BASE_DIR}"){
                        bat label: 'Build', script: '.ci/windows/msbuild.bat'
                        bat label: 'Test IIS', script: '.ci/windows/test-iis.bat'
                        powershell label: 'Convert Test Results to junit format', script: '.ci\\windows\\convert.ps1'
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
            stage('msbuild-tools-16.4.4.0'){
              agent { label 'windows-2019-test-immutable' }
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
                      powershell label: 'Install msbuild tools', script: "${BASE_DIR}\\.ci\\windows\\msbuild-tools-16.4.4.0.ps1"
                    }
                  }
                }
                /**
                Build the project from code..
                */
                stage('Build - MSBuild') {
                  steps {
                      cleanDir("${WORKSPACE}/${BASE_DIR}")
                      unstash 'source'
                      dir("${BASE_DIR}"){
                        bat '.ci/windows/msbuild.bat'
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
                      cleanDir("${WORKSPACE}/${BASE_DIR}")
                      unstash 'source'
                      dir("${BASE_DIR}"){
                        powershell label: 'Install test tools', script: '.ci\\windows\\test-tools.ps1'
                        bat label: 'Prepare solution', script: '.ci/windows/prepare-test.bat'
                        bat label: 'Build', script: '.ci/windows/msbuild.bat'
                        bat label: 'Test & coverage', script: '.ci/windows/testnet461.bat'
                        powershell label: 'Convert Test Results to junit format', script: '.ci\\windows\\convert.ps1'
                      }
                  }
                  post {
                    always {
                      archiveArtifacts(allowEmptyArchive: true, artifacts: "${BASE_DIR}/target/diag.log,${BASE_DIR}/target/TestResults.xml")
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
                      cleanDir("${WORKSPACE}/${BASE_DIR}")
                      unstash 'source'
                      dir("${BASE_DIR}"){
                        bat label: 'Build', script: '.ci/windows/msbuild.bat'
                        bat label: 'Test IIS', script: '.ci/windows/test-iis.bat'
                        powershell label: 'Convert Test Results to junit format', script: '.ci\\windows\\convert.ps1'
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
            stage('msbuild-tools-16.4.5.0'){
              agent { label 'windows-2019-test-immutable' }
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
                      powershell label: 'Install msbuild tools', script: "${BASE_DIR}\\.ci\\windows\\msbuild-tools-16.4.5.0.ps1"
                    }
                  }
                }
                /**
                Build the project from code..
                */
                stage('Build - MSBuild') {
                  steps {
                      cleanDir("${WORKSPACE}/${BASE_DIR}")
                      unstash 'source'
                      dir("${BASE_DIR}"){
                        bat '.ci/windows/msbuild.bat'
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
                      cleanDir("${WORKSPACE}/${BASE_DIR}")
                      unstash 'source'
                      dir("${BASE_DIR}"){
                        powershell label: 'Install test tools', script: '.ci\\windows\\test-tools.ps1'
                        bat label: 'Prepare solution', script: '.ci/windows/prepare-test.bat'
                        bat label: 'Build', script: '.ci/windows/msbuild.bat'
                        bat label: 'Test & coverage', script: '.ci/windows/testnet461.bat'
                        powershell label: 'Convert Test Results to junit format', script: '.ci\\windows\\convert.ps1'
                      }
                  }
                  post {
                    always {
                      archiveArtifacts(allowEmptyArchive: true, artifacts: "${BASE_DIR}/target/diag.log,${BASE_DIR}/target/TestResults.xml")
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
                      cleanDir("${WORKSPACE}/${BASE_DIR}")
                      unstash 'source'
                      dir("${BASE_DIR}"){
                        bat label: 'Build', script: '.ci/windows/msbuild.bat'
                        bat label: 'Test IIS', script: '.ci/windows/test-iis.bat'
                        powershell label: 'Convert Test Results to junit format', script: '.ci\\windows\\convert.ps1'
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
