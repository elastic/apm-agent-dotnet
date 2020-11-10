// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.IO;

namespace Elastic.Apm.Tests.TestHelpers
{
	/// <summary>
	/// A temporary file that is deleted on dispose.
	/// </summary>
	/// <remarks>
	/// https://stackoverflow.com/a/400391
	/// </remarks>
	public sealed class TempFile : IDisposable
	{
		private string _path;
		public TempFile() : this(System.IO.Path.GetTempFileName()) { }

		public TempFile(string path)
		{
			if (string.IsNullOrEmpty(path))
				throw new ArgumentNullException(nameof(path));

			_path = path;
		}

		/// <summary>
		/// The path to the temporary file
		/// </summary>
		/// <exception cref="ObjectDisposedException"></exception>
		public string Path
		{
			get
			{
				if (_path == null)
					throw new ObjectDisposedException(GetType().Name);

				return _path;
			}
		}

		public static TempFile CreateWithContents(string contents)
		{
			if (contents is null)
				throw new ArgumentNullException(nameof(contents));

			var tempFile = new TempFile();
			File.WriteAllText(tempFile.Path, contents);
			return tempFile;
		}

		public static TempFile CreateWithContentsFrom(string path)
		{
			if (!File.Exists(path))
				throw new FileNotFoundException("does not exist", path);

			var tempFile = new TempFile();

			using var readStream = new FileStream(path, FileMode.Open, FileAccess.Read);
			using var writeStream = new FileStream(tempFile.Path, FileMode.OpenOrCreate, FileAccess.Write);
			readStream.CopyTo(writeStream);

			return tempFile;
		}

		~TempFile() => Dispose(false);

		public void Dispose() => Dispose(true);

		private void Dispose(bool disposing)
		{
			if (disposing)
				GC.SuppressFinalize(this);

			if (_path != null)
			{
				try
				{
					File.Delete(_path);
				}
				catch
				{
					// best effort. do nothing if it fails
				}
				_path = null;
			}
		}
	}
}
