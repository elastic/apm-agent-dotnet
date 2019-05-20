#!/usr/bin/env groovy

@Library('apm@current') _

pipeline {
  agent any
  environment {
    BASE_DIR="src/github.com/elastic/apm-agent-dotnet"
    NOTIFY_TO = credentials('notify-to')
    JOB_GCS_BUCKET = credentials('gcs-bucket')
    CODECOV_SECRET = 'secret/apm-team/ci/apm-agent-dotnet-codecov'
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
      stages{
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
                stage('Install tools') {
                  steps {
                    dir("${BASE_DIR}"){
                      deleteDir()
                    }
                    unstash 'source'
                    dir("${BASE_DIR}"){
                      sh label: 'Install tools', script: './.ci/tools.sh'
                      stash allowEmpty: true, name: 'dotnet-linux', includes: "dotnet/**", useDefaultExcludes: false
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
                      sh './.ci/build.sh'
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
                      sh label: 'Build', script: 'dotnet build'
                      sh label: 'Test & coverage', script: './.ci/test.sh'
                      sh label: 'Convert Test Results to junit format', script: './.ci/convert.sh'
                    }
                  }
                  post {
                    always {
                      junit(allowEmptyResults: true,
                        keepLongStdio: true,
                        testResults: "${BASE_DIR}/**/junit-*.xml,${BASE_DIR}/target/**/TEST-*.xml")
                      codecov(repo: 'apm-agent-dotnet', basedir: "${BASE_DIR}", secret: "${CODECOV_SECRET}")
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
                  VS_HOME = "C:\\Program Files (x86)\\Microsoft Visual Studio\\2017\\Enterprise"
                  MSBuildSDKsPath = "${env.DOTNET_ROOT}\\sdk\\2.1.505\\Sdks"
                  PATH = "${env.PATH};${env.HOME}\\bin;${env.DOTNET_ROOT};${env.DOTNET_ROOT}\\tools;\"${env.VS_HOME}\\MSBuild\\15.0\\Bin\""
                }
                stages{
                  /**
                  Checkout the code and stash it, to use it on other stages.
                  */
                  stage('Install tools') {
                    steps {
                      dir("${BASE_DIR}"){
                        deleteDir()
                      }
                      unstash 'source'
                      dir("${HOME}"){
                        powershell label: 'Install tools', script: "${readFile('.ci/windows/tools.ps1')}"
                      }
                    }
                  }
                  /**
                  Build the project from code..
                  */
                  stage('Build - MSBuild') {
                    steps {
                      dir("${BASE_DIR}"){
                        deleteDir()
                      }
                      unstash 'source'
                      dir("${BASE_DIR}"){
                        bat script: "${readFile('.ci/windows/msbuild.bat')}"
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
                        bat script: "${readFile('.ci/windows/dotnet.bat')}"
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
                        bat label: 'Build', script:'dotnet build'
                        bat label: 'Test & coverage', script: "${readFile('.ci/windows/test.bat')}"
                        powershell label: 'Convert Test Results to junit format', script: "${readFile('.ci/windows/convert.ps1')}"
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
                branch 'master'
                branch "\\d+\\.\\d+"
                branch "v\\d?"
                tag "\\d+\\.\\d+\\.\\d+*"
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
          stage('Release to AppVeyor') {
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
                branch 'master'
                expression { return params.Run_As_Master_Branch }
              }
            }
            steps {
              deleteDir()
              unstash 'source'
              unstash('dotnet-linux')
              dir("${BASE_DIR}"){
                release('secret/apm-team/ci/elastic-observability-appveyor')
              }
            }
            post{
              success {
                archiveArtifacts(allowEmptyArchive: true,
                  artifacts: "${BASE_DIR}/**/bin/Release/**/*.nupkg",
                  onlyIfSuccessful: true)
              }
            }
          }
          stage('Release to NuGet') {
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
                tag "\\d+\\.\\d+\\.\\d+*"
                expression { return params.Run_As_Master_Branch }
              }
            }
            steps {
              input(message: 'Should we release a new version on NuGet?', ok: 'Yes, we should.')
              deleteDir()
              unstash 'source'
              unstash('dotnet-linux')
              dir("${BASE_DIR}"){
                release('secret/apm-team/ci/elastic-observability-nuget')
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

def release(secret){
  sh(label: 'Release', script: '''
    dotnet sln remove test/Elastic.Apm.PerfTests/Elastic.Apm.PerfTests.csproj
    dotnet sln remove src/Elastic.Apm.AspNetFullFramework/Elastic.Apm.AspNetFullFramework.csproj
    dotnet pack -c Release
    ''')
  def repo = getVaultSecret(secret: secret)
  wrap([$class: 'MaskPasswordsBuildWrapper', varPasswordPairs: [
    [var: 'REPO_API_KEY', password: repo.apiKey],
    [var: 'REPO_API_URL', password: repo.url],
    ]]) {
      sh(label: 'Deploy',
        script: """
        for nupkg in \$(find . -name '*.nupkg')
        do
          dotnet nuget push \${nupkg} -k ${repo.data.apiKey} -s ${repo.data.url}
        done
        """)
    }
}
