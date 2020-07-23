// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Data.Common;
using System.Data.Entity;
using System.Data.SQLite;
using System.Threading;
using SQLite.CodeFirst;

namespace AspNetFullFrameworkSampleApp.Data
{
	internal class SampleDataDbContext : DbContext
	{
		private static readonly InitOnceHelper InitOnceHelperInstance = new InitOnceHelper();
		private static readonly Lazy<DbConnection> Connection = new Lazy<DbConnection>(CreateDbConnection);

		/// <param name="attachedState">Allows test code to pass an opaque context for DB operations</param>
		public SampleDataDbContext(object attachedState = null)
			: base(Connection.Value, /* contextOwnsConnection */ false)
		{
			Configure();
			InitOnceHelperInstance.Init(() => { Database.Initialize( /* force: */ true); });
			AttachedState = attachedState;
		}

		internal object AttachedState { get; }

		private static DbConnection CreateDbConnection()
		{
			var dbConnection = new SQLiteConnection("data source=:memory:");

			// This is important! Else the in memory database will not work.
			dbConnection.Open();

			return dbConnection;
		}

		private void Configure()
		{
			Configuration.ProxyCreationEnabled = true;
			Configuration.LazyLoadingEnabled = true;
		}

		protected override void OnModelCreating(DbModelBuilder modelBuilder)
		{
			ConfigureSampleDataEntity(modelBuilder);
			var initializer = new DbInitializer(modelBuilder);
			Database.SetInitializer(initializer);
		}

		private static void ConfigureSampleDataEntity(DbModelBuilder modelBuilder) => modelBuilder.Entity<SampleData>();

		private class DbInitializer : SqliteDropCreateDatabaseAlways<SampleDataDbContext>
		{
			public DbInitializer(DbModelBuilder modelBuilder)
				: base(modelBuilder) { }

			protected override void Seed(SampleDataDbContext context)
			{
				// Here you can seed your core data if you have any.
			}
		}

		private class InitOnceHelper
		{
			private bool _isInited;
			private object _lock;

			internal void Init(Action initAction)
			{
				object dummyObj = null /* dummy variable to satisfy EnsureInitialized */;
				LazyInitializer.EnsureInitialized(ref dummyObj, ref _isInited, ref _lock, () =>
				{
					initAction();
					return null /* dummy return value to satisfy EnsureInitialized */;
				});
			}
		}
	}
}
