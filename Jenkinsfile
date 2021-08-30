#!/usr/bin/env groovy

@Library('apm@current') _

pipeline {
  agent { label 'linux && immutable' }
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
    issueCommentTrigger('(?i).*(?:jenkins\\W+)?run\\W+(?:the\\W+)?tests(?:\\W+please)?.*')
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
              environment {
                MSBUILDDEBUGPATH = "${env.WORKSPACE}"
              }
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
                }
                stage('Test') {
                  steps {
                    dir("${BASE_DIR}"){
                      dotnet(){
                        sh label: 'Test', script: '.ci/linux/test.sh'
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
