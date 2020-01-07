#!/usr/bin/env groovy

@Library('apm@current') _

pipeline {
  agent { label 'linux && immutable' }
  environment {
    REPO = 'apm-agent-dotnet'
    // keep it short to avoid the 248 characters PATH limit in Windows
    BASE_DIR = "apm-agent-dotnet"
    NOTIFY_TO = credentials('notify-to')
    JOB_GCS_BUCKET = credentials('gcs-bucket')
    CODECOV_SECRET = 'secret/apm-team/ci/apm-agent-dotnet-codecov'
    GITHUB_CHECK_ITS_NAME = 'Integration Tests'
    ITS_PIPELINE = 'apm-integration-tests-selector-mbp/master'
    OPBEANS_REPO = 'opbeans-dotnet'
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
          options { skipDefaultCheckout() }
          steps {
            pipelineManager([ cancelPreviousRunningBuilds: [ when: 'PR' ] ])
            deleteDir()
            gitCheckout(basedir: "${BASE_DIR}", githubNotifyFirstTimeContributor: true)
            stash allowEmpty: true, name: 'source', useDefaultExcludes: false
          }
        }
        stage('Parallel'){
          parallel{
            stage('Linux'){
              options { skipDefaultCheckout() }
              environment {
                MSBUILDDEBUGPATH = "${env.WORKSPACE}"
              }
              /**
              Make sure there are no code style violation in the repo.
              */
              stages{
                // Disable until https://github.com/elastic/apm-agent-dotnet/issues/563
                // stage('CodeStyleCheck') {
                //   steps {
                //     withGithubNotify(context: 'CodeStyle check') {
                //       deleteDir()
                //       unstash 'source'
                //       dir("${BASE_DIR}"){
                //         dotnet(){
                //           sh label: 'Install and run dotnet/format', script: '.ci/linux/codestyle.sh'
                //         }
                //       }
                //     }
                //   }
                // }
                /**
                Build the project from code..
                */
                stage('Build') {
                  steps {
                    withGithubNotify(context: 'Build - Linux') {
                      deleteDir()
                      unstash 'source'
                      dir("${BASE_DIR}"){
                        dotnet(){
                          sh '.ci/linux/build.sh'
                        }
                      }
                    }
                  }
                  post {
                    unsuccessful {
                      archiveArtifacts(allowEmptyArchive: true,
                        artifacts: "${MSBUILDDEBUGPATH}/**/MSBuild_*.failure.txt")
                    }
                  }
                }
                /**
                Execute unit tests.
                */
                stage('Test') {
                  steps {
                    withGithubNotify(context: 'Test - Linux', tab: 'tests') {
                      deleteDir()
                      unstash 'source'
                      dir("${BASE_DIR}"){
                        dotnet(){
                          sh label: 'Test & coverage', script: '.ci/linux/test.sh'
                          sh label: 'Convert Test Results to junit format', script: '.ci/linux/convert.sh'
                        }
                      }
                    }
                  }
                  post {
                    always {
                      sh label: 'debugging', script: 'find . -name *.pdb'
                      junit(allowEmptyResults: true,
                        keepLongStdio: true,
                        testResults: "${BASE_DIR}/**/junit-*.xml,${BASE_DIR}/target/**/TEST-*.xml")
                      codecov(repo: env.REPO, basedir: "${BASE_DIR}", secret: "${CODECOV_SECRET}")
                    }
                    unsuccessful {
                      archiveArtifacts(allowEmptyArchive: true,
                        artifacts: "${MSBUILDDEBUGPATH}/**/MSBuild_*.failure.txt")
                    }
                    }
                  }
                }
              }
              stage('Windows .NET Framework'){
                agent { label 'windows-2019-immutable' }
                options { skipDefaultCheckout() }
                environment {
                  HOME = "${env.WORKSPACE}"
                  DOTNET_ROOT = "${env.WORKSPACE}\\dotnet"
                  PATH = "${env.DOTNET_ROOT};${env.DOTNET_ROOT}\\tools;${env.PATH};${env.HOME}\\bin"
                  MSBUILDDEBUGPATH = "${env.WORKSPACE}"
                }
                stages{
                  /**
                  Install the required tools
                  */
                  stage('Install tools') {
                    steps {
                      cleanDir("${WORKSPACE}/*")
                      unstash 'source'
                      dir("${HOME}"){
                        powershell label: 'Install tools', script: "${BASE_DIR}\\.ci\\windows\\tools.ps1"
                        powershell label: 'Install msbuild tools', script: "${BASE_DIR}\\.ci\\windows\\msbuild-tools.ps1"
                      }
                    }
                  }
                  /**
                  Build the project from code..
                  */
                  stage('Build - MSBuild') {
                    steps {
                      withGithubNotify(context: 'Build MSBuild - Windows') {
                        cleanDir("${WORKSPACE}/${BASE_DIR}")
                        unstash 'source'
                        dir("${BASE_DIR}"){
                          bat '.ci/windows/msbuild.bat'
                        }
                      }
                    }
                    post {
                      unsuccessful {
                        archiveArtifacts(allowEmptyArchive: true,
                          artifacts: "${MSBUILDDEBUGPATH}/**/MSBuild_*.failure.txt")
                      }
                    }
                  }
                  /**
                  Execute unit tests.
                  */
                  stage('Test') {
                    steps {
                      withGithubNotify(context: 'Test MSBuild - Windows', tab: 'tests') {
                        cleanDir("${WORKSPACE}/${BASE_DIR}")
                        unstash 'source'
                        dir("${BASE_DIR}"){
                          powershell label: 'Install test tools', script: '.ci\\windows\\test-tools.ps1'
                          bat label: 'Prepare solution', script: '.ci/windows/prepare-test.bat'
                          bat label: 'Build', script: '.ci/windows/msbuild.bat'
                          bat label: 'Test & coverage', script: '.ci/windows/test.bat'
                          powershell label: 'Convert Test Results to junit format', script: '.ci\\windows\\convert.ps1'
                        }
                      }
                    }
                    post {
                      always {
                        archiveArtifacts(allowEmptyArchive: true, artifacts: "${BASE_DIR}/target/diag.log,${BASE_DIR}/target/TestResults.xml")
                        junit(allowEmptyResults: true,
                          keepLongStdio: true,
                          testResults: "${BASE_DIR}/**/junit-*.xml,${BASE_DIR}/target/**/TEST-*.xml")
                      }
                      unsuccessful {
                        archiveArtifacts(allowEmptyArchive: true,
                          artifacts: "${MSBUILDDEBUGPATH}/**/MSBuild_*.failure.txt")
                      }
                    }
                  }
                  /**
                  Execute IIS tests.
                  */
                  stage('IIS Tests') {
                    steps {
                      withGithubNotify(context: 'IIS Tests', tab: 'tests') {
                        cleanDir("${WORKSPACE}/${BASE_DIR}")
                        unstash 'source'
                        dir("${BASE_DIR}"){
                          bat label: 'Build', script: '.ci/windows/msbuild.bat'
                          bat label: 'Test IIS', script: '.ci/windows/test-iis.bat'
                          powershell label: 'Convert Test Results to junit format', script: '.ci\\windows\\convert.ps1'
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
                post {
                  always {
                    cleanWs(disableDeferredWipeout: true, notFailBuild: true)
                  }
                }
              }
              stage('Docker .NET Framework'){
                agent { label 'windows-2019-docker-immutable' }
                options { skipDefaultCheckout() }
                stages {
                  stage('Build - Docker MSBuild') {
                    steps {
                      withGithubNotify(context: 'Build MSBuild - Docker') {
                        cleanDir("${WORKSPACE}/${BASE_DIR}")
                        unstash 'source'
                        dir("${BASE_DIR}") {
                          catchError(message: 'Beta stage', buildResult: 'SUCCESS', stageResult: 'UNSTABLE') {
                            dotnetWindows(){
                              bat 'msbuild'
                            }
                          }
                        }
                      }
                    }
                  }
                }
              }
              stage('Windows .NET Core'){
                agent { label 'windows-2019-immutable' }
                options { skipDefaultCheckout() }
                environment {
                  HOME = "${env.WORKSPACE}"
                  DOTNET_ROOT = "C:\\Program Files\\dotnet"
                  PATH = "${env.DOTNET_ROOT};${env.DOTNET_ROOT}\\tools;${env.PATH};${env.HOME}\\bin"
                  MSBUILDDEBUGPATH = "${env.WORKSPACE}"
                }
                stages{
                  /**
                  Install the required tools
                  */
                  stage('Install tools') {
                    steps {
                      cleanDir("${WORKSPACE}/*")
                      unstash 'source'
                      dir("${HOME}"){
                        powershell label: 'Install tools', script: "${BASE_DIR}\\.ci\\windows\\tools.ps1"
                      }
                    }

                  }
                  /**
                  Build the project from code..
                  */
                  stage('Build - dotnet') {
                    steps {
                      withGithubNotify(context: 'Build dotnet - Windows') {
                        retry(3) {
                          cleanDir("${WORKSPACE}/${BASE_DIR}")
                          unstash 'source'
                          dir("${BASE_DIR}"){
                            bat label: 'Build', script: '.ci/windows/dotnet.bat'
                          }
                        }
                      }
                    }
                    post {
                      unsuccessful {
                        archiveArtifacts(allowEmptyArchive: true,
                          artifacts: "${MSBUILDDEBUGPATH}/**/MSBuild_*.failure.txt")
                      }
                    }
                  }
                  /**
                  Execute unit tests.
                  */
                  stage('Test') {
                    steps {
                      withGithubNotify(context: 'Test dotnet - Windows', tab: 'tests') {
                        cleanDir("${WORKSPACE}/${BASE_DIR}")
                        unstash 'source'
                        dir("${BASE_DIR}"){
                          powershell label: 'Install test tools', script: '.ci\\windows\\test-tools.ps1'
                          retry(3) {
                            bat label: 'Build', script: '.ci/windows/dotnet.bat'
                          }
                          bat label: 'Test & coverage', script: '.ci/windows/test.bat'
                          powershell label: 'Convert Test Results to junit format', script: '.ci\\windows\\convert.ps1'
                        }
                      }
                    }
                    post {
                      always {
                        archiveArtifacts(allowEmptyArchive: true, artifacts: "${BASE_DIR}/target/diag.log,${BASE_DIR}/target/TestResults.xml")
                        junit(allowEmptyResults: true,
                          keepLongStdio: true,
                          testResults: "${BASE_DIR}/**/junit-*.xml,${BASE_DIR}/target/**/TEST-*.xml")
                      }
                      unsuccessful {
                        archiveArtifacts(allowEmptyArchive: true,
                          artifacts: "${MSBUILDDEBUGPATH}/**/MSBuild_*.failure.txt")
                      }
                    }
                  }
                }
                post {
                  always {
                    cleanWs(disableDeferredWipeout: true, notFailBuild: true)
                  }
                }
              }
            }
          }
          stage('Release to AppVeyor') {
            options { skipDefaultCheckout() }
            when {
              beforeAgent true
              anyOf {
                branch 'master'
                expression { return params.Run_As_Master_Branch }
              }
            }
            steps {
              withGithubNotify(context: 'Release AppVeyor', tab: 'artifacts') {
                deleteDir()
                unstash 'source'
                dir("${BASE_DIR}"){
                  release('secret/apm-team/ci/elastic-observability-appveyor')
                }
              }
            }
            post{
              success {
                archiveArtifacts(allowEmptyArchive: true,
                  artifacts: "${BASE_DIR}/**/bin/Release/**/*.nupkg")
              }
            }
          }
          stage('AfterRelease') {
            options {
              skipDefaultCheckout()
            }
            when {
              anyOf {
                tag pattern: '\\d+\\.\\d+\\.\\d+', comparator: 'REGEXP'
                expression { return params.Run_As_Master_Branch }
              }
            }
            stages {
              stage('Opbeans') {
                environment {
                  REPO_NAME = "${OPBEANS_REPO}"
                }
                steps {
                  deleteDir()
                  dir("${OPBEANS_REPO}"){
                    git credentialsId: 'f6c7695a-671e-4f4f-a331-acdce44ff9ba',
                        url: "git@github.com:elastic/${OPBEANS_REPO}.git"
                    sh script: ".ci/bump-version.sh ${env.BRANCH_NAME}", label: 'Bump version'
                    // The opbeans pipeline will trigger a release for the master branch
                    gitPush()
                    // The opbeans pipeline will trigger a release for the release tag
                    gitCreateTag(tag: "${env.BRANCH_NAME}")
                  }
                }
              }
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
                                 string(name: 'BUILD_OPTS', value: "--dotnet-agent-version ${env.GIT_BASE_COMMIT}"),
                                 string(name: 'GITHUB_CHECK_NAME', value: env.GITHUB_CHECK_ITS_NAME),
                                 string(name: 'GITHUB_CHECK_REPO', value: env.REPO),
                                 string(name: 'GITHUB_CHECK_SHA1', value: env.GIT_BASE_COMMIT)])
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
  def dockerTagName = 'docker.elastic.co/observability-ci/apm-agent-dotnet-sdk-linux:latest'
  sh label: 'Docker build', script: "docker build --tag ${dockerTagName} .ci/docker/sdk-linux"
  docker.image("${dockerTagName}").inside("-e HOME='${env.WORKSPACE}/${env.BASE_DIR}' -v /var/run/docker.sock:/var/run/docker.sock"){
    body()
  }
}

def dotnetWindows(Closure body){
  def dockerTagName = 'docker.elastic.co/observability-ci/apm-agent-dotnet-windows:latest'
  bat label: 'Docker Build', script: "docker build --tag ${dockerTagName}  -m 2GB .ci\\docker\\buildtools-windows"
  docker.image("${dockerTagName}").inside(){
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
