// Based on .NET Tracer for Datadog APM by Datadog
// https://github.com/DataDog/dd-trace-dotnet
// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.AdoNet.NetStandard;
using Elastic.Apm.Api;

namespace Elastic.Apm.AdoNet
{
	public class DbCommandRunner
	{
		/// <summary>
		/// Creates a RunAllAsync&lt;TDbCommand&gt; transaction with the following spans,
		/// when the profiler is attached:
		/// <para/>
		/// nameof(TDbCommand) command span:
		/// <list type="bullet">
		///		<item>sync span + 7 command spans</item>
		///		<item>async span + 7 command spans (if supports async)</item>
		///		<item>async with cancellation span + 7 command spans (if supports async)</item>
		/// </list>
		/// 25 spans (if supports async), otherwise 9 spans
		///
		/// <para/>
		/// DbCommand command span:
		/// <list type="bullet">
		///		<item>sync span + 7 command spans</item>
		///		<item>async span + 7 command spans</item>
		///		<item>async with cancellation span + 7 command spans</item>
		/// </list>
		/// 25 spans
		///
		/// <para />
		/// IDbCommand command span:
		/// <list type="bullet">
		///		<item>sync span + 7 command spans</item>
		/// </list>
		/// 9 spans
		///
		/// <para />
		/// IDbCommandGenericConstraint&lt;TDbCommand&gt; command span:
		/// <list type="bullet">
		///		<item>sync span + 7 command spans</item>
		/// </list>
		/// 9 spans
		///
		/// <para/>
		/// DbCommand-NetStandard command span:
		/// <list type="bullet">
		///		<item>sync span + 7 command spans</item>
		///		<item>async span + 7 command spans</item>
		///		<item>async with cancellation span + 7 command spans</item>
		/// </list>
		/// 25 spans
		///
		/// <para />
		/// IDbCommand-NetStandard command span:
		/// <list type="bullet">
		///		<item>sync span + 7 command spans</item>
		/// </list>
		/// 9 spans
		///
		/// <para />
		/// IDbCommandGenericConstraint&lt;TDbCommand&gt;-NetStandard command span:
		/// <list type="bullet">
		///		<item>sync span + 7 command spans</item>
		/// </list>
		/// 9 spans
		///
		/// <para />
		/// 111 spans total (if supports async), otherwise 95 spans
		/// </summary>
		public static async Task RunAllAsync<TDbCommand>(
			DbCommandFactory commandFactory,
			IDbCommandExecutor providerCommandExecutor,
			CancellationToken token
		) where TDbCommand : IDbCommand
		{
			var executors = new List<IDbCommandExecutor>
			{
				providerCommandExecutor,
				new DbCommandClassExecutor(),
				new DbCommandInterfaceExecutor(),
				new DbCommandInterfaceGenericExecutor<TDbCommand>(),

				// call methods referencing netstandard.dll
				new DbCommandNetStandardClassExecutor(),
				new DbCommandNetStandardInterfaceExecutor(),
				new DbCommandNetStandardInterfaceGenericExecutor<TDbCommand>(),
			};

			await Agent.Tracer.CaptureTransaction("RunAllAsync<TDbCommand>", "test", async transaction =>
			{
				foreach (var executor in executors)
					await RunAsync(transaction, commandFactory, executor, token);
			});
		}

		/// <summary>
		/// Creates a RunBaseTypesAsync transaction with the following spans,
		/// when the profiler is attached:
		/// <para/>
		/// DbCommand command span:
		/// <list type="bullet">
		///		<item>sync span + 7 command spans</item>
		///		<item>async span + 7 command spans</item>
		///		<item>async with cancellation span + 7 command spans</item>
		/// </list>
		/// 25 spans
		///
		/// <para />
		/// IDbCommand command span:
		/// <list type="bullet">
		///		<item>sync span + 7 command spans</item>
		/// </list>
		/// 9 spans
		///
		/// <para/>
		/// DbCommand-NetStandard command span:
		/// <list type="bullet">
		///		<item>sync span + 7 command spans</item>
		///		<item>async span + 7 command spans</item>
		///		<item>async with cancellation span + 7 command spans</item>
		/// </list>
		/// 25 spans
		///
		/// <para />
		/// IDbCommand-NetStandard command span:
		/// <list type="bullet">
		///		<item>sync span + 7 command spans</item>
		/// </list>
		/// 9 spans
		///
		/// <para />
		/// 68 spans total
		/// </summary>
		public static async Task RunBaseTypesAsync(
			DbCommandFactory commandFactory,
			CancellationToken token
		)
		{
			var executors = new List<IDbCommandExecutor>
			{
				new DbCommandClassExecutor(),
				new DbCommandInterfaceExecutor(),

				// call methods referencing netstandard.dll
				new DbCommandNetStandardClassExecutor(),
				new DbCommandNetStandardInterfaceExecutor(),
			};

			await Agent.Tracer.CaptureTransaction("RunBaseTypesAsync", "test", async transaction =>
			{
				foreach (var executor in executors)
					await RunAsync(transaction, commandFactory, executor, token);
			});
		}

