using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Elastic.Apm.Logging;

namespace Elastic.Apm.PerfTests
{
	/// <summary>
	/// A logger that doesn't do anything.
	/// Useful for perf testing, where we want to avoid the work of printing the message.
	/// </summary>
	internal class PerfTestLogger : ConsoleLogger
	{
		public PerfTestLogger(LogLevel level) : base(level, new DebugTextWriter(), new DebugTextWriter()) { }
	}

	internal class DebugTextWriter : StreamWriter
	{
		public DebugTextWriter()
			: base(new DebugOutStream(), Encoding.Default) { }

		private class DebugOutStream : Stream
		{
			public override bool CanRead => false;

			public override bool CanSeek => false;

			public override bool CanWrite => true;

			public override long Length => throw new InvalidOperationException();

			public override long Position
			{
				get => throw new InvalidOperationException();
				set => throw new InvalidOperationException();
			}

			public override void Write(byte[] buffer, int offset, int count) { }

			public override void Flush() => Debug.Flush();

			public override int Read(byte[] buffer, int offset, int count) => throw new InvalidOperationException();

			public override long Seek(long offset, SeekOrigin origin) => throw new InvalidOperationException();

			public override void SetLength(long value) => throw new InvalidOperationException();
		}
	}
}
