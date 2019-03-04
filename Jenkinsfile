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
        //https://dot.net/v1/dotnet-install.sh
        //https://download.microsoft.com/download/D/7/5/D75188CA-848C-4634-B402-4B746E9F516A/DotNetCore.1.0.1-VS2015Tools.Preview2.0.4.exe
              stage('Windows'){
                agent { label 'windows' }
                options { skipDefaultCheckout() }
                environment {
                  HOME = "${env.WORKSPACE}"
                  DOTNET_ROOT = "${env.WORKSPACE}\\dotnet"
                  VS_HOME = "C:\\Program Files (x86)\\Microsoft Visual Studio\\2017"
                  PATH = "${env.PATH};${env.HOME}\\bin;${env.HOME}\\.dotnet\\tools;${env.DOTNET_ROOT};${env.DOTNET_ROOT}\\tools;\"${env.VS_HOME}\\BuildTools\\MSBuild\\15.0\\Bin\""
                }
                stages{
                  stage('Install .Net SDK from URL') {
                    steps {
                      deleteDir()
                      dir("${HOME}"){
                        powershell label: 'Download .Net SDK', script: """
                        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
                        Invoke-WebRequest "https://dot.net/v1/dotnet-install.ps1" -OutFile dotnet-install.ps1 -UseBasicParsing ;
                        """
                        powershell label: 'Install .Net SDK', script: """
                        & ./dotnet-install.ps1 -Channel LTS -InstallDir ./dotnet
                        """
                        /*
                        powershell label: 'Install MSBuild Tools', script: """
                        Invoke-WebRequest "https://aka.ms/vs/15/release/vs_BuildTools.exe" -OutFile vs_BuildTools.exe -UseBasicParsing ; \
                        	Start-Process -FilePath 'vs_BuildTools.exe' -ArgumentList '--quiet', '--norestart', \
                          '--installPath ${WORKSPACE}\\vs2017', \
                          '--add Microsoft.Net.Core.Component.SDK', \
                          '--add Microsoft.VisualStudio.Workload.MSBuildTools', \
                          '--add Microsoft.VisualStudio.Component.WebDeploy', \
                          '--add Microsoft.VisualStudio.Workload.WebBuildTools', \
                          -Wait ;
                        """
                        vs_BuildTools.exe --add Microsoft.VisualStudio.Workload.MSBuildTools
                        */
                      }
                    }
                  }
                  /**
                  Build the project from code..
                  */
                  stage('Build') {
                    steps {
                      deleteDir()
                      unstash 'source'
                      dir("${BASE_DIR}"){
                        bat """
                        echo %PATH%
                        ;nuget restore ElasticApmAgent.sln
                        ${DOTNET_ROOT}\\dotnet restore
                        msbuild
                        """
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
                        dotnet sln remove sample\\AspNetFullFrameworkSampleApp\\AspNetFullFrameworkSampleApp.csproj
                        dotnet build
                        """
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
