using System.Text;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests.Mocks;
using Elastic.Apm.Tests.TestHelpers;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;
// ReSharper disable InconsistentNaming, StringLiteralTypo

namespace Elastic.Apm.Tests.HelpersTests
{
	public class DbConnectionStringParserTests : LoggingTestBase
	{
		public DbConnectionStringParserTests(ITestOutputHelper xUnitOutputHelper) : base(xUnitOutputHelper) { }

		internal static void TestImpl(IApmLogger logger, string dbgDescription, string connectionString, string expectedHost
			, int? expectedPort
		)
		{
			var dbgInfo = $"connectionString: `{connectionString}'. dbgDescription: {dbgDescription}.";
			var parser = new DbConnectionStringParser(logger);
			var actualDestination = parser.ExtractDestination(connectionString);
			actualDestination.Should().NotBeNull(dbgInfo);
			actualDestination.Address.Should().Be(expectedHost, dbgInfo);
			actualDestination.Port.Should().Be(expectedPort, dbgInfo);
		}

		internal static void InvalidValueTestImpl(string dbgDescription, string connectionString, string invalidPart)
		{
			var mockLogger = new TestLogger(LogLevel.Trace);
			var dbgInfo = $"connectionString: `{connectionString}'. dbgDescription: {dbgDescription}.";
			var parser = new DbConnectionStringParser(mockLogger);
			parser.ExtractDestination(connectionString).Should().BeNull(dbgInfo);
			mockLogger.Lines.Should()
				.Contain(line =>
					line.Contains(nameof(DbConnectionStringParser))
					&& line.Contains(connectionString)
					&& line.Contains(invalidPart), dbgInfo);
		}

		public class Microsoft_SQL_Server : LoggingTestBase
		{
			public Microsoft_SQL_Server(ITestOutputHelper xUnitOutputHelper) : base(xUnitOutputHelper) { }

