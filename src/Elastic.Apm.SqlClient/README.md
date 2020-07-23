# Elastic.Apm.SqlClient

The project contains an auto instrumentation for `System.Data.SqlClient` and `Microsoft.Data.SqlClient` packages.
At the moment `Elastic.Apm.SqlClient` project has some limitations in depends on what you want to instrument.

In case of auto instrumentation for `System.Data.SqlClient`, both .NET Framework and .NET Core are supported, however, support of .NET Framework has one limitation: сommand text cannot be captured.

In case of auto instrumentation for `Microsoft.Data.SqlClient`, only .NET Core is supported.
