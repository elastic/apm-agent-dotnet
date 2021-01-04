// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Tests.Mocks
{
	internal class TestLogger : ConsoleLogger
	{
		private readonly SynchronizedStringWriter _writer;

		public TestLogger() : this(LogLevel.Error, new SynchronizedStringWriter()) { }

		public TestLogger(LogLevel level) : this(level, new SynchronizedStringWriter()) { }

		private TestLogger(LogLevel level, SynchronizedStringWriter writer) : base(level, writer, writer) => _writer = writer;

		public IReadOnlyList<string> Lines
		{
			get
			{
				lock (_writer.Lock)
				{
					return _writer.GetStringBuilder()
						.ToString()
						.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
						.ToList();
				}
			}
		}
	}

	public class SynchronizedStringWriter : StringWriter
	{
		public readonly object Lock = new object();

		public override void Close()
		{
			lock (Lock)
				base.Close();
		}

		protected override void Dispose(bool disposing)
		{
			lock (Lock)
				base.Dispose(disposing);
		}

		public override void Flush()
		{
			lock (Lock)
				base.Flush();
		}

		public override void Write(char value)
		{
			lock (Lock)
				base.Write(value);
		}

		public override void Write(char[] buffer)
		{
			lock (Lock)
				base.Write(buffer);
		}

		public override void Write(char[] buffer, int index, int count)
		{
			lock (Lock)
				base.Write(buffer, index, count);
		}

		public override void Write(bool value)
		{
			lock (Lock)
				base.Write(value);
		}

		public override void Write(int value)
		{
			lock (Lock)
				base.Write(value);
		}

		public override void Write(uint value)
		{
			lock (Lock)
				base.Write(value);
		}

		public override void Write(long value)
		{
			lock (Lock)
				base.Write(value);
		}

		public override void Write(ulong value)
		{
			lock (Lock)
				base.Write(value);
		}

		public override void Write(float value)
		{
			lock (Lock)
				base.Write(value);
		}

		public override void Write(double value)
		{
			lock (Lock)
				base.Write(value);
		}

		public override void Write(decimal value)
		{
			lock (Lock)
				base.Write(value);
		}

		public override void Write(string value)
		{
			lock (Lock)
				base.Write(value);
		}

		public override void Write(object value)
		{
			lock (Lock)
				base.Write(value);
		}

		public override void Write(string format, object arg0)
		{
			lock (Lock)
				base.Write(format, arg0);
		}

		public override void Write(string format, object arg0, object arg1)
		{
			lock (Lock)
				base.Write(format, arg0, arg1);
		}

		public override void Write(string format, object arg0, object arg1, object arg2)
		{
			lock (Lock)
				base.Write(format, arg0, arg1, arg2);
		}

		public override void Write(string format, params object[] arg)
		{
			lock (Lock)
				base.Write(format, arg);
		}

		public override void WriteLine()
		{
			lock (Lock)
				base.WriteLine();
		}

		public override void WriteLine(char value)
		{
			lock (Lock)
				base.WriteLine(value);
		}

		public override void WriteLine(char[] buffer)
		{
			lock (Lock)
				base.WriteLine(buffer);
		}

		public override void WriteLine(char[] buffer, int index, int count)
		{
			lock (Lock)
				base.WriteLine(buffer, index, count);
		}

		public override void WriteLine(bool value)
		{
			lock (Lock)
				base.WriteLine(value);
		}

		public override void WriteLine(int value)
		{
			lock (Lock)
				base.WriteLine(value);
		}

		public override void WriteLine(uint value)
		{
			lock (Lock)
				base.WriteLine(value);
		}

		public override void WriteLine(long value)
		{
			lock (Lock)
				base.WriteLine(value);
		}

		public override void WriteLine(ulong value)
		{
			lock (Lock)
				base.WriteLine(value);
		}

		public override void WriteLine(float value)
		{
			lock (Lock)
				base.WriteLine(value);
		}

		public override void WriteLine(double value)
		{
			lock (Lock)
				base.WriteLine(value);
		}

		public override void WriteLine(decimal value)
		{
			lock (Lock)
				base.WriteLine(value);
		}

		public override void WriteLine(string value)
		{
			lock (Lock)
				base.WriteLine(value);
		}

		public override void WriteLine(object value)
		{
			lock (Lock)
				base.WriteLine(value);
		}

		public override void WriteLine(string format, object arg0)
		{
			lock (Lock)
				base.WriteLine(format, arg0);
		}

		public override void WriteLine(string format, object arg0, object arg1)
		{
			lock (Lock)
				base.WriteLine(format, arg0, arg1);
		}

		public override void WriteLine(string format, object arg0, object arg1, object arg2)
		{
			lock (Lock)
				base.WriteLine(format, arg0, arg1, arg2);
		}

		public override void WriteLine(string format, params object[] arg)
		{
			lock (Lock)
				base.WriteLine(format, arg);
		}

		public override Task WriteAsync(char value)
		{
			lock (Lock)
				return base.WriteAsync(value);
		}


		public override Task WriteAsync(string value)
		{
			lock (Lock)
				return base.WriteAsync(value);
		}

		public override Task WriteAsync(char[] buffer, int index, int count)
		{
			lock (Lock)
				return base.WriteAsync(buffer, index, count);
		}

		public override Task WriteLineAsync(char value)
		{
			lock (Lock)
				return base.WriteLineAsync(value);
		}

		public override Task WriteLineAsync(string value)
		{
			lock (Lock)
				return base.WriteLineAsync(value);
		}

		public override Task WriteLineAsync(char[] buffer, int index, int count)
		{
			lock (Lock)
				return base.WriteLineAsync(buffer, index, count);
		}

		public override Task WriteLineAsync()
		{
			lock (Lock)
				return base.WriteLineAsync();
		}

		public override Task FlushAsync()
		{
			lock (Lock)
				return base.FlushAsync();
		}

		public override string NewLine
		{
			get
			{
				lock (Lock)
					return base.NewLine;
			}

			set
			{
				lock (Lock)
					base.NewLine = value;
			}
		}
	}
}
