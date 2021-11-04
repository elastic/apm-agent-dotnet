// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
//
// <copyright file="LazyILGenerator.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Elastic.Apm.Profiler.Managed.DuckTyping
{
    internal class LazyILGenerator
    {
        private ILGenerator _generator;
        private List<Action<ILGenerator>> _instructions;
        private int _offset;

        public LazyILGenerator(ILGenerator generator)
        {
            _generator = generator;
            _instructions = new List<Action<ILGenerator>>(16);
        }

        public int Offset => _offset;

        public int Count => _instructions.Count;

        public void SetOffset(int value)
        {
            if (value > _instructions.Count)
				_offset = _instructions.Count;
			else
				_offset = value;
		}

        public void ResetOffset() => _offset = _instructions.Count;

		public void BeginScope()
        {
            if (_offset == _instructions.Count)
				_instructions.Add(il => il.BeginScope());
			else
				_instructions.Insert(_offset, il => il.BeginScope());

			_offset++;
        }

        public LocalBuilder DeclareLocal(Type localType, bool pinned) => _generator.DeclareLocal(localType, pinned);

		public LocalBuilder DeclareLocal(Type localType) => _generator.DeclareLocal(localType);

		public Label DefineLabel() => _generator.DefineLabel();

		public void Emit(OpCode opcode, string str)
        {
            if (_offset == _instructions.Count)
				_instructions.Add(il => il.Emit(opcode, str));
			else
				_instructions.Insert(_offset, il => il.Emit(opcode, str));

			_offset++;
        }

        public void Emit(OpCode opcode, FieldInfo field)
        {
            if (_offset == _instructions.Count)
				_instructions.Add(il => il.Emit(opcode, field));
			else
				_instructions.Insert(_offset, il => il.Emit(opcode, field));

			_offset++;
        }

        public void Emit(OpCode opcode, Label[] labels)
        {
            if (_offset == _instructions.Count)
				_instructions.Add(il => il.Emit(opcode, labels));
			else
				_instructions.Insert(_offset, il => il.Emit(opcode, labels));

			_offset++;
        }

        public void Emit(OpCode opcode, Label label)
        {
            if (_offset == _instructions.Count)
				_instructions.Add(il => il.Emit(opcode, label));
			else
				_instructions.Insert(_offset, il => il.Emit(opcode, label));

			_offset++;
        }

        public void Emit(OpCode opcode, LocalBuilder local)
        {
            if (_offset == _instructions.Count)
				_instructions.Add(il => il.Emit(opcode, local));
			else
				_instructions.Insert(_offset, il => il.Emit(opcode, local));

			_offset++;
        }

        public void Emit(OpCode opcode, float arg)
        {
            if (_offset == _instructions.Count)
				_instructions.Add(il => il.Emit(opcode, arg));
			else
				_instructions.Insert(_offset, il => il.Emit(opcode, arg));

			_offset++;
        }

        public void Emit(OpCode opcode, byte arg)
        {
            if (_offset == _instructions.Count)
				_instructions.Add(il => il.Emit(opcode, arg));
			else
				_instructions.Insert(_offset, il => il.Emit(opcode, arg));

			_offset++;
        }

        public void Emit(OpCode opcode, sbyte arg)
        {
            if (_offset == _instructions.Count)
				_instructions.Add(il => il.Emit(opcode, arg));
			else
				_instructions.Insert(_offset, il => il.Emit(opcode, arg));

			_offset++;
        }

        public void Emit(OpCode opcode, short arg)
        {
            if (_offset == _instructions.Count)
				_instructions.Add(il => il.Emit(opcode, arg));
			else
				_instructions.Insert(_offset, il => il.Emit(opcode, arg));

			_offset++;
        }

        public void Emit(OpCode opcode, double arg)
        {
            if (_offset == _instructions.Count)
				_instructions.Add(il => il.Emit(opcode, arg));
			else
				_instructions.Insert(_offset, il => il.Emit(opcode, arg));

			_offset++;
        }

        public void Emit(OpCode opcode, MethodInfo meth)
        {
            if (_offset == _instructions.Count)
				_instructions.Add(il => il.Emit(opcode, meth));
			else
				_instructions.Insert(_offset, il => il.Emit(opcode, meth));

			_offset++;
        }

        public void Emit(OpCode opcode, int arg)
        {
            if (_offset == _instructions.Count)
				_instructions.Add(il => il.Emit(opcode, arg));
			else
				_instructions.Insert(_offset, il => il.Emit(opcode, arg));

			_offset++;
        }

        public void Emit(OpCode opcode)
        {
            if (_offset == _instructions.Count)
				_instructions.Add(il => il.Emit(opcode));
			else
				_instructions.Insert(_offset, il => il.Emit(opcode));

			_offset++;
        }

        public void Emit(OpCode opcode, long arg)
        {
            if (_offset == _instructions.Count)
				_instructions.Add(il => il.Emit(opcode, arg));
			else
				_instructions.Insert(_offset, il => il.Emit(opcode, arg));

			_offset++;
        }

        public void Emit(OpCode opcode, Type cls)
        {
            if (_offset == _instructions.Count)
				_instructions.Add(il => il.Emit(opcode, cls));
			else
				_instructions.Insert(_offset, il => il.Emit(opcode, cls));

			_offset++;
        }

        public void Emit(OpCode opcode, SignatureHelper signature)
        {
            if (_offset == _instructions.Count)
				_instructions.Add(il => il.Emit(opcode, signature));
			else
				_instructions.Insert(_offset, il => il.Emit(opcode, signature));

			_offset++;
        }

        public void Emit(OpCode opcode, ConstructorInfo con)
        {
            if (_offset == _instructions.Count)
				_instructions.Add(il => il.Emit(opcode, con));
			else
				_instructions.Insert(_offset, il => il.Emit(opcode, con));

			_offset++;
        }

        public void EmitCall(OpCode opcode, MethodInfo methodInfo, Type[] optionalParameterTypes)
        {
            if (_offset == _instructions.Count)
				_instructions.Add(il => il.EmitCall(opcode, methodInfo, optionalParameterTypes));
			else
				_instructions.Insert(_offset, il => il.EmitCall(opcode, methodInfo, optionalParameterTypes));

			_offset++;
        }

        public void EmitCalli(OpCode opcode, CallingConventions callingConvention, Type returnType, Type[] parameterTypes, Type[] optionalParameterTypes)
        {
            if (_offset == _instructions.Count)
				_instructions.Add(il => il.EmitCalli(opcode, callingConvention, returnType, parameterTypes, optionalParameterTypes));
			else
				_instructions.Insert(_offset, il => il.EmitCalli(opcode, callingConvention, returnType, parameterTypes, optionalParameterTypes));

			_offset++;
        }

        public void EmitWriteLine(string value)
        {
            if (_offset == _instructions.Count)
				_instructions.Add(il => il.EmitWriteLine(value));
			else
				_instructions.Insert(_offset, il => il.EmitWriteLine(value));

			_offset++;
        }

        public void EmitWriteLine(FieldInfo fld)
        {
            if (_offset == _instructions.Count)
				_instructions.Add(il => il.EmitWriteLine(fld));
			else
				_instructions.Insert(_offset, il => il.EmitWriteLine(fld));

			_offset++;
        }

        public void EmitWriteLine(LocalBuilder localBuilder)
        {
            if (_offset == _instructions.Count)
				_instructions.Add(il => il.EmitWriteLine(localBuilder));
			else
				_instructions.Insert(_offset, il => il.EmitWriteLine(localBuilder));

			_offset++;
        }

        public void EndScope()
        {
            if (_offset == _instructions.Count)
				_instructions.Add(il => il.EndScope());
			else
				_instructions.Insert(_offset, il => il.EndScope());

			_offset++;
        }

        public void MarkLabel(Label loc)
        {
            if (_offset == _instructions.Count)
				_instructions.Add(il => il.MarkLabel(loc));
			else
				_instructions.Insert(_offset, il => il.MarkLabel(loc));

			_offset++;
        }

        public void ThrowException(Type excType)
        {
            if (_offset == _instructions.Count)
				_instructions.Add(il => il.ThrowException(excType));
			else
				_instructions.Insert(_offset, il => il.ThrowException(excType));

			_offset++;
        }

        public void UsingNamespace(string usingNamespace)
        {
            if (_offset == _instructions.Count)
				_instructions.Add(il => il.UsingNamespace(usingNamespace));
			else
				_instructions.Insert(_offset, il => il.UsingNamespace(usingNamespace));

			_offset++;
        }

        public void Flush()
        {
            foreach (var instr in _instructions)
				instr(_generator);

			_instructions.Clear();
            _offset = 0;
        }
    }
}
