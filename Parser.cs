
using System;
using System.IO;

namespace Wasm2CIL {
	public static class WebassemblySection
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

	public class WebassemblyType
	{
		readonly ulong form;
		readonly ulong[] parameters;
		readonly ulong[] results;

		public WebassemblyType (ulong form, ulong[] parameters, ulong[] results)
		{
			this.form = form;
			this.parameters = parameters;
			this.results = results;
		}
	}

	public class Parser {
		const int WebassemblyMagic = 0x6d736100;
		const int WebassemblyVersion = 0x01;

		WebassemblyType [] types;
		// function_index_to_type_index_map. fn_to_type [fn_idx] = type_idx
		ulong [] fn_to_type;
		ulong start_idx;

		public void ParseTypeSection (BinaryReader reader)
		{
			int num_types = Convert.ToInt32 (Parser.ParseLEBSigned (reader, 32));
			Console.WriteLine ("Parse type section:  #types: {0}", num_types);

			this.types = new WebassemblyType [num_types];

			for (int i = 0; i < num_types; i++) {
				var form = Parser.ParseLEBSigned (reader, 7);
				var parameters = Parser.ParseLEBSignedArray (reader);
				var results = Parser.ParseLEBSignedArray (reader);
				var type = new WebassemblyType (form, parameters, results);
				this.types [i] = type;
			}
		}

		public void ParseFunctionSection (BinaryReader reader)
		{
			fn_to_type = ParseLEBSignedArray (reader);
			Console.WriteLine ("Parse function section:  #types: {0} ", fn_to_type.Length);
		}

		public void ParseStartSection(BinaryReader reader)
		{
			start_idx = Parser.ParseLEBSigned (reader, 32);
			Console.WriteLine ("Parse start section:  #index: {0} ", start_idx);
		}

		public void ParseCodeSection (BinaryReader reader)
		{
		}

		public void ParseSection (int section_num, byte [] section)
		{
			Console.WriteLine ("Parse section {0} length {1}", section_num, section.Length);
			var memory = new MemoryStream (section);

			using (BinaryReader reader = new BinaryReader (memory)) {
				switch (section_num) {
					case WebassemblySection.Custom:
						break;
					case WebassemblySection.Type:
						ParseTypeSection (reader);
						break;
					case WebassemblySection.Import:
						break;
					case WebassemblySection.Function:
						ParseFunctionSection (reader);
						break;
					case WebassemblySection.Table:
						break;
					case WebassemblySection.Memory:
						break;
					case WebassemblySection.Global:
						break;
					case WebassemblySection.Export:
						break;
					case WebassemblySection.Start:
						break;
					case WebassemblySection.Element:
						break;
					case WebassemblySection.Code:
						break;
					case WebassemblySection.Data:
						break;
				}
			}
		}

		public static ulong[] ParseLEBSignedArray (BinaryReader reader) 
		{
			int len = Convert.ToInt32 (Parser.ParseLEBSigned (reader, 32));
			var accum = new ulong [len];

			for (int i=0; i < len; i++)
				accum [i] = Parser.ParseLEBSigned (reader, 32);

			return accum;
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
				result |= (ulong) (((long) ~0) << shift);
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
