// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.Logging;

namespace Elastic.Apm.Config
{
	internal class ConfigurationStore : IConfigurationStore
	{
		private const string ThisClassName = nameof(ConfigurationStore);
		private readonly object _lock = new object();

		private readonly IApmLogger _logger;

		internal ConfigurationStore(IConfigurationSnapshot initialSnapshot, IApmLogger logger)
		{
			_logger = logger.Scoped(ThisClassName);
			_currentSnapshot = initialSnapshot;
		}

		private volatile IConfigurationSnapshot _currentSnapshot;

		public IConfigurationSnapshot CurrentSnapshot
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
							, oldSnapshot.Description(), _currentSnapshot.Description());
				}
			}
		}
	}
}
