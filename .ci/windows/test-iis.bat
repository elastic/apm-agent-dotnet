set ELASTIC_APM_TESTS_FULL_FRAMEWORK_ENABLED=true
set sample_app_log_dir=%WORKSPACE%\SAMPLE_APP_LOG_DIR
if not exist "%sample_app_log_dir%" mkdir "%sample_app_log_dir%"
icacls %sample_app_log_dir% /t /grant Everyone:F
set ELASTIC_APM_ASP_NET_FULL_FRAMEWORK_SAMPLE_APP_LOG_FILE=%sample_app_log_dir%\Elastic.Apm.AspNetFullFramework.Tests.SampleApp.log

dotnet test test\Elastic.Apm.AspNetFullFramework.Tests --filter VerifyMetricsBasicConstraints -v n -r target -d target\diag.log --no-build
