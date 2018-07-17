using System;
using System.IO;
using System.Reflection.Emit;

namespace Wasm2CIL {

	public class WebassemblyInstruction
	{
		public const byte END = 0x0B;

		protected byte opcode;

		public static int BlockDepthDiff (BinaryReader reader) 
		{
			int opcode = reader.PeekChar ();
			if (opcode == END)
				return -1;

			// Block, loop, if
			if (opcode == 0x02 || opcode == 0x03 || opcode == 0x04)
				return 1;

			// Everything else is unchanged
			return 0;
		}

		public static WebassemblyInstruction Parse (BinaryReader reader) 
		{
			int opcode = reader.PeekChar ();

			if (opcode <= WebassemblyControlInstruction.UpperBound ()) {
				Console.WriteLine ("0x{0:X} <= 0x{1:X}", opcode, WebassemblyControlInstruction.UpperBound ());
				return new WebassemblyControlInstruction (reader);
			} else if (opcode <= WebassemblyParametricInstruction.UpperBound ()) {
				Console.WriteLine ("0x{0:X} <= 0x{1:X}", opcode, WebassemblyParametricInstruction.UpperBound ());
				return new WebassemblyParametricInstruction (reader);
			} else if (opcode <= WebassemblyMemoryInstruction.UpperBound ()) {
				Console.WriteLine ("0x{0:X} <= 0x{1:X}", opcode, WebassemblyMemoryInstruction.UpperBound ());
				return new WebassemblyMemoryInstruction (reader);
			} else if (opcode <= WebassemblyNumericInstruction.UpperBound ()) {
				Console.WriteLine ("0x{0:X} <= 0x{1:X}", opcode, WebassemblyNumericInstruction.UpperBound ());
				return new WebassemblyNumericInstruction (reader);
			} else {
				throw new Exception (String.Format ("Illegal instruction {0}", opcode));
			}
		}

		public void Add (MethodBuilder builder) 
		{
			return;
		}

		//public string ToString ();
	}

	public class WebassemblyControlInstruction : WebassemblyInstruction
	{
		ulong [] table;
		ulong index;
		ulong default_target;
		ulong block_type;

		public static byte UpperBound ()
		{
			return 0x11;
		}

		public WebassemblyControlInstruction (BinaryReader reader) 
		{
			this.opcode = reader.ReadByte ();
			switch (this.opcode) {
				case 0x0: // unreachable
				case 0x1: // nop
				case 0x0F: // return
				case 0x05: // else
					break;
				case 0x0C: // br
				case 0x0D: // br_if
					this.index = Parser.ParseLEBSigned (reader, 32);
					break;
				case 0x02: // block
				case 0x03: // loop 
				case 0x04: // if
					this.block_type = Parser.ParseLEBSigned (reader, 32);
					break;
				case 0x0e: // br_table
					// works by getting index from stack. If index is in range of table,
					// we jump to the label at that index. Else go to default.
					this.table = Parser.ParseLEBSignedArray (reader);
					this.default_target = Parser.ParseLEBSigned (reader, 32);
					break;
				default:
					throw new Exception (String.Format ("Control instruction out of range {0}", this.opcode));
			}
			
		}
	}

	public class WebassemblyParametricInstruction : WebassemblyInstruction
	{
		public static byte UpperBound ()
		{
			return 0x1B;
		}

		public WebassemblyParametricInstruction (BinaryReader reader) {
			this.opcode = reader.ReadByte ();
			if (this.opcode != 0x1A && this.opcode <= 0x1B) {
				throw new Exception ("Parametric opcode out of range");
			}
		}
	}

	public class WebassemblyVariableInstruction : WebassemblyInstruction
	{
		ulong index;

		public static byte UpperBound ()
		{
			return 0x24;
		}

		public WebassemblyVariableInstruction (BinaryReader reader) {
			this.opcode = reader.ReadByte ();
			if (this.opcode >= 0x20 && this.opcode <= 0x24) {
				this.index = Parser.ParseLEBSigned (reader, 32);
			} else if (this.opcode > 0x24) {
				throw new Exception ("Variable opcode out of range");
			}
		}
}

	public class WebassemblyMemoryInstruction : WebassemblyInstruction
	{
		ulong align;
		ulong offset;

		public static byte UpperBound ()
		{
			return 0x40;
		}

		public WebassemblyMemoryInstruction (BinaryReader reader) {
			this.opcode = reader.ReadByte ();
			if (this.opcode >= 0x28 && this.opcode <= 0x3E) {
				this.align = Parser.ParseLEBSigned (reader, 32);
				this.offset = Parser.ParseLEBSigned (reader, 32);
			} else if (this.opcode > 0x40) {
				throw new Exception ("Memory opcode out of range");
			}
		}
	}

	public class WebassemblyNumericInstruction : WebassemblyInstruction
	{
		public static byte UpperBound ()
		{
			return 0xBF;
		}

		readonly byte operand;
		public WebassemblyNumericInstruction (BinaryReader reader) 
		{
			this.opcode = reader.ReadByte ();
			if (this.opcode >= 0x41 && this.opcode <= 0x44) {

				//this.operand = reader.ReadByte ();
				// size of literal varies by type

			} else if (this.opcode > 0xBF) {
				throw new Exception ("Numerical opcode out of range");
			}
		}
	}
}



