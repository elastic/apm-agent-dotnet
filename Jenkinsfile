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
                        bat returnStatus: true, script: 'msbuild'
                        bat "dotnet msbuild sample/AspNetFullFrameworkSampleApp/AspNetFullFrameworkSampleApp.csproj"
                      }
                    }
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