			[Theory]
			[InlineData("Standard Security"
				, @"Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword;"
				, "myServerAddress", null)] // https://www.connectionstrings.com/sqlconnection/standard-security/
			[InlineData("Standard Security - aditional white space"
				, @"Server= myServerAddress ;Database=myDataBase;User Id=myUsername;Password=myPassword;"
				, "myServerAddress", null)] // https://www.connectionstrings.com/sqlconnection/standard-security/
			[InlineData("Standard Security - Server keyword has different case"
				, @"SERVER=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword;"
				, "myServerAddress", null)] // https://www.connectionstrings.com/sqlconnection/standard-security/
			[InlineData("Connection to a SQL Server instance"
				, @"Server=myServerName\myInstanceName;Database=myDataBase;User Id=myUsername;Password=myPassword;"
				, "myServerName", null)] // https://www.connectionstrings.com/sqlconnection/connection-to-a-sql-server-instance/
			[InlineData("Connection to a SQL Server instance - aditional white space"
				, @"Server= myServerName \ myInstanceName ;Database=myDataBase;User Id=myUsername;Password=myPassword;"
				, "myServerName", null)] // https://www.connectionstrings.com/sqlconnection/connection-to-a-sql-server-instance/
			[InlineData("Connect via an IPv4 address - without port"
				, @"Data Source=190.190.200.100;Network Library=DBMSSOCN;Initial Catalog=myDataBase;User ID=myUsername;Password=myPassword;"
				, "190.190.200.100", null)] // https://www.connectionstrings.com/sqlconnection/connect-via-an-ip-address/
			[InlineData("Connect via an IPv4 address - without port - aditional white space"
				, @"Data Source= 190.190.200.100\t;Network Library=DBMSSOCN;Initial Catalog=myDataBase;User ID=myUsername;Password=myPassword;"
				, "190.190.200.100", null)] // https://www.connectionstrings.com/sqlconnection/connect-via-an-ip-address/
			[InlineData("Connect via an IPv4 address - with port"
				, @"Data Source=190.190.200.100,1433;Network Library=DBMSSOCN;Initial Catalog=myDataBase;User ID=myUsername;Password=myPassword;"
				, "190.190.200.100", 1433)] // https://www.connectionstrings.com/sqlconnection/connect-via-an-ip-address/
			[InlineData("Connect via an IPv4 address - with port - additional white space"
				, @"Data Source= 190.190.200.100 , 1433 ;Network Library=DBMSSOCN;Initial Catalog=myDataBase;User ID=myUsername;Password=myPassword;"
				, "190.190.200.100", 1433)] // https://www.connectionstrings.com/sqlconnection/connect-via-an-ip-address/
			[InlineData("Connect via an IPv6 address - without port"
				, @"Data Source=2012:b86a:f950::b86a:f950;Network Library=DBMSSOCN;Initial Catalog=myDataBase;User ID=myUsername;Password=myPassword;"
				, "2012:b86a:f950::b86a:f950", null)] // https://www.connectionstrings.com/sqlconnection/connect-via-an-ip-address/
			[InlineData("Connect via an IPv6 address - without port - additional white space"
				, @"Data Source= 2012:b86a:f950::b86a:f950 ;Network Library=DBMSSOCN;Initial Catalog=myDataBase;User ID=myUsername;Password=myPassword;"
				, "2012:b86a:f950::b86a:f950", null)] // https://www.connectionstrings.com/sqlconnection/connect-via-an-ip-address/
			[InlineData("Connect via an IPv6 address - without port - in square brackets"
				, @"Data Source=[2012:b86a:f950::b86a:f950];Network Library=DBMSSOCN;Initial Catalog=myDataBase;User ID=myUsername;Password=myPassword;"
				, "2012:b86a:f950::b86a:f950", null)] // https://www.connectionstrings.com/sqlconnection/connect-via-an-ip-address/
			[InlineData("Connect via an IPv6 address - without port - in square brackets - additional white space"
				, @"Data Source= [ 2012:b86a:f950::b86a:f950 ] ;Network Library=DBMSSOCN;Initial Catalog=myDataBase;User ID=myUsername;Password=myPassword;"
				, "2012:b86a:f950::b86a:f950", null)] // https://www.connectionstrings.com/sqlconnection/connect-via-an-ip-address/
			[InlineData("Connect via an IPv6 address - with port"
				, @"Data Source=2012:b86a:f950::b86a:f950,9876;Network Library=DBMSSOCN;Initial Catalog=myDataBase;User ID=myUsername;Password=myPassword;"
				, "2012:b86a:f950::b86a:f950", 9876)] // https://www.connectionstrings.com/sqlconnection/connect-via-an-ip-address/
			[InlineData("Connect via an IPv6 address - with port - additional white space"
				, @"Data Source= 2012:b86a:f950::b86a:f950 , 9876 ;Network Library=DBMSSOCN;Initial Catalog=myDataBase;User ID=myUsername;Password=myPassword;"
				, "2012:b86a:f950::b86a:f950", 9876)] // https://www.connectionstrings.com/sqlconnection/connect-via-an-ip-address/
			[InlineData("Connect via an IPv6 address - with port - in square brackets"
				, @"Data Source=[2012:b86a:f950::b86a:f950],4567;Network Library=DBMSSOCN;Initial Catalog=myDataBase;User ID=myUsername;Password=myPassword;"
				, "2012:b86a:f950::b86a:f950", 4567)] // https://www.connectionstrings.com/sqlconnection/connect-via-an-ip-address/
			[InlineData("Connect via an IPv6 address - with port - in square brackets - additional white space"
				, @"Data Source= [ 2012:b86a:f950::b86a:f950 ] , 4567;Network Library=DBMSSOCN;Initial Catalog=myDataBase;User ID=myUsername;Password=myPassword;"
				, "2012:b86a:f950::b86a:f950", 4567)] // https://www.connectionstrings.com/sqlconnection/connect-via-an-ip-address/
			[InlineData("Trusted Connection from a CE device"
				, @"Data Source=myServerAddress;Initial Catalog=myDataBase;Integrated Security=SSPI;User ID=myDomain\myUsername;Password=myPassword;"
				, "myServerAddress", null)] // https://www.connectionstrings.com/sqlconnection/trusted-connection-from-a-ce-device/
			public void SQL_Server_2016(string dbgDescription, string connectionString, string expectedHost, int? expectedPort)
				=> TestImpl(LoggerBase, dbgDescription, connectionString, expectedHost, expectedPort);

			[Theory]
			[InlineData("LocalDB automatic instance"
				, @"Server=(localdb)\v11.0;Integrated Security=true;"
				, "localhost", null)] // https://www.connectionstrings.com/sqlconnection/localdb-automatic-instance/
			[InlineData("LocalDB named instance"
				, @"Server=(localdb)\MyInstance;Integrated Security=true;"
				, "localhost", null)] // https://www.connectionstrings.com/sqlconnection/localdb-named-instance/
			[InlineData("LocalDB shared instance"
				, @"Server=(localdb)\.\MyInstanceShare;Integrated Security=true;"
				, "localhost", null)] // https://www.connectionstrings.com/sqlconnection/localdb-shared-instance/
			public void LocalDB(string dbgDescription, string connectionString, string expectedHost, int? expectedPort)
				=> TestImpl(LoggerBase, dbgDescription, connectionString, expectedHost, expectedPort);

			[Theory]
			[InlineData("Using an User Instance on a local SQL Server Express instance"
				, @"Data Source=.\SQLExpress;Integrated Security=true;AttachDbFilename=C:\MyFolder\MyDataFile.mdf;User Instance=true;"
				, "localhost", null)] // https://www.connectionstrings.com/sqlconnection/using-an-user-instance-on-a-local-sql-server-express-instance/
			public void local_user_instance(string dbgDescription, string connectionString, string expectedHost, int? expectedPort)
				=> TestImpl(LoggerBase, dbgDescription, connectionString, expectedHost, expectedPort);

