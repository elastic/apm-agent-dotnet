#!/usr/bin/env groovy

@Library('apm@current') _

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
    rateLimitBuilds(throttle: [count: 60, durationName: 'hour', userBoost: true])
    quietPeriod(10)
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
          agent { label 'master || immutable' }
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
                    sh label: 'Install .Net SDK', script: """#!/bin/bash
                    curl -O https://dot.net/v1/dotnet-install.sh
                    /bin/bash ./dotnet-install.sh --install-dir ${HOME}/dotnet -Channel LTS
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
                      sh '''
                      dotnet sln remove sample/AspNetFullFrameworkSampleApp/AspNetFullFrameworkSampleApp.csproj
                      dotnet build
                      '''
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
                      dotnet sln remove sample/AspNetFullFrameworkSampleApp/AspNetFullFrameworkSampleApp.csproj

                      # install tools
                      dotnet tool install -g dotnet-xunit-to-junit --version 0.3.1
                      for i in $(find . -name '*.csproj')
                      do
                        if [[ $i == *"AspNetFullFrameworkSampleApp.csproj"* ]]; then
                            continue
                        fi
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
                  VS_HOME = "C:\\Program Files (x86)\\Microsoft Visual Studio\\2017"
                  PATH = "${env.PATH};${env.HOME}\\bin;${env.HOME}\\.dotnet\\tools;${env.DOTNET_ROOT};${env.DOTNET_ROOT}\\tools;\"${env.VS_HOME}\\BuildTools\\MSBuild\\15.0\\Bin\""
                }
                stages{
                  /**
                  Checkout the code and stash it, to use it on other stages.
                  */
                  stage('Install .Net SDK') {
                    steps {
                      deleteDir()
                      dir("${HOME}"){
                        powershell label: 'Download .Net SDK installer script', script: """
                        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
                        Invoke-WebRequest "https://dot.net/v1/dotnet-install.ps1" -OutFile dotnet-install.ps1 -UseBasicParsing ;
                        """
                        powershell label: 'Install .Net SDK', script: """
                        & ./dotnet-install.ps1 -Channel LTS -InstallDir ./dotnet
                        """

                        powershell label: 'Install NuGet Tool', script: """
                        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
                        Invoke-WebRequest "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe" -OutFile dotnet\\nuget.exe -UseBasicParsing ;
                        """
                      }
                    }
                  }
                  /**
                  Build the project from code..
                  */
                  stage('Build - MSBuild (disabled)') {
                    steps {
                      dir("${BASE_DIR}"){
                        deleteDir()
                      }
                      unstash 'source'
                      dir("${BASE_DIR}"){
                        // bat """
                        // nuget restore ElasticApmAgent.sln
                        // msbuild
                        // """
                        echo "NOOP"
                      }
                    }
                  }
                  /**
                  Build the project from code..
                  */
                  stage('Build - dotnet') {
                    steps {
                      dir("${BASE_DIR}"){
                        deleteDir()
                      }
                      unstash 'source'
                      dir("${BASE_DIR}"){
                        bat """
                        dotnet sln remove sample/AspNetFullFrameworkSampleApp/AspNetFullFrameworkSampleApp.csproj
                        dotnet build
                        """
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
                        & dotnet sln remove sample/AspNetFullFrameworkSampleApp/AspNetFullFrameworkSampleApp.csproj

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
