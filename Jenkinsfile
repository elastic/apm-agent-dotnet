#!/usr/bin/env groovy
@Library('apm@current') _

import groovy.transform.Field

/**
This is the git commit sha which it's required to be used in different stages.
It does store the env GIT_SHA
*/
@Field def gitCommit

pipeline {
  agent none
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
          agent { label 'immutable' }
          options { skipDefaultCheckout() }
          steps {
            deleteDir()
            gitCheckout(basedir: "${BASE_DIR}", githubNotifyFirstTimeContributor: true)
            stash allowEmpty: true, name: 'source', useDefaultExcludes: false
            script {
              gitCommit = env.GIT_SHA
            }
            sh 'env | sort'
          }
        }

        stage('Integration Tests') {
          agent none
          when {
            beforeAgent true
            allOf {
              anyOf {
                environment name: 'GIT_BUILD_CAUSE', value: 'pr'
                expression { return !params.Run_As_Master_Branch }
              }
            }
          }
          steps {
            log(level: 'INFO', text: 'Launching Async ITs')
            // TODO: use commit rather than branch to be reproducible.
            build(job: env.ITS_PIPELINE, propagate: false, wait: false,
                  parameters: [string(name: 'AGENT_INTEGRATION_TEST', value: '.NET'),
                               string(name: 'BUILD_OPTS', value: "--dotnet-agent-version ${gitCommit}"),
                               string(name: 'GITHUB_CHECK_NAME', value: env.GITHUB_CHECK_ITS_NAME),
                               string(name: 'GITHUB_CHECK_REPO', value: env.REPO),
                               string(name: 'GITHUB_CHECK_SHA1', value: gitCommit)])
            githubNotify(context: "${env.GITHUB_CHECK_ITS_NAME}", description: "${env.GITHUB_CHECK_ITS_NAME} ...", status: 'PENDING', targetUrl: "${env.JENKINS_URL}search/?q=${env.ITS_PIPELINE.replaceAll('/','+')}")
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
    sh(label: 'Release', script: '.ci/linux/release.sh')
    def repo = getVaultSecret(secret: secret)
    wrap([$class: 'MaskPasswordsBuildWrapper', varPasswordPairs: [
      [var: 'REPO_API_KEY', password: repo.apiKey],
      [var: 'REPO_API_URL', password: repo.url],
      ]]) {
        sh(label: 'Deploy', script: ".ci/linux/deploy.sh ${repo.data.apiKey} ${repo.data.url}")
    }
  }
}
