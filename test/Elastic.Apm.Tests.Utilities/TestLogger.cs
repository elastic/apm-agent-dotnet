// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Tests.Utilities
{
	internal class TestLogger : ConsoleLogger
	{
		private readonly SynchronizedStringWriter _writer;

		public TestLogger() : this(LogLevel.Error, new SynchronizedStringWriter()) { }

		public TestLogger(LogLevel level) : this(level, new SynchronizedStringWriter()) { }

		private TestLogger(LogLevel level, SynchronizedStringWriter writer) : base(level, writer, writer) => _writer = writer;

		public IReadOnlyList<string> Lines =>
			_writer.GetStringBuilder()
				.ToString()
				.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
				.ToList();
	}

	public class SynchronizedStringWriter : StringWriter
	{
		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void Close() => base.Close();

		[MethodImpl(MethodImplOptions.Synchronized)]
		protected override void Dispose(bool disposing) => base.Dispose(disposing);

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void Flush() => base.Flush();

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override StringBuilder GetStringBuilder() => base.GetStringBuilder();

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void Write(char value) => base.Write(value);

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void Write(char[] buffer) => base.Write(buffer);

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void Write(char[] buffer, int index, int count) => base.Write(buffer, index, count);

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void Write(bool value) => base.Write(value);

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void Write(int value) => base.Write(value);

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void Write(uint value) => base.Write(value);

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void Write(long value) => base.Write(value);

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void Write(ulong value) => base.Write(value);

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void Write(float value) => base.Write(value);

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void Write(double value) => base.Write(value);

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void Write(decimal value) => base.Write(value);

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void Write(string value) => base.Write(value);

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void Write(object value) => base.Write(value);

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void Write(string format, object arg0) => base.Write(format, arg0);

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void Write(string format, object arg0, object arg1) => base.Write(format, arg0, arg1);

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void Write(string format, object arg0, object arg1, object arg2) => base.Write(format, arg0, arg1, arg2);

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void Write(string format, params object[] arg) => base.Write(format, arg);

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void WriteLine() => base.WriteLine();

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void WriteLine(char value) => base.WriteLine(value);

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void WriteLine(char[] buffer) => base.WriteLine(buffer);

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void WriteLine(char[] buffer, int index, int count) => base.WriteLine(buffer, index, count);

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void WriteLine(bool value) => base.WriteLine(value);

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void WriteLine(int value) => base.WriteLine(value);

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void WriteLine(uint value) => base.WriteLine(value);

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void WriteLine(long value) => base.WriteLine(value);

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void WriteLine(ulong value) => base.WriteLine(value);

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void WriteLine(float value) => base.WriteLine(value);

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void WriteLine(double value) => base.WriteLine(value);

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void WriteLine(decimal value) => base.WriteLine(value);

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void WriteLine(string value) => base.WriteLine(value);

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void WriteLine(object value) => base.WriteLine(value);

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void WriteLine(string format, object arg0) => base.WriteLine(format, arg0);

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void WriteLine(string format, object arg0, object arg1) => base.WriteLine(format, arg0, arg1);

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void WriteLine(string format, object arg0, object arg1, object arg2) => base.WriteLine(format, arg0, arg1, arg2);

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void WriteLine(string format, params object[] arg) => base.WriteLine(format, arg);

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override Task WriteAsync(char value) => base.WriteAsync(value);

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override Task WriteAsync(string value) => base.WriteAsync(value);

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override Task WriteAsync(char[] buffer, int index, int count) => base.WriteAsync(buffer, index, count);

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override Task WriteLineAsync(char value) => base.WriteLineAsync(value);

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override Task WriteLineAsync(string value) => base.WriteLineAsync(value);

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override Task WriteLineAsync(char[] buffer, int index, int count) => base.WriteLineAsync(buffer, index, count);

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override Task WriteLineAsync() => base.WriteLineAsync();

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override Task FlushAsync() => base.FlushAsync();

		public override string NewLine
		{
			[MethodImpl(MethodImplOptions.Synchronized)]
			get => base.NewLine;
			[MethodImpl(MethodImplOptions.Synchronized)]
			set => base.NewLine = value;
		}
	}
}