			[Theory]
			[InlineData("Standard"
				, @"Server=tcp:myServerName;Database=myDataBase;User ID=[LoginForDb]@[serverName];Password=myPassword;Trusted_Connection=False;Encrypt=True;"
				, "myServerName",
				null)] // https://www.connectionstrings.com/sqlconnection/standard/
			[InlineData("SQLNCLI11 OLEDB / Standard"
				, @"Provider=SQLNCLI11;Password=myPassword;User ID=[username]@[servername];Initial Catalog=databasename;Data Source=myServerName;"
				, "myServerName",
				null)] // https://www.connectionstrings.com/sql-server-native-client-11-0-oledb-provider/standard/
			[InlineData("SQL Server Native Client 10.0 ODBC / Standard security Azure"
				, @"Driver={SQL Server Native Client 10.0};Server=tcp:myServerName;Uid=[LoginForDb]@[serverName];Pwd=myPassword;Encrypt=yes;"
				, "myServerName",
				null)] // https://www.connectionstrings.com/sql-server-native-client-10-0-odbc-driver/standard-security-azure/
			public void SQL_Azure(string dbgDescription, string connectionString, string expectedHost, int? expectedPort)
				=> TestImpl(LoggerBase, dbgDescription, connectionString, expectedHost, expectedPort);

			[Theory]
			[InlineData("Connect via an IP address with invalid port - letters instead of digits"
				, @"Data Source=190.190.200.100,abc;Network Library=DBMSSOCN;Initial Catalog=myDataBase;User ID=myUsername;Password=myPassword;"
				, "abc")] // https://www.connectionstrings.com/sqlconnection/connect-via-an-ip-address/
			[InlineData("Connect via an IP address with invalid port - negative integer"
				, @"Data Source=190.190.200.100,-1234;Network Library=DBMSSOCN;Initial Catalog=myDataBase;User ID=myUsername;Password=myPassword;"
				, "-1234")] // https://www.connectionstrings.com/sqlconnection/connect-via-an-ip-address/
			[InlineData("Connect via an IP address with invalid port - empty string"
				, @"Data Source=190.190.200.100,;Network Library=DBMSSOCN;Initial Catalog=myDataBase;User ID=myUsername;Password=myPassword;"
				, "")] // https://www.connectionstrings.com/sqlconnection/connect-via-an-ip-address/
			[InlineData("Connect via an IPv6 address with invalid port - letters instead of digits"
				, @"Data Source=2012:b86a:f950::b86a:f950,abc;Network Library=DBMSSOCN;Initial Catalog=myDataBase;User ID=myUsername;Password=myPassword;"
				, "abc")] // https://www.connectionstrings.com/sqlconnection/connect-via-an-ip-address/
			[InlineData("Connect via an IPv6 address with invalid port - negative integer"
				, @"Data Source=2012:b86a:f950::b86a:f950,-1234;Network Library=DBMSSOCN;Initial Catalog=myDataBase;User ID=myUsername;Password=myPassword;"
				, "-1234")] // https://www.connectionstrings.com/sqlconnection/connect-via-an-ip-address/
			[InlineData("Connect via an IPv6 address with invalid port - empty string"
				, @"Data Source=2012:b86a:f950::b86a:f950,;Network Library=DBMSSOCN;Initial Catalog=myDataBase;User ID=myUsername;Password=myPassword;"
				, "")] // https://www.connectionstrings.com/sqlconnection/connect-via-an-ip-address/
			[InlineData("Connect via an IPv6 address in square brackets with invalid port - letters instead of digits"
				, @"Data Source=[2012:b86a:f950::b86a:f950],abc;Network Library=DBMSSOCN;Initial Catalog=myDataBase;User ID=myUsername;Password=myPassword;"
				, "abc")] // https://www.connectionstrings.com/sqlconnection/connect-via-an-ip-address/
			[InlineData("Connect via an IPv6 address in square brackets with invalid port - negative integer"
				, @"Data Source=[2012:b86a:f950::b86a:f950],-1234;Network Library=DBMSSOCN;Initial Catalog=myDataBase;User ID=myUsername;Password=myPassword;"
				, "-1234")] // https://www.connectionstrings.com/sqlconnection/connect-via-an-ip-address/
			[InlineData("Connect via an IPv6 address in square brackets with invalid port - empty string"
				, @"Data Source=[2012:b86a:f950::b86a:f950],;Network Library=DBMSSOCN;Initial Catalog=myDataBase;User ID=myUsername;Password=myPassword;"
				, "Data Source=[2012:b86a:f950::b86a:f950],")] // https://www.connectionstrings.com/sqlconnection/connect-via-an-ip-address/
			public void invalid_port(string dbgDescription, string connectionString, string invalidPort)
				=> InvalidValueTestImpl(dbgDescription, connectionString, invalidPort);

