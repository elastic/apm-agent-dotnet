using System;
using System.Reflection;
using System.Reflection.Emit;

#nullable enable
namespace Elastic.Apm.Libraries.Ben.Demystifier.Internal
{
	internal class ILReader
	{
		private static readonly OpCode[] doubleByteOpCode;
		private static readonly OpCode[] singleByteOpCode;

		private readonly byte[] _cil;

		static ILReader()
		{
			singleByteOpCode = new OpCode[225];
			doubleByteOpCode = new OpCode[31];

			var fields = GetOpCodeFields();

			for (var i = 0; i < fields.Length; i++)
			{
				var code = (OpCode)fields[i].GetValue(null)!;
				if (code.OpCodeType == OpCodeType.Nternal)
					continue;

				if (code.Size == 1)
					singleByteOpCode[code.Value] = code;
				else
					doubleByteOpCode[code.Value & 0xff] = code;
			}
		}


		public ILReader(byte[] cil) => _cil = cil;

		public int MetadataToken { get; private set; }

		public OpCode OpCode { get; private set; }
		public MemberInfo? Operand { get; private set; }
		private int ptr;

		public bool Read(MethodBase methodInfo)
		{
			if (ptr < _cil.Length)
			{
				OpCode = ReadOpCode();
				Operand = ReadOperand(OpCode, methodInfo);
				return true;
			}
			return false;
		}

		private OpCode ReadOpCode()
		{
			var instruction = ReadByte();
			if (instruction < 254)
				return singleByteOpCode[instruction];
			else
				return doubleByteOpCode[ReadByte()];
		}

		private MemberInfo? ReadOperand(OpCode code, MethodBase methodInfo)
		{
			MetadataToken = 0;
			int inlineLength;
			switch (code.OperandType)
			{
				case OperandType.InlineMethod:
					MetadataToken = ReadInt();
					Type[]? methodArgs = null;
					if (methodInfo.GetType() != typeof(ConstructorInfo) && !methodInfo.GetType().IsSubclassOf(typeof(ConstructorInfo)))
						methodArgs = methodInfo.GetGenericArguments();
					Type[]? typeArgs = null;
					if (methodInfo.DeclaringType != null) typeArgs = methodInfo.DeclaringType.GetGenericArguments();
					try
					{
						return methodInfo.Module.ResolveMember(MetadataToken, typeArgs, methodArgs);
					}
					catch
					{
						// Can return System.ArgumentException : Token xxx is not a valid MemberInfo token in the scope of module xxx.dll
						return null;
					}

				case OperandType.InlineNone:
					inlineLength = 0;
					break;

				case OperandType.ShortInlineBrTarget:
				case OperandType.ShortInlineVar:
				case OperandType.ShortInlineI:
					inlineLength = 1;
					break;

				case OperandType.InlineVar:
					inlineLength = 2;
					break;

				case OperandType.InlineBrTarget:
				case OperandType.InlineField:
				case OperandType.InlineI:
				case OperandType.InlineString:
				case OperandType.InlineSig:
				case OperandType.InlineSwitch:
				case OperandType.InlineTok:
				case OperandType.InlineType:
				case OperandType.ShortInlineR:
					inlineLength = 4;
					break;

				case OperandType.InlineI8:
				case OperandType.InlineR:
					inlineLength = 8;
					break;

				default:
					return null;
			}

			for (var i = 0; i < inlineLength; i++) ReadByte();

			return null;
		}

		private byte ReadByte() => _cil[ptr++];

		private int ReadInt()
		{
			var b1 = ReadByte();
			var b2 = ReadByte();
			var b3 = ReadByte();
			var b4 = ReadByte();
			return b1 | (b2 << 8) | (b3 << 16) | (b4 << 24);
		}

		private static FieldInfo[] GetOpCodeFields() => typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static);
	}
}
