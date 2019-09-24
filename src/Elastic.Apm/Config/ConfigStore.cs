using System;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Config
{
	internal class ConfigStore: IConfigStore
	{
		private const string ThisClassName = nameof(ConfigStore);

		private readonly IApmLogger _logger;
		private readonly object _lock = new object();
		private IConfigSnapshot _currentSnapshot;

		internal ConfigStore(IConfigSnapshot initialSnapshot, IApmLogger logger)
		{
			_logger = logger.Scoped(ThisClassName);
			_currentSnapshot = initialSnapshot;
		}

		public IConfigSnapshot CurrentSnapshot
		{
			get
			{
				lock (_lock) return _currentSnapshot;
			}

			set
			{
				lock (_lock)
				{
					var oldSnapshot = _currentSnapshot;
					_currentSnapshot = value;
					_logger.Info()?.Log("Replaced current snapshot. Old: {ConfigSnapshotDescription}. New: {ConfigSnapshotDescription}."
						, oldSnapshot.DbgDescription, _currentSnapshot.DbgDescription);
				}

			}
		}
	}
}