			[Theory]
			[InlineData("Empty server address part with port"
				, @"Data Source=,12345;Network Library=DBMSSOCN;Initial Catalog=myDataBase;User ID=myUsername;Password=myPassword;"
				, "Data Source=,12345")] // https://www.connectionstrings.com/sqlconnection/connect-via-an-ip-address/
			[InlineData("Empty server address part without port"
				, @"Data Source=;Network Library=DBMSSOCN;Initial Catalog=myDataBase;User ID=myUsername;Password=myPassword;"
				, "Data Source=")] // https://www.connectionstrings.com/sqlconnection/connect-via-an-ip-address/
			public void invalid_empty_server(string dbgDescription, string connectionString, string invalidPart)
				=> InvalidValueTestImpl(dbgDescription, connectionString, invalidPart);
		}

		public class Oracle : LoggingTestBase
		{
			public Oracle(ITestOutputHelper xUnitOutputHelper) : base(xUnitOutputHelper) { }

			[Theory]
			[InlineData("Specifying username and password"
				, @"Data Source=MyOracleDB;User Id=myUsername;Password=myPassword;Integrated Security=no;"
				, "MyOracleDB", null)] // https://www.connectionstrings.com/sqlconnection/standard-security/
			public void dotNET_Data_Provider(string dbgDescription, string connectionString, string expectedHost, int? expectedPort)
				=> TestImpl(LoggerBase, dbgDescription, connectionString, expectedHost, expectedPort);

			[Theory]
			[InlineData("Standard"
				, @"User ID=myUsername;Password=myPassword;Host=myServerName;Pooling=true;Min Pool Size=0;Max Pool Size=100;Connection Lifetime=0;"
				, "myServerName", null)] // https://www.connectionstrings.com/dotconnect-for-oracle/standard/
			public void Devart_dotConnect(string dbgDescription, string connectionString, string expectedHost, int? expectedPort)
				=> TestImpl(LoggerBase, dbgDescription, connectionString, expectedHost, expectedPort);

			[Theory]
			[InlineData("From issue #791 - Oracle [with Devart provider]"
				, @"Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=172.21.25.186)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=ORCLCDB)));User Id=SAJDDL; Direct=True;"
				, "172.21.25.186", 1521)] // https://github.com/elastic/apm-agent-dotnet/issues/791
			[InlineData("Oracle Data Provider for .NET / ODP.NET"
				, @"Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST=MyHost)(PORT=4321)))(CONNECT_DATA=(SERVER=DEDICATED)(SERVICE_NAME=MyOracleSID)));User Id=myUsername;Password=myPassword;"
				, "MyHost", 4321)] // https://www.connectionstrings.com/oracle-data-provider-for-net-odp-net/using-odpnet-without-tnsnamesora/
			[InlineData("Oracle Data Provider for .NET / ODP.NET - multiple addresses"
				, @"Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST=MyHost1)(PORT=1))(ADDRESS=(PROTOCOL=TCP)(HOST=MyHost2)(PORT=22)))(CONNECT_DATA=(SERVER=DEDICATED)(SERVICE_NAME=MyOracleSID)));User Id=myUsername;Password=myPassword;"
				, "MyHost1", 1)] // https://www.connectionstrings.com/oracle-data-provider-for-net-odp-net/using-odpnet-without-tnsnamesora/
			[InlineData("Reported in a GitHub Issue"
				, @"Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=DATABASENAME)(PORT=1521))(CONNECT_DATA=(SERVER=DEDICATED)(SERVICE_NAME=DATABASESERVICENAME))); User ID=USERNAME; password=PASSWORD; Pooling=False;"
				, "DATABASENAME", 1521)] // https://github.com/elastic/apm-agent-dotnet/issues/796#issuecomment-609668197
			public void nested_value(string dbgDescription, string connectionString, string expectedHost, int? expectedPort)
				=> TestImpl(LoggerBase, dbgDescription, connectionString, expectedHost, expectedPort);

