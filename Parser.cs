
using System;
using System.IO;

namespace Wasm2CIL {
	public static class WebassemblySections
	{
		public const int Custom = 0;
		public const int Type = 1;
		public const int Import = 2;
		public const int Function = 3;
		public const int Table = 4;
		public const int Memory = 5;
		public const int Global = 6;
		public const int Export = 7;
		public const int Start = 8;
		public const int Element = 9;
		public const int Code = 10;
		public const int Data = 11;
	}

	public class Parser {
		const int WebassemblyMagic = 0x6d736100;
		const int WebassemblyVersion = 0x01;

		public void ParseSection (int section_num, byte [] section)
		{
			Console.WriteLine ("Parse section {0} length {1}", section_num, section.Length);

			switch (section_num) {
				case WebassemblySections.Custom:
					break;
				case WebassemblySections.Type:
					break;
				case WebassemblySections.Import:
					break;
				case WebassemblySections.Function:
					break;
				case WebassemblySections.Table:
					break;
				case WebassemblySections.Memory:
					break;
				case WebassemblySections.Global:
					break;
				case WebassemblySections.Export:
					break;
				case WebassemblySections.Start:
					break;
				case WebassemblySections.Element:
					break;
				case WebassemblySections.Code:
					break;
				case WebassemblySections.Data:
					break;
			}
		}

		public static ulong ParseLEBSigned (BinaryReader reader, int size_bits) {
			return ParseLEB (reader, Convert.ToUInt32 (size_bits), true);
		}

		public static ulong ParseLEBUnsigned (BinaryReader reader, int size_bits) {
			return ParseLEB (reader, Convert.ToUInt32 (size_bits), false);
		}

		public static ulong ParseLEB (BinaryReader reader, uint size_bits, bool signed)
		{
			// Taken from pseudocode here: https://en.wikipedia.org/wiki/LEB128
			ulong result = 0;
			int shift = 0;
			uint maxiters = (size_bits + 7 - 1) / 7;
			bool done = false;

			byte last_byte = 0;
			for (int i=0; i < maxiters; i++) {
				last_byte = reader.ReadByte ();

				uint low_order_7_bits = Convert.ToUInt32 (last_byte & 0x7f);
				uint high_order_bit = Convert.ToUInt32 (last_byte & 0x80);

				result |= (low_order_7_bits << shift);
				shift += 7;

				if (high_order_bit == 0) {
					done = true;
					break;
				}
			}

			if (!done)
					throw new Exception ("Overflow when parsing leb");

			// sign bit of byte is second high order bit (0x40)
			bool sign_bit_set = (last_byte & 0x40) != 0;
			// shift less than the entirety of the message
			bool shift_partial = (shift < size_bits);

			// Sign extend if all conditions met
			if (signed && shift_partial && sign_bit_set) {
				int diff = - (1 << shift);
				result |= Convert.ToUInt64 (diff);
			}

			return result;
		}

		public static void Main (string [] args) 
		{
			var inputPath = args [0];
			var parser = new Parser ();

			try
			{
				if (File.Exists(inputPath)) {
					using (BinaryReader reader = new BinaryReader(File.Open(inputPath, FileMode.Open))) {
						var magic_constant = reader.ReadInt32 ();
						if (magic_constant != Parser.WebassemblyMagic)
							throw new Exception (String.Format ("Unsupported magic number {0} != {1}", magic_constant, Parser.WebassemblyMagic));

						var version_constant = reader.ReadInt32 ();
						if (version_constant != Parser.WebassemblyVersion)
							throw new Exception (String.Format ("Unsupported version number {0} != {1}", magic_constant, Parser.WebassemblyVersion));

						// Read the sections
						while (reader.BaseStream.Position != reader.BaseStream.Length) {
							int id = (int) Parser.ParseLEBUnsigned (reader, 7);
							int len = (int) Parser.ParseLEBUnsigned (reader, 32);
							var this_section = reader.ReadBytes (len);
							// Read slen number bytes into section bytes
							// Process section
							// asynchronously parse?
							parser.ParseSection (id, this_section);
						}
					}
				} else {
					throw new Exception (String.Format ("Missing file {0}", inputPath));
				}
			}
			catch (FileNotFoundException ioEx)
			{
				Console.WriteLine("Error: {0}", ioEx.Message);
			}
		}
	}

}
