set ELASTIC_APM_TESTS_FULL_FRAMEWORK_ENABLED=true
set sample_app_log_dir=C:\Elastic_APM_TEMP
if not exist "%sample_app_log_dir%" mkdir "%sample_app_log_dir%"
icacls %sample_app_log_dir% /t /grant Everyone:F
set ELASTIC_APM_ASP_NET_FULL_FRAMEWORK_SAMPLE_APP_LOG_FILE=%sample_app_log_dir%\Elastic.Apm.AspNetFullFramework.Tests.SampleApp.log

dotnet test test\Elastic.Apm.AspNetFullFramework.Tests -v n -r target -d target\diag.log --no-build