			[Theory]
			[InlineData(DbConnectionStringParser.MaxNestingDepth/2, true)]
			[InlineData(DbConnectionStringParser.MaxNestingDepth-1, true)]
			[InlineData(DbConnectionStringParser.MaxNestingDepth, true)]
			[InlineData(DbConnectionStringParser.MaxNestingDepth+1, false)]
			[InlineData(DbConnectionStringParser.MaxNestingDepth*2, false)]
			public void nested_value_max_depth(int nestingDepth, bool isValid)
			{
				// @"Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=172.21.25.186)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=ORCLCDB)));User Id=SAJDDL; Direct=True;"
				const string expectedHost = "2012:b86a:f950::b86a:f950";
				const int expectedPort = 9876;

				// -2 because inner part already has nesting depth of 2
				var connectionString = BuildString(nestingDepth - 2);
				var dbgDescription = $"nestingDepth: {nestingDepth}";
				if (isValid)
					TestImpl(LoggerBase, dbgDescription, connectionString, expectedHost, expectedPort);
				else
					InvalidValueTestImpl(dbgDescription, connectionString, "");

				string BuildString(int outerNestingDepth)
				{
					return
						"Data Source=" +
						$"(dummy_key_with_over_max_nesting={BuildNestedPart(DbConnectionStringParser.MaxNestingDepth, "(ADDRESS=(HOST=dummy))", 'D')})" +
						BuildNestedPart(outerNestingDepth, $"(ADDRESS=(PROTOCOL=TCP)(HOST={expectedHost})(PORT={expectedPort}))", 'K') +
						"(CONNECT_DATA=(SERVICE_NAME=ORCLCDB)));User Id=SAJDDL; Direct=True;";
				}

				string BuildNestedPart(int outerNestingDepth, string innerPart, char nestingKey)
				{
					var strBuilder = new StringBuilder(nestingDepth * 6);
					for (var i = 0; i < outerNestingDepth; ++i) strBuilder.Append($"({nestingKey}{i+1}=");
					strBuilder.Append(innerPart);
					for (var i = 0; i < outerNestingDepth; ++i) strBuilder.Append(')');
					return strBuilder.ToString();
				}
			}

			[Theory]
			[InlineData("Multiple values - the first one with all mandatory parts wins"
				, @"Data Source=(ADDRESS_LIST=(ADDRESS=(PORT=1))(ADDRESS=(HOST=host_2)(PORT=2))(ADDRESS=(HOST=host_3)(PORT=3)))"
				, "host_2", 2)]
			public void multiple_nested_values(string dbgDescription, string connectionString, string expectedHost, int? expectedPort)
				=> TestImpl(LoggerBase, dbgDescription, connectionString, expectedHost, expectedPort);

			[Theory]
			[InlineData("No address (which is mandatory) - just port"
				, @"Data Source=(ADDRESS_LIST=(ADDRESS=(PORT=1))(ADDRESS=(not_host=xyz)(PORT=2))(ADDRESS=(not_host=xyz)(PORT=3)))"
				, "mandatory")]
			public void nested_values_without_mandatory_parts(string dbgDescription, string connectionString, string invalidPart)
				=> InvalidValueTestImpl(dbgDescription, connectionString, invalidPart);

			[Theory]
			[InlineData("Standard security"
				, @"Provider=msdaora;Data Source=MyOracleDB;User Id=myUsername;Password=myPassword;"
				, "MyOracleDB", null)] // https://www.connectionstrings.com/microsoft-ole-db-provider-for-oracle-msdaora/standard-security/
			[InlineData("Trusted connection"
				, @"Provider=msdaora;Data Source=MyOracleDB;Persist Security Info=False;Integrated Security=Yes;"
				, "MyOracleDB", null)] // https://www.connectionstrings.com/microsoft-ole-db-provider-for-oracle-msdaora/trusted-connection/
			public void OLE_DB_Provider(string dbgDescription, string connectionString, string expectedHost, int? expectedPort)
				=> TestImpl(LoggerBase, dbgDescription, connectionString, expectedHost, expectedPort);

			[Theory]
			[InlineData("New version"
				, @"Driver={Microsoft ODBC for Oracle};Server=myServerAddress;Uid=myUsername;Pwd=myPassword;"
				, "myServerAddress", null)] // https://www.connectionstrings.com/microsoft-odbc-for-oracle/new-version/
			public void ODBC(string dbgDescription, string connectionString, string expectedHost, int? expectedPort)
				=> TestImpl(LoggerBase, dbgDescription, connectionString, expectedHost, expectedPort);

			[Theory]
			[InlineData("Standard / IPv4 without port"
				, @"Driver=(Oracle in XEClient);dbq=111.21.31.99/XE;Uid=myUsername;Pwd=myPassword;"
				, "111.21.31.99", null)] // https://www.connectionstrings.com/oracle-in-xeclient/standard/
			[InlineData("Standard / IPv4 with port"
				, @"Driver=(Oracle in XEClient);dbq=111.21.31.99:4321/XE;Uid=myUsername;Pwd=myPassword;"
				, "111.21.31.99", 4321)] // https://www.connectionstrings.com/oracle-in-xeclient/standard/
			[InlineData("Standard / IPv6 without port"
				, @"Driver=(Oracle in XEClient);dbq=[2012:b86a:f950::b86a:f950]/XE;Uid=myUsername;Pwd=myPassword;"
				, "2012:b86a:f950::b86a:f950", null)] // https://www.connectionstrings.com/oracle-in-xeclient/standard/
			[InlineData("Standard / IPv6 with port"
				, @"Driver=(Oracle in XEClient);dbq=[2012:b86a:f950::b86a:f950]:4321/XE;Uid=myUsername;Pwd=myPassword;"
				, "2012:b86a:f950::b86a:f950", 4321)] // https://www.connectionstrings.com/oracle-in-xeclient/standard/
			public void XEClient(string dbgDescription, string connectionString, string expectedHost, int? expectedPort)
				=> TestImpl(LoggerBase, dbgDescription, connectionString, expectedHost, expectedPort);

