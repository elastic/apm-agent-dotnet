set ELASTIC_APM_TESTS_FULL_FRAMEWORK_ENABLED=true
set sample_app_log_dir=C:\Elastic_APM_TEMP
if not exist "%sample_app_log_dir%" mkdir "%sample_app_log_dir%"
icacls %sample_app_log_dir% /t /q /grant Everyone:F
set ELASTIC_APM_ASP_NET_FULL_FRAMEWORK_SAMPLE_APP_LOG_FILE=%sample_app_log_dir%\Elastic.Apm.AspNetFullFramework.Tests.SampleApp.log

REM enable permissions for the Application Pool Identity group
icacls %cd% /t /q /grant "IIS_IUSRS:(OI)(CI)(IO)(RX)"
REM enable permissions for the anonymous access group
icacls %cd% /t /q /grant "IUSR:(OI)(CI)(IO)(RX)"

dotnet test -c Release test\Elastic.Apm.AspNetFullFramework.Tests ^
 --verbosity normal ^
 --results-directory target ^
 --diag target\diag-iis.log ^
 --logger:"junit;LogFilePath=junit-{framework}-{assembly}.xml;MethodFormat=Class;FailureBodyFormat=Verbose"
