#!/usr/bin/env groovy

pipeline {
  agent none
  environment {
    BASE_DIR="src/github.com/elastic/apm-agent-dotnet"
  }
  options {
    timeout(time: 1, unit: 'HOURS') 
    buildDiscarder(logRotator(numToKeepStr: '20', artifactNumToKeepStr: '20', daysToKeepStr: '30'))
    timestamps()
    ansiColor('xterm')
    disableResume()
    durabilityHint('PERFORMANCE_OPTIMIZED')
  }
  parameters {
    booleanParam(name: 'Run_As_Master_Branch', defaultValue: false, description: 'Allow to run any steps on a PR, some steps normally only run on master branch.')
  }
  stages {
    stage('Initializing'){
      agent { label 'linux && immutable' }
      options { skipDefaultCheckout() }
      environment {
        ELASTIC_DOCS = "${env.WORKSPACE}/elastic/docs"
        HOME = "${env.WORKSPACE}"
        PATH = "${env.PATH}:${env.HOME}/bin:${env.HOME}/dotnet:${env.HOME}.dotnet/tools"
        DOTNET_ROOT = "${env.HOME}/dotnet"
      }
      stages {
        /**
         Checkout the code and stash it, to use it on other stages.
        */
        stage('Checkout') {
          steps {
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
            withEnvWrapper() {
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
        }
        /**
         Execute unit tests.
        */
        stage('Test') {
          steps {
            withEnvWrapper() {
              unstash 'source'
              unstash 'dotnet'
              dir("${BASE_DIR}"){
                sh '''#!/bin/bash
                set -euxo pipefail
                for i in $(find . -name '*.??proj') 
                do 
                  dotnet add "$i" package XunitXml.TestLogger --version 2.0.0
                done
                dotnet tool install -g dotnet-xunit-to-junit
                dotnet test -v n -r target -d target/diag.log --logger:"xunit"
                
                for i in $(find . -name TestResults.xml)
                do
                  DIR=$(dirname "$i")
                  dotnet xunit-to-junit "$i" "${DIR}/junit-testTesults.xml"
                done
                '''
              }
            }
          }
          post { 
            always {
              junit(allowEmptyResults: true, 
                keepLongStdio: true, 
                testResults: "${BASE_DIR}/**/junit-*.xml,${BASE_DIR}/target/**/TEST-*.xml")
              codecov("apm-agent-dotnet")
            }
          }
        }
        /**
          Build the documentation.
        */
        stage('Documentation') {
          when {
            beforeAgent true
            allOf {
              anyOf {
                not {
                  changeRequest()
                }
                branch 'master'
                branch "\\d+\\.\\d+"
                branch "v\\d?"
                tag "v\\d+\\.\\d+\\.\\d+*"
                environment name: 'Run_As_Master_Branch', value: 'true'
              }
              environment name: 'doc_ci', value: 'true'
            }
          }
          steps {
            withEnvWrapper() {
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
          }
          post{
            success {
              tar(file: "doc-files.tgz", archive: true, dir: "html", pathPrefix: "${BASE_DIR}/docs")
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