			[Theory]
			// https://github.com/elastic/apm-agent-dotnet/issues/746
			[InlineData("Issue #746"
				, @"DATA SOURCE=192.168.0.151:1521/ORCL;PASSWORD=xxx;PERSIST SECURITY INFO=True;USER ID=xxx"
				, "192.168.0.151", 1521)]
			[InlineData("Issue #746 with dummy suffix"
				, @"DATA SOURCE=192.168.0.151:1521/MYSUFFIX;PASSWORD=xxx;PERSIST SECURITY INFO=True;USER ID=xxx"
				, "192.168.0.151", 1521)]
			[InlineData("Issue #746 with IPv6 with port"
				, @"DATA SOURCE=[ff02::2:ff00:0]:1521/ORCL;PASSWORD=xxx;PERSIST SECURITY INFO=True;USER ID=xxx"
				, "ff02::2:ff00:0", 1521)]
			[InlineData("Issue #746 with IPv6 without port address enclosed in []"
				, @"DATA SOURCE=[ff02::2:ff00:0]/ORCL;PASSWORD=xxx;PERSIST SECURITY INFO=True;USER ID=xxx"
				, "ff02::2:ff00:0", null)]
			[InlineData("Issue #746 with IPv6 without port address not enclosed in []"
				, @"DATA SOURCE=ff02::2:ff00:0/ORCL;PASSWORD=xxx;PERSIST SECURITY INFO=True;USER ID=xxx"
				, "ff02::2:ff00:0", null)]
			// According to https://en.wikipedia.org/wiki/IPv6_address#Special_addresses
			// IPv6 can contain `/<hex number>' and we don't want to discard a part of the address
			[InlineData("Issue #746 but with IPv6 with slash with port"
				, @"DATA SOURCE=[ff02::2:ff00:0/104]:1521/ORCL;PASSWORD=xxx;PERSIST SECURITY INFO=True;USER ID=xxx"
				, "ff02::2:ff00:0/104", 1521)]
			[InlineData("Issue #746 but with IPv6 with slash without port address enclosed in []"
				, @"DATA SOURCE=[ff02::2:ff00:0/104]/ORCL;PASSWORD=xxx;PERSIST SECURITY INFO=True;USER ID=xxx"
				, "ff02::2:ff00:0/104", null)]
			[InlineData("Issue #746 but with IPv6, with slash without port address not enclosed in []"
				, @"DATA SOURCE=ff02::2:ff00:0/104/ORCL;PASSWORD=xxx;PERSIST SECURITY INFO=True;USER ID=xxx"
				, "ff02::2:ff00:0/104", null)]
			[InlineData("IPv6 with slash in the address part without port"
				, @"DATA SOURCE=ff02::2:ff00:0/104;PASSWORD=xxx;PERSIST SECURITY INFO=True;USER ID=xxx"
				, "ff02::2:ff00:0/104", null)]
			[InlineData("IPv6 with slash in the address part with port"
				, @"DATA SOURCE=[ff02::2:ff00:0/104]:1521;PASSWORD=xxx;PERSIST SECURITY INFO=True;USER ID=xxx"
				, "ff02::2:ff00:0/104", 1521)]
			public void issue_746_discardable_ORCL_suffix(string dbgDescription, string connectionString, string expectedHost, int? expectedPort)
				=> TestImpl(LoggerBase, dbgDescription, connectionString, expectedHost, expectedPort);

			[Theory]
			[InlineData("Standard / IPv4 with invalid port - letters instead of digits"
				, @"Driver=(Oracle in XEClient);dbq=111.21.31.99:abc/XE;Uid=myUsername;Pwd=myPassword;"
				, "abc")] // https://www.connectionstrings.com/oracle-in-xeclient/standard/
			[InlineData("Standard / IPv4 with invalid port - negative integer"
				, @"Driver=(Oracle in XEClient);dbq=111.21.31.99:-6543/XE;Uid=myUsername;Pwd=myPassword;"
				, "-6543")] // https://www.connectionstrings.com/oracle-in-xeclient/standard/
			[InlineData("Standard / IPv4 with invalid port - empty string"
				, @"Driver=(Oracle in XEClient);dbq=111.21.31.99:/XE;Uid=myUsername;Pwd=myPassword;"
				, "")] // https://www.connectionstrings.com/oracle-in-xeclient/standard/
			[InlineData("Standard / IPv6 with invalid port - letters instead of digits"
				, @"Driver=(Oracle in XEClient);dbq=[2012:b86a:f950::b86a:f950]:abc/XE;Uid=myUsername;Pwd=myPassword;"
				, "abc")] // https://www.connectionstrings.com/oracle-in-xeclient/standard/
			[InlineData("Standard / IPv6 with invalid port - negative integer"
				, @"Driver=(Oracle in XEClient);dbq=[2012:b86a:f950::b86a:f950]:-6543/XE;Uid=myUsername;Pwd=myPassword;"
				, "-6543")] // https://www.connectionstrings.com/oracle-in-xeclient/standard/
			[InlineData("Standard / IPv6 with invalid port - empty string"
				, @"Driver=(Oracle in XEClient);dbq=[2012:b86a:f950::b86a:f950]:/XE;Uid=myUsername;Pwd=myPassword;"
				, "")] // https://www.connectionstrings.com/oracle-in-xeclient/standard/
			public void invalid_port(string dbgDescription, string connectionString, string invalidPort)
				=> InvalidValueTestImpl(dbgDescription, connectionString, invalidPort);

