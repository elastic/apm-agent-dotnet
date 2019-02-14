#!/usr/bin/env groovy

@Library('apm@v1.0.6') _

pipeline {
  agent any
  environment {
    BASE_DIR="src/github.com/elastic/apm-agent-dotnet"
    NOTIFY_TO = credentials('notify-to')
    JOB_GCS_BUCKET = credentials('gcs-bucket')
  }
  options {
    timeout(time: 1, unit: 'HOURS')
    buildDiscarder(logRotator(numToKeepStr: '20', artifactNumToKeepStr: '20', daysToKeepStr: '30'))
    timestamps()
    ansiColor('xterm')
    disableResume()
    durabilityHint('PERFORMANCE_OPTIMIZED')
  }
  triggers {
    issueCommentTrigger('.*(?:jenkins\\W+)?run\\W+(?:the\\W+)?tests(?:\\W+please)?.*')
  }
  parameters {
    booleanParam(name: 'Run_As_Master_Branch', defaultValue: false, description: 'Allow to run any steps on a PR, some steps normally only run on master branch.')
  }
  stages {
    stage('Initializing'){
      stages {
        stage('Checkout') {
          agent { label 'linux && immutable' }
          options { skipDefaultCheckout() }
          steps {
            deleteDir()
            gitCheckout(basedir: "${BASE_DIR}")
            stash allowEmpty: true, name: 'source', useDefaultExcludes: false
          }
        }
        stage('Parallel'){
          parallel{
            stage('Linux'){
              agent { label 'linux && immutable' }
              options { skipDefaultCheckout() }
              environment {
                HOME = "${env.WORKSPACE}"
                PATH = "${env.PATH}:${env.HOME}/bin:${env.HOME}/dotnet:${env.HOME}/.dotnet/tools"
                DOTNET_ROOT = "${env.HOME}/dotnet"
              }
              stages{
                /**
                Checkout the code and stash it, to use it on other stages.
                */
                stage('Install .Net SDK') {
                  steps {
                    deleteDir()
                    sh label: 'Download and install .Net SDK', script: """#!/bin/bash
                    curl -o dotnet.tar.gz -L https://download.microsoft.com/download/4/0/9/40920432-3302-47a8-b13c-bbc4848ad114/dotnet-sdk-2.1.302-linux-x64.tar.gz
                    mkdir -p ${HOME}/dotnet && tar zxf dotnet.tar.gz -C ${HOME}/dotnet
                    """
                    stash allowEmpty: true, name: 'dotnet-linux', includes: "dotnet/**", useDefaultExcludes: false
                  }
                }
                /**
                Build the project from code..
                */
                stage('Build') {
                  steps {
                    dir("${BASE_DIR}"){
                      deleteDir()
                    }
                    unstash 'source'
                    dir("${BASE_DIR}"){
                      sh 'dotnet build'
                    }
                  }
                }
                /**
                Execute unit tests.
                */
                stage('Test') {
                  steps {
                    dir("${BASE_DIR}"){
                      deleteDir()
                    }
                    unstash 'source'
                    dir("${BASE_DIR}"){
                      sh label: 'Install tools', script: '''#!/bin/bash
                      set -euxo pipefail
                      # install tools
                      dotnet tool install -g dotnet-xunit-to-junit --version 0.3.1
                      for i in $(find . -name '*.csproj')
                      do
                        dotnet add "$i" package XunitXml.TestLogger --version 2.0.0
                        dotnet add "$i" package coverlet.msbuild --version 2.5.1
                      done
                      '''

                      sh label: 'Build', script: 'dotnet build'

                      sh label: 'Test & coverage', script: '''#!/bin/bash
                      set -euxo pipefail
                      # run tests
                      dotnet test -v n -r target -d target/diag.log --logger:"xunit" --no-build \
                        /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura \
                        /p:CoverletOutput=target/Coverage/ \
                        /p:Exclude='"[Elastic.Apm.Tests]*,[SampleAspNetCoreApp*]*,[xunit*]*"' \
                        /p:Threshold=0 /p:ThresholdType=branch /p:ThresholdStat=total \
                        || echo -e "\033[31;49mTests FAILED\033[0m"
                      '''

                      sh label: 'Convert Test Results to junit format', script: '''#!/bin/bash
                      set -euxo pipefail
                      #convert xunit files to junit files
                      for i in $(find . -name TestResults.xml)
                      do
                        DIR=$(dirname "$i")
                        dotnet xunit-to-junit "$i" "${DIR}/junit-testTesults.xml"
                      done
                      '''
                    }
                  }
                  post {
                    always {
                      junit(allowEmptyResults: true,
                        keepLongStdio: true,
                        testResults: "${BASE_DIR}/**/junit-*.xml,${BASE_DIR}/target/**/TEST-*.xml")
                      codecov(repo: 'apm-agent-dotnet', basedir: "${BASE_DIR}")
                      }
                    }
                  }
                }
              }
              stage('Windows'){
                agent { label 'windows-2016' }
                options { skipDefaultCheckout() }
                environment {
                  HOME = "${env.WORKSPACE}"
                  DOTNET_ROOT = "${env.WORKSPACE}\\dotnet"
                  PATH = "${env.PATH};${env.HOME}\\bin;${env.HOME}\\.dotnet\\tools;${env.DOTNET_ROOT};${env.DOTNET_ROOT}\\tools"
                }
                stages{
                  /**
                  Checkout the code and stash it, to use it on other stages.
                  */
                  stage('Install .Net SDK') {
                    steps {
                      deleteDir()
                      dir("${HOME}/dotnet"){
                        powershell label: 'Download .Net SDK', script: """
                        Invoke-WebRequest https://download.visualstudio.microsoft.com/download/pr/c332d70f-6582-4471-96af-4b0c17a616ad/5f3043d4bc506bf91cb89fa90462bb58/dotnet-sdk-2.2.103-win-x64.zip -OutFile dotnet.zip
                        """
                        powershell label: 'Install .Net SDK', script: """
                        Add-Type -As System.IO.Compression.FileSystem
                        [IO.Compression.ZipFile]::ExtractToDirectory('dotnet.zip', '.')
                        """
                      }
                    }
                  }
                  /**
                  Build the project from code..
                  */
                  stage('Build') {
                    steps {
                      dir("${BASE_DIR}"){
                        deleteDir()
                      }
                      unstash 'source'
                      dir("${BASE_DIR}"){
                        bat "dotnet build"
                      }
                    }
                  }
                  /**
                  Execute unit tests.
                  */
                  stage('Test') {
                    steps {
                      dir("${BASE_DIR}"){
                        deleteDir()
                      }
                      unstash 'source'
                      dir("${BASE_DIR}"){
                        powershell label: 'Install tools', script: '''
                        & dotnet tool install -g dotnet-xunit-to-junit --version 0.3.1
                        & dotnet tool install -g Codecov.Tool --version 1.2.0

                        Get-ChildItem -Path . -Recurse -Filter *.csproj |
                        Foreach-Object {
                          & dotnet add $_.FullName package XunitXml.TestLogger --version 2.0.0
                          & dotnet add $_.FullName package coverlet.msbuild --version 2.5.1
                        }
                        '''

                        bat label: 'Build', script:'dotnet build'

                        bat label: 'Test & Coverage', script: 'dotnet test -v n -r target -d target\\diag.log --logger:xunit --no-build /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:CoverletOutput=target\\Coverage\\ /p:Exclude=\\"[Elastic.Apm.Tests]*,[SampleAspNetCoreApp*]*,[xunit*]*\\" /p:Threshold=0 /p:ThresholdType=branch /p:ThresholdStat=total'

                        powershell label: 'Convert Test Results to junit format', script: '''
                        [System.Environment]::SetEnvironmentVariable("PATH", $Env:Path + ";" + $Env:USERPROFILE + "\\.dotnet\\tools")
                        Get-ChildItem -Path . -Recurse -Filter TestResults.xml |
                        Foreach-Object {
                          & dotnet xunit-to-junit $_.FullName $_.parent.FullName + '\\junit-testTesults.xml'
                        }
                        '''

                        script {
                          def codecovId = getVaultSecret('apm-agent-dotnet-codecov')?.data?.value
                          powershell label: 'Send covertura report to Codecov', script:"""
                          [System.Environment]::SetEnvironmentVariable("PATH", \$Env:Path + ";" + \$Env:USERPROFILE + "\\.dotnet\\tools")
                          Get-ChildItem -Path . -Recurse -Filter coverage.cobertura.xml |
                          Foreach-Object {
                            & codecov -t ${codecovId} -f \$_.FullName
                          }
                          """
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
              }
            }
          }
          /**
          Build the documentation.
          */
          stage('Documentation') {
            agent { label 'linux && immutable' }
            options { skipDefaultCheckout() }
            environment {
              HOME = "${env.WORKSPACE}"
            }
            when {
              beforeAgent true
              anyOf {
                not {
                  changeRequest()
                }
                branch 'master'
                branch "\\d+\\.\\d+"
                branch "v\\d?"
                tag "v\\d+\\.\\d+\\.\\d+*"
                expression { return params.Run_As_Master_Branch }
              }
            }
            steps {
              deleteDir()
              unstash 'source'
              dir("${BASE_DIR}"){
                buildDocs(docsDir: "docs", archive: true)
              }
            }
          }
          stage('Release') {
            agent { label 'linux && immutable' }
            options { skipDefaultCheckout() }
            environment {
              HOME = "${env.WORKSPACE}"
              PATH = "${env.PATH}:${env.HOME}/bin:${env.HOME}/dotnet:${env.HOME}/.dotnet/tools"
              DOTNET_ROOT = "${env.HOME}/dotnet"
            }
            when {
              beforeAgent true
              anyOf {
                not {
                  changeRequest()
                }
                branch 'master'
                branch "\\d+\\.\\d+"
                branch "v\\d?"
                tag "v\\d+\\.\\d+\\.\\d+*"
                expression { return params.Run_As_Master_Branch }
              }
            }
            steps {
              deleteDir()
              unstash 'source'
              unstash('dotnet-linux')
              dir("${BASE_DIR}"){
                sh label: 'Release', script: 'dotnet pack -c Release'
              }
            }
            post{
              success {
                archiveArtifacts(allowEmptyArchive: true,
                  artifacts: "${BASE_DIR}/bin/release/**/*",
                  onlyIfSuccessful: true)
              }
            }
          }
        }
      }
    }
    post {
      success {
        echoColor(text: '[SUCCESS]', colorfg: 'green', colorbg: 'default')
      }
      aborted {
        echoColor(text: '[ABORTED]', colorfg: 'magenta', colorbg: 'default')
      }
      failure {
        echoColor(text: '[FAILURE]', colorfg: 'red', colorbg: 'default')
        step([$class: 'Mailer', notifyEveryUnstableBuild: true, recipients: "${NOTIFY_TO}", sendToIndividuals: false])
      }
      unstable {
        echoColor(text: '[UNSTABLE]', colorfg: 'yellow', colorbg: 'default')
      }
    }
  }
