
using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

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

	public class WebassemblyResult
	{
		public static Type Convert (BinaryReader reader)
		{
			byte res = reader.ReadByte ();
			if (res == 0x40)
				return null;
			else
				return WebassemblyValueType.Convert (res);
		}
	}

	public static class WebassemblyValueType
	{
		public const byte I32 = 0x7F;
		public const byte I64 = 0x7E;
		public const byte F32 = 0x7D;
		public const byte F64 = 0x7C;

		public static Type Convert (byte key)
		{
			switch (key) {
				case 0x7F:
					return typeof (int);
				case 0x7E:
					return typeof (long);
				case 0x7D:
					return typeof (float);
				case 0x7C:
					return typeof (double);
				default:
					throw new Exception (String.Format ("Illegal value type {0:X}", key));
			}
		}
	}

	public class WebassemblyLocal
	{
		public readonly int Count;
		public readonly int valueTypeInit;

		public WebassemblyLocal (int count, int valueTypeInit)
		{
			this.Count = count;
			this.valueTypeInit = valueTypeInit;
		}

		public Type GetType () {
			if (Count == 1) {
				switch (valueTypeInit) {
					case WebassemblyValueType.I32:
						return typeof (int);
						break;
					case WebassemblyValueType.I64:
						return typeof (long);
						break;
					case WebassemblyValueType.F32:
						return typeof (float);
						break;
					case WebassemblyValueType.F64:
						return typeof (double);
						break;
				}
			}

			switch (valueTypeInit) {
				case WebassemblyValueType.I32:
					return typeof (int);
					break;
				case WebassemblyValueType.I64:
					return typeof (long);
					break;
				case WebassemblyValueType.F32:
					return typeof (float);
					break;
				case WebassemblyValueType.F64:
					return typeof (double);
					break;
			}

			throw new Exception ("Illegal local type");
		}
	}

	public class WebassemblyLimit
	{
		public readonly ulong min;

		// 0 signifies unlimited
		public readonly ulong max;

		public WebassemblyLimit (ulong min, ulong max)
		{
			this.min = min;
			this.max = max;
		}

		public WebassemblyLimit (BinaryReader reader)
		{
			int kind = Convert.ToInt32 (Parser.ParseLEBSigned (reader,  32));
			this.min = Parser.ParseLEBUnsigned (reader,  32);

			if (kind == 0x1)
				this.max = Parser.ParseLEBUnsigned (reader,  32);
			else if (kind == 0x0)
				this.max = 0;
			else
				throw new Exception ("Wrong limit type");
		}
	}

    public class WebassemblyImport
    {
        public readonly string module;
        public readonly string name;
        public readonly int kind;
        public readonly ulong index;

        public WebassemblyImport(BinaryReader reader)
        {
            module = Parser.ReadString(reader);
            name = Parser.ReadString(reader);
            kind = Convert.ToInt32(Parser.ParseLEBUnsigned(reader, 7));
            index = Parser.ParseLEBUnsigned(reader,32);
        }
    }

	public class WebassemblyMemory
	{
		public readonly WebassemblyLimit limit;

		public WebassemblyMemory (WebassemblyLimit limit)
		{
			this.limit = limit;
		}
	}

	public class WebassemblyTable
	{
		public readonly WebassemblyLimit limit;
		public readonly ulong elementType;

		public WebassemblyTable (ulong elementType, WebassemblyLimit limit)
		{
			this.limit = limit;
			this.elementType = elementType;
		}
	}

	public class WebassemblyGlobal
	{
		// WebassemblyValueType
		public readonly int valueType;
		public readonly bool mutable;
		public readonly WebassemblyExpression init;

		public WebassemblyGlobal (BinaryReader reader)
		{
			valueType = Convert.ToInt32 (Parser.ParseLEBSigned (reader, 7));
			long mut = Parser.ParseLEBSigned (reader, 7);
			init = new WebassemblyExpression (reader);
		}
	}

    public class WebassemblyExport
    {
        public readonly string name;
        public readonly uint kind;
        public readonly ulong index;

        public WebassemblyExport (BinaryReader reader)
        {
            name = Parser.ReadString(reader);
            kind = Convert.ToUInt32(Parser.ParseLEBUnsigned(reader, 8));
            index = Parser.ParseLEBUnsigned(reader, 32);
        }
    }

	public class WebassemblyElementInit
	{
		public readonly long index;
		public readonly byte [] body;
		public readonly WebassemblyExpression expr;

		public WebassemblyElementInit (BinaryReader reader)
		{
			index = Parser.ParseLEBSigned (reader, 32);
			// assert table index is 0, only one allowed in this version
			if (index != 0)
				throw new Exception ("At most one table allowed in this version of webassembly");

			expr = new WebassemblyExpression (reader);

			long body_size = Parser.ParseLEBSigned (reader, 32);
			body = reader.ReadBytes (Convert.ToInt32 (body_size));
		}
	}

	public class WebassemblyDataInit
	{
		public readonly long index;
		public readonly byte [] body;
		public readonly WebassemblyExpression expr;

		public WebassemblyDataInit (BinaryReader reader)
		{
			index = Parser.ParseLEBSigned (reader, 32);
			// assert table index is 0, only one allowed in this version
			if (index != 0)
				throw new Exception ("At most one memory allowed in this version of webassembly");

			expr = new WebassemblyExpression (reader);

			long body_size = Parser.ParseLEBSigned (reader, 32);
			body = reader.ReadBytes (Convert.ToInt32 (body_size));
		}
	}

	public class WebassemblyFunc
	{
		public readonly WebassemblyLocal [] locals;
		public readonly int num_locals;
		public readonly WebassemblyExpression expr;

		public WebassemblyFunc (BinaryReader reader) 
		{
			int num_local_types = Convert.ToInt32 (Parser.ParseLEBSigned (reader, 32));
			this.locals = new WebassemblyLocal [num_local_types];
			this.num_locals = 0;

			for (int local=0; local < num_local_types; local++) {
				// Size of local in count of 32-bit segments
				int count_of_locals = Convert.ToInt32 (Parser.ParseLEBSigned (reader, 7));
				int valueTypeInit = Convert.ToInt32 (Parser.ParseLEBSigned (reader, 7));
				this.locals [local] = new WebassemblyLocal (count_of_locals, valueTypeInit);
				this.num_locals += count_of_locals;
				Console.WriteLine ("Parsed code section local: Count {0} Type {1}", count_of_locals, valueTypeInit);
			}
			Console.WriteLine ("Parsed code section one {0} locals", num_locals);
			this.expr = new WebassemblyExpression (reader, true);
		}

		public MethodInfo Emit (WebassemblyFunctionType type, TypeBuilder tb, string name)
		{
			var param_types = type.EmitParams ();
			var return_type = type.EmitReturn ();
			var method = tb.DefineMethod (name, MethodAttributes.Static | MethodAttributes.Public, return_type, param_types);
			var ilgen = method.GetILGenerator ();

			var outputLocals = new LocalBuilder [num_locals];
			var index = 0;

			for (int i=0; i < locals.Length; i++) {
				var ty = locals [i].GetType ();
				for (int j=0; j < locals [i].Count; j++)
					outputLocals [index++] = ilgen.DeclareLocal (ty);
			}

			expr.Body.Emit (ilgen, param_types.Length);

			return method;
		}
	}

	public class WebassemblyExpression
	{
		public readonly WebassemblyFunctionBody Body;

		public WebassemblyExpression (BinaryReader reader): this (reader, false)
		{
		}

		public WebassemblyExpression (BinaryReader reader, bool readToEnd) 
		{
			Body = new WebassemblyFunctionBody (reader);

			if (readToEnd && (reader.BaseStream.Position != reader.BaseStream.Length))
				throw new Exception ("Didn't actually read to end");
		}
	}

	public class WebassemblyFunctionType
	{
		readonly long form;
		readonly ulong[] parameters;
		readonly ulong[] results;

		public Type [] EmitParams ()
		{
			if (parameters.Length == 0)
				return null;

			var accum = new List<Type> ();
			foreach (var res in parameters)
				accum.Add (WebassemblyValueType.Convert ((byte) res));

			return accum.ToArray ();
		}

		public Type EmitReturn ()
		{
			if (results.Length == 0)
				return null;

			if (results.Length == 1)
				return WebassemblyValueType.Convert ((byte) this.results [0]);

			throw new Exception ("Result type array may only have length 1 in this version of Webassembly");
		}

		public WebassemblyFunctionType (long form, ulong[] parameters, ulong[] results)
		{
			this.form = form;
			this.parameters = parameters;
			this.results = results;
		}
	}

	public class Parser {
		const int WebassemblyMagic = 0x6d736100;
		const int WebassemblyFunctionEnd = 0x0b;
		const int WebassemblyVersion = 0x01;

		// Function index is into table of both imported functions and
		// defined functions. So fn_idx is not valid into types [], must subtract imported
		WebassemblyFunctionType [] types;
		WebassemblyFunc [] exprs;
		// function_index_to_type_index_map. fn_to_type [fn_idx] = type_idx
		ulong [] fn_to_type;
		long start_idx;

		WebassemblyGlobal [] globals;
		WebassemblyElementInit [] elements;
		WebassemblyDataInit [] data;
		WebassemblyTable table;
		WebassemblyMemory mem;
        WebassemblyExport [] exports;
        WebassemblyImport [] imports;

		// Can only be called after all sections are done parsing
		public void Emit (string outputName)
		{
			var assemblyName = String.Format ("{0}Proxy", outputName);
			AssemblyName aName = new AssemblyName (assemblyName);
			AssemblyBuilder ab = AppDomain.CurrentDomain.DefineDynamicAssembly (aName, AssemblyBuilderAccess.RunAndSave);
			var outputFileExtension = start_idx == -1 ? ".dll" : ".exe";
			var outputFileName = assemblyName + outputFileExtension;
			ModuleBuilder mb = ab.DefineDynamicModule(aName.Name, outputFileName);

			TypeBuilder tb = mb.DefineType (outputName, TypeAttributes.Public);

			// fixme: imports / exports?
			for (int i=0; i < exprs.Length; i++) {
				var fn = exprs [i];
				var type = types [fn_to_type [i]];
				var fn_name = String.Format ("Function{0}", i);
				var emitted = fn.Emit (type, tb, fn_name);

				var dummy_entry = tb.DefineMethod ("Main", MethodAttributes.HideBySig | MethodAttributes.Static | MethodAttributes.Public, typeof(void), new Type[] { typeof(string[]) });
				var dummy_gen = dummy_entry.GetILGenerator ();
				dummy_gen.Emit(OpCodes.Ldarg, 0);
				dummy_gen.Emit(OpCodes.Ldc_I4_0, 0);
				dummy_gen.Emit(OpCodes.Ldelem_Ref, 0);
				dummy_gen.Emit(OpCodes.Call, typeof (System.Convert).GetMethod("ToInt32", new Type [] {typeof (string)}));
				dummy_gen.Emit(OpCodes.Call, emitted);
				dummy_gen.Emit(OpCodes.Call, typeof (System.Console).GetMethod ("WriteLine", new Type [] {typeof (double)}));
				dummy_gen.Emit(OpCodes.Ret);
				ab.SetEntryPoint (dummy_entry);

				tb.CreateType ();
			}

			ab.Save (outputFileName);
		}

		public void ParseTypeSection (BinaryReader reader)
		{
			int num_types = Convert.ToInt32 (Parser.ParseLEBSigned (reader, 32));
			Console.WriteLine ("Parse type section:  #types: {0}", num_types);

			this.types = new WebassemblyFunctionType [num_types];

			for (int i = 0; i < num_types; i++) {
				var form = Parser.ParseLEBSigned (reader, 7);
				var parameters = Parser.ParseLEBUnsignedArray (reader);
				var results = Parser.ParseLEBUnsignedArray (reader);
				var type = new WebassemblyFunctionType (form, parameters, results);
				this.types [i] = type;
			}
		}

		public void ParseFunctionSection (BinaryReader reader)
		{
			fn_to_type = ParseLEBUnsignedArray (reader);
			Console.WriteLine ("Parse function section:  #types: {0} ", fn_to_type.Length);
		}

		public void ParseStartSection(BinaryReader reader)
		{
			start_idx = (long) Parser.ParseLEBUnsigned (reader, 32);
			Console.WriteLine ("Parse start section:  #index: {0} ", start_idx);
		}

		public void ParseCodeSection (BinaryReader sectionReader)
		{
			int count = Convert.ToInt32 (Parser.ParseLEBSigned (sectionReader, 32));
			exprs = new WebassemblyFunc [count];

			for (int i=0; i < count; i++) {
				int size_of_entry = Convert.ToInt32 (Parser.ParseLEBSigned (sectionReader, 32));
				// doing now so I can parallelize lower parsing later
				byte [] entry = sectionReader.ReadBytes (size_of_entry);

				using (BinaryReader bodyReader = new BinaryReader (new MemoryStream (entry))) {
					exprs [i] = new WebassemblyFunc (bodyReader);
				}
			}

			Console.WriteLine ("Parsed code section done");
		}

		public void ParseMemorySection(BinaryReader reader)
		{
			var count = Convert.ToInt32 (Parser.ParseLEBSigned (reader, 32));
			if (count != 1)
				throw new Exception ("At most one memory allowed in this version of webassembly");
			this.mem = new WebassemblyMemory (new WebassemblyLimit (reader));

			Console.WriteLine ("Parsed memory section. Limit is {0} {1}", this.mem.limit.min, this.mem.limit.max);
		}

		public void ParseTableSection(BinaryReader reader)
		{
			var count = Convert.ToInt32 (Parser.ParseLEBSigned (reader, 32));
			if (count != 1)
				throw new Exception ("At most one table allowed in this version of webassembly");
			var elementType = Parser.ParseLEBUnsigned (reader, 7);
			var limit = new WebassemblyLimit (reader);
			this.table = new WebassemblyTable (elementType, limit);

			Console.WriteLine ("Parsed table section. Limit is {0} {1}", limit.min, limit.max);
		}

		public void ParseGlobalSection(BinaryReader reader)
		{
			var count = Convert.ToInt32 (Parser.ParseLEBSigned (reader, 32));
			this.globals = new WebassemblyGlobal [count];

			for (int i=0; i < count; i++)
				this.globals [i] = new WebassemblyGlobal (reader);

			Console.WriteLine ("Parsed global section, {0}", count);
		}

		public void ParseElementSection(BinaryReader reader)
		{
			var count = Convert.ToInt32 (Parser.ParseLEBSigned (reader, 32));
			this.elements = new WebassemblyElementInit [count];

			for (int i=0; i < count; i++)
				this.elements [i] = new WebassemblyElementInit (reader);

			Console.WriteLine ("Parsed element section, {0}", count);
		}

		public void ParseDataSection(BinaryReader reader)
		{
			var count = Convert.ToInt32 (Parser.ParseLEBSigned (reader, 32));
			this.data = new WebassemblyDataInit [count];

			for (int i=0; i < count; i++)
				this.data [i] = new WebassemblyDataInit (reader);

			Console.WriteLine ("Parsed data section, {0}", count);
		}

		public void ParseImportSection(BinaryReader reader)
		{
            var count = Convert.ToInt32(Parser.ParseLEBUnsigned(reader, 32));
            this.imports = new WebassemblyImport [count];

            for (int i = 0; i < count; i++)
                this.imports[i] = new WebassemblyImport(reader);

            Console.WriteLine("Parsed import section, {0}", count);
        }

        public void ParseExportSection(BinaryReader reader)
		{
            var count = Convert.ToInt32(Parser.ParseLEBUnsigned(reader,32));
            this.exports = new WebassemblyExport [count];

            for (int i = 0; i < count; i++)
                this.exports[i] = new WebassemblyExport(reader);

            Console.WriteLine("Parsed export section, {0}", count);
        }

		public void ParseSection (int section_num, byte [] section)
		{
			Console.WriteLine ("Parse section {0} length {1}", section_num, section.Length);
			var memory = new MemoryStream (section);

			using (BinaryReader reader = new BinaryReader (memory)) {
				switch (section_num) {
					case WebassemblySection.Custom:
						// We don't have anything here for mono
						break;
					case WebassemblySection.Type:
						ParseTypeSection (reader);
						break;
					case WebassemblySection.Import:
						ParseImportSection (reader);
						break;
					case WebassemblySection.Function:
						ParseFunctionSection (reader);
						break;
					case WebassemblySection.Table:
						ParseTableSection (reader);
						break;
					case WebassemblySection.Memory:
						ParseMemorySection (reader);
						break;
					case WebassemblySection.Global:
						ParseGlobalSection (reader);
						break;
					case WebassemblySection.Export:
						ParseExportSection (reader);
						break;
					case WebassemblySection.Start:
						ParseStartSection (reader);
						break;
					case WebassemblySection.Element:
						ParseElementSection (reader);
						break;
					case WebassemblySection.Code:
						ParseCodeSection (reader);
						break;
					case WebassemblySection.Data:
						ParseDataSection (reader);
						break;
				}
			}
		}

		public static ulong[] ParseLEBUnsignedArray (BinaryReader reader) 
		{
			int len = Convert.ToInt32 (Parser.ParseLEBSigned (reader, 32));
			var accum = new ulong [len];

			for (int i=0; i < len; i++)
				accum [i] = Parser.ParseLEBUnsigned (reader, 32);

			return accum;
		}

		public static long ParseLEBSigned (BinaryReader reader, int size_bits) {
			return (long) ParseLEB (reader, Convert.ToUInt32 (size_bits), true);
		}

		public static ulong ParseLEBUnsigned (BinaryReader reader, int size_bits) {
			return (ulong) ParseLEB (reader, Convert.ToUInt32 (size_bits), false);
		}

        public static string ReadString(BinaryReader reader)
        {
            int length = Convert.ToInt32(Parser.ParseLEBUnsigned(reader, 32));
            char[] chars = reader.ReadChars(length);
            return new string(chars);
        }

        public static IntPtr ParseLEB (BinaryReader reader, uint size_bits, bool signed)
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

			return (IntPtr) result;
		}

		public Parser () 
		{
			start_idx = -1;
		}

		public static void Main (string [] args) 
		{
			var inputPath = args [0];
			var outputName = args [1];
			var parser = new Parser ();

			try
			{
				if (File.Exists (inputPath)) {
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

						parser.Emit (outputName);
					}
				} else {
					throw new Exception (String.Format ("Missing file {0}", inputPath));
				}
			}
			catch (FileNotFoundException ioEx)
			{
				Console.WriteLine("Error: {0}", ioEx.Message);
			}
            Console.ReadKey();
		}
	}
}
