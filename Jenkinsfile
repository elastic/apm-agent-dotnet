#!/usr/bin/env groovy

@Library('apm@current') _

pipeline {
  agent { label 'linux && immutable && docker' }
  environment {
    REPO = 'apm-agent-dotnet-auto-instrumentation'
    BASE_DIR = 'src'
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
    issueCommentTrigger("${obltGitHubComments()}")
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
              dir("${BASE_DIR}"){
                // Skip all the stages for PRs with changes in the docs only
              setEnvVar('ONLY_DOCS', isGitRegionMatch(patterns: [ '^docs/.*' ], shouldMatchAll: true))
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
              stages{
                stage('Build') {
                  steps {
                      deleteDir()
                      unstash 'source'
                      dir("${BASE_DIR}"){
                        dotnet(){
                        sh(label: 'Build', script: '.ci/linux/build.sh')
                          }
                        }
                      }
                  post {
                    success {
                      archiveArtifacts(allowEmptyArchive: true, artifacts: "${BASE_DIR}/build/output/*.zip")
                      }
                    }
                  }
                stage('Test') {
                  steps {
                        dir("${BASE_DIR}"){
                          dotnet(){
                        sh label: 'Test', script: '.ci/linux/test.sh'
                          }
                        }
                      }
                  post {
                    always {
                      junit(allowEmptyResults: true, keepLongStdio: true, testResults: "${BASE_DIR}/test_results/junit-*.xml")
                    }
                    }
                  }
                }
              }
            stage('Windows'){
              agent { label 'windows-2019 && immutable' }
              options { skipDefaultCheckout() }
              environment {
                HOME = "${env.WORKSPACE}"
                CARGO_MAKE_HOME = "C:\\tools\\cargo"    // If cargo is installed within the CI build
                PATH = "${PATH};${env.CARGO_MAKE_HOME};${env.USERPROFILE}\\.cargo\\bin" // If cargo is installed within the CI build
              }
              stages{
                stage('Install tools') {
                  steps {
                    cleanDir("${WORKSPACE}/*")
                    unstash 'source'
                      dir("${BASE_DIR}"){
                      powershell(label: 'Install tools', script: ".ci\\windows\\tools.ps1")
                      }
                    }
                  }
                stage('Build') {
                  steps {
                      dir("${BASE_DIR}"){
                      bat(label: 'Build', script: '.\\build.bat profiler-zip')
                      }
                    }
                  post {
                    success {
                      archiveArtifacts(allowEmptyArchive: true, artifacts: "${BASE_DIR}/build/output/*.zip")
                    }
                    }
                  }
                stage('Test') {
                  steps {
                      dir("${BASE_DIR}"){
                      bat(label: 'Build', script: 'cargo make test')
                        }
                        }
                  post {
                    always {
                      junit(allowEmptyResults: true, keepLongStdio: true, testResults: "${BASE_DIR}/test_results/junit-*.xml")
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
                  }

def dotnet(Closure body){
  def dockerTagName = 'docker.elastic.co/observability-ci/apm-agent-dotnet-auto-instrumentation-sdk-linux:latest'
  sh label: 'Docker build', script: "docker build --tag ${dockerTagName} .ci/docker/sdk-linux"
  def homePath = "${env.WORKSPACE}/${env.BASE_DIR}"
  docker.image("${dockerTagName}").inside("-e HOME='${homePath}' -v /var/run/docker.sock:/var/run/docker.sock"){
    // CARGO_HOME env variable is set explicilty in the docker image, let's override it
    // to permission access when running the docker container with a differen user.
    withEnv(["CARGO_HOME=${homePath}"]) {
        body()
      }
    }
  }

def cleanDir(path){
  powershell label: "Clean ${path}", script: "Remove-Item -Recurse -Force ${path}"
        }
