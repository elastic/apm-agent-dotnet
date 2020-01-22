using System;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using Elastic.Apm.Helpers;
using Microsoft.Data.SqlClient;

namespace Elastic.Apm.PerfTests
{
	public class PropertyFetcherBenchmark
	{
		private const string CommandPropertyName = "Command";
		private const string CommandTextPropertyName = "CommandText";

		private object _object;
		private readonly PropertyFetcher _commandFetcher = new PropertyFetcher(CommandPropertyName);
		private readonly PropertyFetcher _commandTextFetcher = new PropertyFetcher(CommandTextPropertyName);

		private CascadePropertyFetcher _commandTextCascadeFetcher;

		private MethodInfo _commandPropertyInfo;
		private MethodInfo _commandTextPropertyInfo;


		[GlobalSetup]
		public void Setup()
		{
			_object = new { OperationId = Guid.NewGuid(), Command = new SqlCommand { CommandText = "Some text" } };

			_commandTextCascadeFetcher = new CascadePropertyFetcher(_commandFetcher, CommandTextPropertyName);

			_commandPropertyInfo = _object.GetType().GetTypeInfo().GetDeclaredProperty(CommandPropertyName).GetGetMethod();
			_commandTextPropertyInfo = _commandPropertyInfo.ReturnType.GetTypeInfo().GetDeclaredProperty(CommandTextPropertyName).GetGetMethod();
		}

		[Benchmark]
		public string Reflection()
		{
			var command = _object.GetType().GetTypeInfo().GetDeclaredProperty(CommandPropertyName)?.GetValue(_object);
			return command?.GetType().GetTypeInfo().GetDeclaredProperty(CommandTextPropertyName).ToString();
		}

		[Benchmark]
		public string PropertyFetcher()
		{
			var command = _commandFetcher.Fetch(_object);

			return _commandTextFetcher.Fetch(command).ToString();
		}

		[Benchmark]
		public string CascadeFetcher() => _commandTextCascadeFetcher.Fetch(_object).ToString();

		[Benchmark]
		public string CachedPropertyInfo()
		{
			var command = _commandPropertyInfo.Invoke(_object, Array.Empty<object>());
			return _commandTextPropertyInfo.Invoke(command, Array.Empty<object>()).ToString();
		}
	}
}
