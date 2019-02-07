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
                stage('Checkout') {
                  steps {
                    deleteDir()
                    gitCheckout(basedir: "${BASE_DIR}")
                    stash allowEmpty: true, name: 'source', useDefaultExcludes: false

                    sh """#!/bin/bash
                    curl -o dotnet.tar.gz -L https://download.microsoft.com/download/4/0/9/40920432-3302-47a8-b13c-bbc4848ad114/dotnet-sdk-2.1.302-linux-x64.tar.gz
                    mkdir -p ${HOME}/dotnet && tar zxf dotnet.tar.gz -C ${HOME}/dotnet
                    """
                    stash allowEmpty: true, name: 'dotnet', includes: "dotnet/**", useDefaultExcludes: false
                  }
                }
                /**
                Build the project from code..
                */
                stage('Build') {
                  steps {
                    deleteDir()
                    unstash 'source'
                    unstash 'dotnet'
                    dir("${BASE_DIR}"){
                      sh """#!/bin/bash
                      set -euxo pipefail
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
                    deleteDir()
                    unstash 'source'
                    unstash 'dotnet'
                    dir("${BASE_DIR}"){
                      sh '''#!/bin/bash
                      set -euxo pipefail

                      # install tools
                      dotnet tool install -g dotnet-xunit-to-junit --version 0.3.1
                      for i in $(find . -name '*.??proj')
                      do
                      dotnet add "$i" package XunitXml.TestLogger --version 2.0.0
                      dotnet add "$i" package coverlet.msbuild --version 2.5.0
                      done

                      # build
                      dotnet build

                      # run tests
                      dotnet test -v n -r target -d target/diag.log --logger:"xunit" --no-build \
                      /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura \
                      /p:CoverletOutput=target/Coverage/ || echo -e "\033[31;49mTests FAILED\033[0m"

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
                agent { label 'windows' }
                options { skipDefaultCheckout() }
                environment {
                  HOME = "${env.WORKSPACE}"
                  DOTNET_ROOT = "${env.HOME}/dotnet"
                  PATH = "${env.PATH};${env.HOME}\\bin;${env.DOTNET_ROOT};${env.DOTNET_ROOT}\\tools"
                }
                stages{
                  /**
                  Checkout the code and stash it, to use it on other stages.
                  */
                  stage('Checkout') {
                    steps {
                      deleteDir()
                      gitCheckout(basedir: "${BASE_DIR}")
                      stash allowEmpty: true, name: 'source', useDefaultExcludes: false

                      powershell label: 'Download and install .Net SDK', script: """
                      mkdir -p ${HOME}/dotnet
                      cd ${HOME}/dotnet
                      Invoke-WebRequest https://download.visualstudio.microsoft.com/download/pr/c332d70f-6582-4471-96af-4b0c17a616ad/5f3043d4bc506bf91cb89fa90462bb58/dotnet-sdk-2.2.103-win-x64.zip -OutFile dotnet.zip
                      [IO.Compression.ZipFile]::ExtractToDirectory('dotnet.zip', '.')
                      """
                      stash allowEmpty: true, name: 'dotnet', includes: "dotnet/**", useDefaultExcludes: false
                    }
                  }
                  /**
                  Build the project from code..
                  */
                  stage('Build') {
                    steps {
                      deleteDir()
                      unstash 'source'
                      unstash 'dotnet'
                      dir("${BASE_DIR}"){
                        powershell "dotnet build"
                      }
                    }
                  }
                  /**
                  Execute unit tests.
                  */
                  stage('Test') {
                    steps {
                      deleteDir()
                      unstash 'source'
                      unstash 'dotnet'
                      dir("${BASE_DIR}"){
                        powershell '''
                        dotnet tool install -g dotnet-xunit-to-junit --version 0.3.1

                        Get-ChildItem '.' -Filter *.??proj |
                        Foreach-Object {
                          dotnet add $_.FullName package XunitXml.TestLogger --version 2.0.0
                          dotnet add $_.FullName package coverlet.msbuild --version 2.5.0
                        }

                        dotnet build

                        dotnet test -v n -r target -d target\\diag.log --logger:xunit --no-build \
                        /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura \
                        /p:CoverletOutput=target\\Coverage\\

                        Get-ChildItem '.' -Filter TestResults.xml |
                        Foreach-Object {
                          dotnet xunit-to-junit $_.FullName $_.parent.FullName + '\\junit-testTesults.xml'
                        }
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
            }
          }
          /**
          Build the documentation.
          */
          stage('Documentation') {
            environment {
              ELASTIC_DOCS = "${env.WORKSPACE}/elastic/docs"
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
              checkoutElasticDocsTools(basedir: "${ELASTIC_DOCS}")
              dir("${BASE_DIR}"){
                sh '''#!/usr/bin/env bash

                if [ -z "${ELASTIC_DOCS}" -o ! -d "${ELASTIC_DOCS}" ]; then
                echo "ELASTIC_DOCS is not defined, it should point to a folder where you checkout https://github.com/elastic/docs.git."
                echo "You also can define BUILD_DOCS_ARGS for aditional build options."
                exit 1
                fi

                ${ELASTIC_DOCS}/build_docs.pl --chunk=1 ${BUILD_DOCS_ARGS} --doc docs/index.asciidoc -out docs/html
                '''
              }
            }
            post{
              success {
                tar(file: "doc-files.tgz", archive: true, dir: "html", pathPrefix: "${BASE_DIR}/docs")
              }
            }
          }
          stage('Release') {
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
                sh '''#!/usr/bin/env bash
                  dotnet pack -c Release
                '''
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