			[Theory]
			[InlineData("Empty server address"
				, @"Driver=(Oracle in XEClient);dbq=:9821/XE;Uid=myUsername;Pwd=myPassword;"
				, "dbq=:9821/XE")] // https://www.connectionstrings.com/oracle-in-xeclient/standard/
			public void invalid_empty_server(string dbgDescription, string connectionString, string invalidPort)
				=> InvalidValueTestImpl(dbgDescription, connectionString, invalidPort);
		}

		public class PostgreSQL : LoggingTestBase
		{
			public PostgreSQL(ITestOutputHelper xUnitOutputHelper) : base(xUnitOutputHelper) { }

			[Theory]
			[InlineData("Standard - without port"
				, @"User ID=root;Password=myPassword;Host=myServerName;Database=myDataBase;Pooling=true;Min Pool Size=0;Max Pool Size=100;Connection Lifetime=0;"
				, "myServerName", null)] // https://www.connectionstrings.com/dotconnect-for-postgresql/standard/
			[InlineData("Standard - with port"
				, @"User ID=root;Password=myPassword;Host=myServerName;Port=1234;Database=myDataBase;Pooling=true;Min Pool Size=0;Max Pool Size=100;Connection Lifetime=0;"
				, "myServerName", 1234)] // https://www.connectionstrings.com/dotconnect-for-postgresql/standard/
			[InlineData("Standard - with port - additional white space"
				, @"User ID=root;Password=myPassword;Host=myServerName;Port= 1234 ;Database=myDataBase;Pooling=true;Min Pool Size=0;Max Pool Size=100;Connection Lifetime=0;"
				, "myServerName", 1234)] // https://www.connectionstrings.com/dotconnect-for-postgresql/standard/
			public void Devart_dotConnect(string dbgDescription, string connectionString, string expectedHost, int? expectedPort)
				=> TestImpl(LoggerBase, dbgDescription, connectionString, expectedHost, expectedPort);

			[Theory]
			[InlineData("Standard"
				, @"Provider=PostgreSQL OLE DB Provider;Data Source=myServerAddress;location=myDataBase;User ID=myUsername;password=myPassword;timeout=1000;"
				, "myServerAddress", null)] // https://www.connectionstrings.com/dotconnect-for-postgresql/standard/
			public void OLE_DB(string dbgDescription, string connectionString, string expectedHost, int? expectedPort)
				=> TestImpl(LoggerBase, dbgDescription, connectionString, expectedHost, expectedPort);

			[Theory]
			[InlineData("Standard - without port"
				, @"Driver={PostgreSQL};Server=myServerName;Database=myDataBase;Uid=myUsername;Pwd=myPassword;"
				, "myServerName", null)] // https://www.connectionstrings.com/postgresql-odbc-driver-psqlodbc/standard/
			[InlineData("Standard - with port"
				, @"Driver={PostgreSQL};Server=myServerName;Port=1234;Database=myDataBase;Uid=myUsername;Pwd=myPassword;"
				, "myServerName", 1234)] // https://www.connectionstrings.com/postgresql-odbc-driver-psqlodbc/standard/
			public void ODBC(string dbgDescription, string connectionString, string expectedHost, int? expectedPort)
				=> TestImpl(LoggerBase, dbgDescription, connectionString, expectedHost, expectedPort);

		}

		public class MySQL : LoggingTestBase
		{
			public MySQL(ITestOutputHelper xUnitOutputHelper) : base(xUnitOutputHelper) { }

			[Theory]
			[InlineData("Standard"
				, @"Server=myServerAddress;Database=myDataBase;Uid=myUsername;Pwd=myPassword;"
				, "myServerAddress", null)] // https://www.connectionstrings.com/mysql-connector-net-mysqlconnection/standard/
			[InlineData("Specifying TCP port"
				, @"Server=myServerAddress;Port=1234;Database=myDataBase;Uid=myUsername;Pwd=myPassword;"
				, "myServerAddress", 1234)] // https://www.connectionstrings.com/mysql-connector-net-mysqlconnection/specifying-tcp-port/
			[InlineData("MySql.Data.EntityFrameworkCore 8.0.18"
				, @"server=myServerAddress;database=library;user=TestUser;password=w7nwGF%yjfvi&3YyZcTh"
				, "myServerAddress", null)] // https://www.connectionstrings.com/mysql-connector-net-mysqlconnection/specifying-tcp-port/
			public void dotNET_Connector(string dbgDescription, string connectionString, string expectedHost, int? expectedPort)
				=> TestImpl(LoggerBase, dbgDescription, connectionString, expectedHost, expectedPort);
		}