		private static async Task RunAsync(
			ITransaction transaction,
			DbCommandFactory commandFactory,
			IDbCommandExecutor commandExecutor,
			CancellationToken cancellationToken
		)
		{
			var commandName = commandExecutor.CommandTypeName;
			Console.WriteLine(commandName);

			await transaction.CaptureSpan($"{commandName} command", "command", async span =>
			{
				IDbCommand command;

				await span.CaptureSpan($"{commandName} sync", "sync", async childSpan =>
				{
					Console.WriteLine("  synchronous");
					await Task.Delay(100, cancellationToken);

					command = commandFactory.GetCreateTableCommand();
					commandExecutor.ExecuteNonQuery(command);

					command = commandFactory.GetInsertRowCommand();
					commandExecutor.ExecuteNonQuery(command);

					command = commandFactory.GetSelectScalarCommand();
					commandExecutor.ExecuteScalar(command);

					command = commandFactory.GetUpdateRowCommand();
					commandExecutor.ExecuteNonQuery(command);

					command = commandFactory.GetSelectRowCommand();
					commandExecutor.ExecuteReader(command);

					command = commandFactory.GetSelectRowCommand();
					commandExecutor.ExecuteReader(command, CommandBehavior.Default);

					command = commandFactory.GetDeleteRowCommand();
					commandExecutor.ExecuteNonQuery(command);
				});

				if (commandExecutor.SupportsAsyncMethods)
				{
					await Task.Delay(100, cancellationToken);

					await span.CaptureSpan($"{commandName} async", "async", async childSpan =>
					{
						Console.WriteLine("  asynchronous");
						await Task.Delay(100, cancellationToken);

						command = commandFactory.GetCreateTableCommand();
						await commandExecutor.ExecuteNonQueryAsync(command);

						command = commandFactory.GetInsertRowCommand();
						await commandExecutor.ExecuteNonQueryAsync(command);

						command = commandFactory.GetSelectScalarCommand();
						await commandExecutor.ExecuteScalarAsync(command);

						command = commandFactory.GetUpdateRowCommand();
						await commandExecutor.ExecuteNonQueryAsync(command);

						command = commandFactory.GetSelectRowCommand();
						await commandExecutor.ExecuteReaderAsync(command);

						command = commandFactory.GetSelectRowCommand();
						await commandExecutor.ExecuteReaderAsync(command, CommandBehavior.Default);

						command = commandFactory.GetDeleteRowCommand();
						await commandExecutor.ExecuteNonQueryAsync(command);
					});

					await Task.Delay(100, cancellationToken);

					await span.CaptureSpan($"{commandName} async with cancellation", "async-cancellation", async childSpan =>
					{
						Console.WriteLine("  asynchronous with cancellation");
						await Task.Delay(100, cancellationToken);

						command = commandFactory.GetCreateTableCommand();
						await commandExecutor.ExecuteNonQueryAsync(command, cancellationToken);

						command = commandFactory.GetInsertRowCommand();
						await commandExecutor.ExecuteNonQueryAsync(command, cancellationToken);

						command = commandFactory.GetSelectScalarCommand();
						await commandExecutor.ExecuteScalarAsync(command, cancellationToken);

						command = commandFactory.GetUpdateRowCommand();
						await commandExecutor.ExecuteNonQueryAsync(command, cancellationToken);

						command = commandFactory.GetSelectRowCommand();
						await commandExecutor.ExecuteReaderAsync(command, cancellationToken);

						command = commandFactory.GetSelectRowCommand();
						await commandExecutor.ExecuteReaderAsync(command, CommandBehavior.Default, cancellationToken);

						command = commandFactory.GetDeleteRowCommand();
						await commandExecutor.ExecuteNonQueryAsync(command, cancellationToken);
					});
				}
			});

			await Task.Delay(100, cancellationToken);
		}
	}
}
