// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.Logging;

namespace Elastic.Apm.Config
{
	internal class ConfigStore : IConfigStore
	{
		private const string ThisClassName = nameof(ConfigStore);
		private readonly object _lock = new object();

		private readonly IApmLogger _logger;

		internal ConfigStore(IConfigSnapshot initialSnapshot, IApmLogger logger)
		{
			_logger = logger.Scoped(ThisClassName);
			_currentSnapshot = initialSnapshot;
		}

		private volatile IConfigSnapshot _currentSnapshot;

		public IConfigSnapshot CurrentSnapshot
		{
			get => _currentSnapshot;

			set
			{
				lock (_lock)
				{
					var oldSnapshot = _currentSnapshot;
					_currentSnapshot = value;
					_logger.Info()
						?.Log("Replaced current snapshot. Old: {ConfigSnapshotDescription}. New: {ConfigSnapshotDescription}."
							, oldSnapshot.DbgDescription, _currentSnapshot.DbgDescription);
				}
			}
		}
	}
}