		public class DB2 : LoggingTestBase
		{
			public DB2(ITestOutputHelper xUnitOutputHelper) : base(xUnitOutputHelper) { }

			[Theory]
			[InlineData("Standard"
				, @"Driver={IBM DB2 ODBC DRIVER};Database=myDataBase;Hostname=myServerAddress;Port=1234;Protocol=TCPIP;Uid=myUsername;Pwd=myPassword;"
				, "myServerAddress", 1234)] // https://www.connectionstrings.com/mysql-connector-net-mysqlconnection/standard/
			public void ODBC(string dbgDescription, string connectionString, string expectedHost, int? expectedPort)
				=> TestImpl(LoggerBase, dbgDescription, connectionString, expectedHost, expectedPort);

			[Theory]
			[InlineData("Standard"
				, @"Server=myServerAddress:1234;Database=myDataBase;UID=myUsername;PWD=myPassword;"
				, "myServerAddress", 1234)] // https://www.connectionstrings.com/mysql-connector-net-mysqlconnection/standard/
			public void dotNET_Data_Provider(string dbgDescription, string connectionString, string expectedHost, int? expectedPort)
				=> TestImpl(LoggerBase, dbgDescription, connectionString, expectedHost, expectedPort);

			[Theory]
			[InlineData("TCP/IP"
				, @"Provider=DB2OLEDB;Network Transport Library=TCPIP;Network Address=11.22.33.44;Initial Catalog=MyCtlg;Package Collection=MyPkgCol;Default Schema=Schema;User ID=myUsername;Password=myPassword;"
				, "11.22.33.44", null)] // https://www.connectionstrings.com/mysql-connector-net-mysqlconnection/standard/
			public void OLE_DB(string dbgDescription, string connectionString, string expectedHost, int? expectedPort)
				=> TestImpl(LoggerBase, dbgDescription, connectionString, expectedHost, expectedPort);
		}

		[Fact]
		public void positive_cache()
		{
			const string connectionString = @"Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword";
			var parser = new DbConnectionStringParser(LoggerBase);
			var actualDestination = parser.ExtractDestination(connectionString, out var wasFoundInCache);
			wasFoundInCache.Should().BeFalse();
			actualDestination.Should().NotBeNull();
			actualDestination.Address.Should().Be("myServerAddress");
			actualDestination.Port.Should().BeNull();
			var actualDestination2 = parser.ExtractDestination(connectionString, out var wasFoundInCache2);
			wasFoundInCache2.Should().BeTrue();
			actualDestination2.Should().Be(actualDestination);
		}

		[Fact]
		public void negative_cache()
		{
			const string connectionString = "";
			var parser = new DbConnectionStringParser(LoggerBase);
			var actualDestination = parser.ExtractDestination(connectionString, out var wasFoundInCache);
			wasFoundInCache.Should().BeFalse();
			actualDestination.Should().BeNull();
			var actualDestination2 = parser.ExtractDestination(connectionString, out var wasFoundInCache2);
			wasFoundInCache2.Should().BeTrue();
			actualDestination2.Should().BeNull();
		}

		[Fact]
		public void caches_only_first_MaxCacheSize_results()
		{
			const string validConnectionStringPrefix = @"Server=myServerAddress;Port=";
			var parser = new DbConnectionStringParser(LoggerBase);
			DbConnectionStringParser.MaxCacheSize.Repeat(i => { VerifyForIndex(i, /* isFirstTime: */ true); });
			DbConnectionStringParser.MaxCacheSize.Repeat(i => { VerifyForIndex(i, /* isFirstTime: */ false); });
			10.Repeat(i => { VerifyForIndex(DbConnectionStringParser.MaxCacheSize + i, /* isFirstTime: */ true); });
			10.Repeat(i => { VerifyForIndex(DbConnectionStringParser.MaxCacheSize + i, /* isFirstTime: */ false); });

			void VerifyForIndex(int index, bool isFirstTime)
			{
				var isValidConnectionString = index % 2 == 0;
				var port = 10000 + index;
				var connectionString = isValidConnectionString ? validConnectionStringPrefix + (port) : $"{index}";
				var actualDestination = parser.ExtractDestination(connectionString, out var wasFoundInCache);
				wasFoundInCache.Should().Be(!isFirstTime && index < DbConnectionStringParser.MaxCacheSize);
				if (isValidConnectionString)
				{
					actualDestination.Should().NotBeNull();
					actualDestination.Address.Should().Be("myServerAddress");
					actualDestination.Port.Should().Be(port);
				}
				else
					actualDestination.Should().BeNull();
			}
		}
	}
}
