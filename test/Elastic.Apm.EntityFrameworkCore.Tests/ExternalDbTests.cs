using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Elastic.Apm.Api;
using Elastic.Apm.Helpers;
using Elastic.Apm.Model;
using Elastic.Apm.Tests.Extensions;
using Elastic.Apm.Tests.Mocks;
using Elastic.Apm.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.EntityFrameworkCore.Tests
{
	/// <summary>
	/// Tests using external DB servers.
	/// Tests will not run (even though they will show as passed) if any of the following environment variables is not set:
	///  	- ELASTIC_APM_TESTS_XYZ_HOST
	/// 			Note: The value should contain only the host name i.e., without DB instance name - the test uses default DB instance
	///  	- ELASTIC_APM_TESTS_XYZ_USERNAME
	///  	- ELASTIC_APM_TESTS_XYZ_PASSWORD
	/// where XYZ is database type.
	/// For MySQL the expected environment variables are:
	///  	- ELASTIC_APM_TESTS_MYSQL_HOST
	///  	- ELASTIC_APM_TESTS_MYSQL_USERNAME
	///  	- ELASTIC_APM_TESTS_MYSQL_PASSWORD
	/// For Microsoft SQL Server the expected environment variables are:
	///  	- ELASTIC_APM_TESTS_MS_SQL_HOST
	///  	- ELASTIC_APM_TESTS_MS_SQL_USERNAME
	///  	- ELASTIC_APM_TESTS_MS_SQL_PASSWORD
	/// </summary>
	public class ExternalDbTests : LoggingTestBase
	{
		private static readonly IDictionary<string, Action<ConnectionDetails, string>> EnvVarSuffixToConnectionProperty =
			new Dictionary<string, Action<ConnectionDetails, string>>
			{
				{ "HOST", (connectionDetails, envVarValue) => { connectionDetails.Host = envVarValue; } },
				{ "USERNAME", (connectionDetails, envVarValue) => { connectionDetails.Username = envVarValue; } },
				{ "PASSWORD", (connectionDetails, envVarValue) => { connectionDetails.Password = envVarValue; } }
			};

		private static readonly IReadOnlyList<ExternalDbType> PotentialExternalDbTypes = new List<ExternalDbType>
		{
			new ExternalDbType
			{
				Description = "Microsoft SQL Server",
				EnvVarNameMiddlePart = "MS_SQL",
				DbContextBuilder = connectionDetails => new MsSqlDbContext(connectionDetails),
				DefaultPort = DbSpanCommon.DefaultPorts.MsSql,
				SpanSubtype = ApiConstants.SubtypeMssql
			},
			new ExternalDbType
			{
				Description = "MySQL",
				EnvVarNameMiddlePart = "MYSQL",
				DbContextBuilder = connectionDetails => new MySqlDbContext(connectionDetails),
				DefaultPort = DbSpanCommon.DefaultPorts.MySql,
				SpanSubtype = ApiConstants.SubtypeMySql
			},
		};

		public ExternalDbTests(ITestOutputHelper xUnitOutputHelper) : base(xUnitOutputHelper) { }

		private static IEnumerable<ValueTuple<ExternalDbType, ConnectionDetails>> FindConfiguredExternalDbs()
		{
			var isAtLeastOneExternalDbConfigured = false;
			foreach (var externalDbType in PotentialExternalDbTypes)
			{
				var connectionDetails = GetConnectionDetails(externalDbType);
				if (connectionDetails == null) continue;
				isAtLeastOneExternalDbConfigured = true;
				yield return (externalDbType, connectionDetails);
			}

			if (!isAtLeastOneExternalDbConfigured)
				yield return (new ExternalDbType { Description = "None of the potential external DB types is configured " }, null);
		}

		public static IEnumerable<object[]> ConfiguredExternalDbVariants =>
			FindConfiguredExternalDbs().Select(tuple => new object[] { tuple.Item1, tuple.Item2 });

		[Theory]
		[MemberData(nameof(ConfiguredExternalDbVariants))]
		public void Context_Destination_from_Db(ExternalDbType externalDbType, ConnectionDetails connectionDetails)
		{
			if (connectionDetails == null) return;

			var mockPayloadSender = new MockPayloadSender();
			using (var agent = new ApmAgent(new AgentComponents(payloadSender: mockPayloadSender)))
			{
				agent.Subscribe(new EfCoreDiagnosticsSubscriber());
				agent.Tracer.CaptureTransaction("test TX name", "test TX type"
					, () => { ExecuteTestCrudSequence(() => externalDbType.DbContextBuilder(connectionDetails)); });
			}
			mockPayloadSender.Transactions.Should().HaveCount(1);
			mockPayloadSender.Spans.ForEach(span =>
			{
				span.Type.Should().Be(ApiConstants.TypeDb);
				span.Subtype.Should().Be(externalDbType.SpanSubtype);
				span.Action.Should().Be(ApiConstants.ActionQuery);
				span.Context.Db.Type.Should().Be(Database.TypeSql);
				span.Context.Destination.Address.Should().Be(connectionDetails.Host);
				span.Context.Destination.Port.Should().Be(externalDbType.DefaultPort);
			});
		}

		private static ConnectionDetails GetConnectionDetails(ExternalDbType externalDbType)
		{
			var connectionDetails = new ConnectionDetails();
			foreach (var envVarSuffixToConnectionProperty in EnvVarSuffixToConnectionProperty)
			{
				var envVarName = "ELASTIC_APM_TESTS_" + externalDbType.EnvVarNameMiddlePart + "_" + envVarSuffixToConnectionProperty.Key;
				var envVarValue = Environment.GetEnvironmentVariable(envVarName);
				if (envVarValue == null) return null;
				envVarSuffixToConnectionProperty.Value(connectionDetails, envVarValue);
			}

			return connectionDetails;
		}

		private static void ExecuteTestCrudSequence(Func<DbContextImplBase> dbContextFactory)
		{
			using(var dbContext = dbContextFactory())
			{
				dbContext.Database.EnsureDeleted();
				dbContext.Database.EnsureCreated();
			}

			//
			// Create data
			//
			Publisher publisher;
			Book book1;
			using(var dbContext = dbContextFactory())
			{
				publisher = new Publisher { Name = "Mariner Books" };
				dbContext.Publishers.Add(publisher);
				book1 = new Book { ISBN = "978-0544003415", Title = "The Lord of the Rings", Publisher = publisher };
				dbContext.Books.Add(book1);
				dbContext.SaveChanges();
			}

			//
			// Read data and verify
			//
			using(var dbContext = dbContextFactory())
			{
				var publishers = dbContext.Publishers;
				var books = dbContext.Books;

				publishers.Should().HaveCount(1);
				books.Should().HaveCount(1);

				var actualPublisher = publishers.First();
				actualPublisher.Name.Should().Be(publisher.Name);
				actualPublisher.Books.Should().HaveCount(1);
				actualPublisher.Books.First().ISBN.Should().Be(book1.ISBN);
			}

			//
			// Update some data
			//
			Book book2;
			using(var dbContext = dbContextFactory())
			{
				book2 = new Book { ISBN = "978-0547247762", Title = "The Sealed Letter", Publisher = dbContext.Publishers.First() };
				dbContext.Books.Add(book2);
				dbContext.SaveChanges();
			}

			//
			// Read data and verify
			//
			using(var dbContext = dbContextFactory())
			{
				var publishers = dbContext.Publishers;
				var books = dbContext.Books;

				publishers.Should().HaveCount(1);
				books.Should().HaveCount(2);

				var actualPublisher = publishers.First();
				actualPublisher.Name.Should().Be(publisher.Name);
				actualPublisher.Books.Should().HaveCount(2);
				actualPublisher.Books.Select(b => b.ISBN).Should().Equal(book1.ISBN, book2.ISBN);
			}

			//
			// Delete some data
			//
			using(var dbContext = dbContextFactory())
			{
				dbContext.Books.Remove(book1);
				dbContext.SaveChanges();
			}

			//
			// Read data and verify
			//
			using(var dbContext = dbContextFactory())
			{
				var publishers = dbContext.Publishers;
				var books = dbContext.Books;

				publishers.Should().HaveCount(1);
				books.Should().HaveCount(1);

				var actualPublisher = publishers.First();
				actualPublisher.Name.Should().Be(publisher.Name);
				actualPublisher.Books.Should().HaveCount(1);
				actualPublisher.Books.First().ISBN.Should().Be(book2.ISBN);
			}
		}

		public class ConnectionDetails
		{
			internal string Host { get; set; }
			internal string Password { get; set; }
			internal string Username { get; set; }

			public override string ToString() => new ToStringBuilder
			{
				{ nameof(Host), Host.ToLog() },
				{ nameof(Username), Username.ToLog() },
				{ nameof(Password), Password.ToLog() }
			}.ToString();
		}

		public class ExternalDbType
		{
			internal Func<ConnectionDetails, DbContextImplBase> DbContextBuilder { get; set; }
			internal string Description { get; set; }
			internal string EnvVarNameMiddlePart { get; set; }
			internal int DefaultPort { get; set; }
			internal string SpanSubtype { get; set; }

			public override string ToString() => Description;
		}

		public class DbContextImplBase : DbContext
		{
			protected const string DbInstance = "ElasticApmExternalDbTests";

			internal readonly ConnectionDetails ConnectionDetails;

			protected DbContextImplBase(ConnectionDetails connectionDetails) => ConnectionDetails = connectionDetails;

			protected override void OnModelCreating(ModelBuilder modelBuilder)
			{
				base.OnModelCreating(modelBuilder);

				modelBuilder.Entity<Publisher>(entity =>
				{
					entity.HasKey(e => e.Id);
					entity.Property(e => e.Name).IsRequired();
				});

				modelBuilder.Entity<Book>(entity =>
				{
					entity.HasKey(e => e.ISBN);
					entity.Property(e => e.Title).IsRequired();
					entity.HasOne(d => d.Publisher)
						.WithMany(p => p.Books);
				});
			}

			// ReSharper disable UnusedAutoPropertyAccessor.Local
			public DbSet<Book> Books { get; set; }

			public DbSet<Publisher> Publishers { get; set; }
			// ReSharper restore UnusedAutoPropertyAccessor.Local
		}

		private class MsSqlDbContext : DbContextImplBase
		{
			internal MsSqlDbContext(ConnectionDetails connectionDetails)
				: base(connectionDetails) { }

			protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
			{
				// "Data Source=<Host>;Initial Catalog=<SubDatabase>;User ID=<Username>;Password=<Password>"
				var connectionStringBuilder = new SqlConnectionStringBuilder
				{
					DataSource = ConnectionDetails.Host,
					UserID = ConnectionDetails.Username,
					Password = ConnectionDetails.Password,
					InitialCatalog = DbInstance
				};

				optionsBuilder.UseSqlServer(connectionStringBuilder.ConnectionString);
			}
		}

		private class MySqlDbContext : DbContextImplBase
		{
			internal MySqlDbContext(ConnectionDetails connectionDetails)
				: base(connectionDetails) { }

			protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
			{
				// "server=<Host>;database=<SubDatabase>;user=<Username>;password=<Password>"
				var connectionStringBuilder = new MySqlConnectionStringBuilder
				{
					Server = ConnectionDetails.Host,
					UserID = ConnectionDetails.Username,
					Password = ConnectionDetails.Password,
					Database = DbInstance
				};

				optionsBuilder.UseMySQL(connectionStringBuilder.ConnectionString);
			}
		}

		public class Book
		{
			// ReSharper disable once InconsistentNaming
			public string ISBN { get; set; }
			public virtual Publisher Publisher { get; set; }
			public string Title { get; set; }

			public override string ToString() => new ToStringBuilder(nameof(Book))
			{
				{ nameof(ISBN), ISBN },
				{ nameof(Title), Title },
				{ nameof(Publisher), Publisher }
			}.ToString();
		}

		public class Publisher
		{
			public virtual ICollection<Book> Books { get; set; }
			public int Id { get; set; }
			public string Name { get; set; }

			public override string ToString() => new ToStringBuilder(nameof(Publisher))
			{
				{ nameof(Id), Id },
				{ nameof(Name), Name },
				{ "Books.Count", Books.Count }
			}.ToString();
		}
	}
}
