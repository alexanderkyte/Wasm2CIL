using System;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Collections.Generic;

namespace Wasm2CIL {
	public class WebassemblyCodeParser
	{
		public readonly bool ElseTerminated;
		public WebassemblyInstruction [] body;

		private bool finished;

		public int ParamCount;

		public void Emit (ILGenerator ilgen, int num_params)
		{
			Console.WriteLine ("Emitting the following instructions");
			foreach (var instr in body)
				Console.WriteLine (instr.ToString ());

			this.ParamCount = num_params;
			var iter = BodyBuilder.GetEnumerator ();

			while (true) {
				var valid = iter.MoveNext ();
				if (!valid)
					break;

				((WebassemblyInstruction) iter.Current).Emit (iter, ilgen, this);
			}
		}

		public void Add (WebassemblyInstruction instr, ref int depth) {
			if (instr == null)
				throw new Exception ("Cannot add null instructions");

			BodyBuilder.Add (instr);

			var brancher = instr as WebassemblyControlInstruction;
			if (brancher == null)
				return;

			if (brancher.EndsBlock ()) {
				//Console.WriteLine ("End block");
				// Tracks whether we've hit the extra 0x0b that marks end-of-function
				depth -= 1;
				if (depth != -1)
					LabelStack.RemoveAt (LabelStack.Count - 1);
			} 
			if (brancher.StartsBlock ()) {
				//Console.WriteLine ("Start block");
				depth += 1;
				LabelStack.Add (brancher);
			}
		}

		private int CurrLabel; // Incremented by control instructions
		private List<WebassemblyInstruction> BodyBuilder;
		private List<WebassemblyControlInstruction> LabelStack;

		public WebassemblyCodeParser () {
			BodyBuilder = new List<WebassemblyInstruction> ();
			LabelStack = new List<WebassemblyControlInstruction> ();
			CurrLabel = 0;
		}

		public WebassemblyCodeParser (BinaryReader reader): this () {
			ParseExpression (reader);
			FinishParsing ();
		}

		public WebassemblyInstruction Current ()
		{
			return this.BodyBuilder [this.BodyBuilder.Count - 1];
		}

		public void ParseExpression (BinaryReader reader)
		{
			int start = BodyBuilder.Count;

			if (this.finished)
				throw new Exception ("Expression block has already been finalized.");

			// The "depth" is used to find the extra 0x0B at the end of an expression.
			// When we see it and depth != 0, then it ends a block, not the function
			//
			// We make the nested blocks in if/else bodies without labels parse with
			// the included logic as well, so an Else can also end this block
			int depth = 0;

			while (depth >= 0 && (reader.BaseStream.Position != reader.BaseStream.Length)) {
				if (depth > LabelStack.Count)
					throw new Exception (String.Format ("Depth {0} exceeds number of labels on stack {1}", depth, LabelStack.Count));

				Console.WriteLine ("Depth {0} and number of labels on stack {1}", depth, LabelStack.Count);

				WebassemblyInstruction result = null;
				byte opcode = reader.ReadByte ();

				if (opcode <= WebassemblyControlInstruction.UpperBound ()) {
					// The appending is done inside the control instruction so that it can ensure ordering
					// So we don't assign to result
					new WebassemblyControlInstruction (opcode, reader, LabelStack, ref CurrLabel, this, ref depth);
				} else if (opcode <= WebassemblyParametricInstruction.UpperBound ()) {
					result = new WebassemblyParametricInstruction (opcode, reader);
				} else if (opcode <= WebassemblyVariableInstruction.UpperBound ()) {
					result = new WebassemblyVariableInstruction (opcode, reader);
				} else if (opcode <= WebassemblyMemoryInstruction.UpperBound ()) {
					result = new WebassemblyMemoryInstruction (opcode, reader);
				} else if (opcode <= WebassemblyNumericInstruction.UpperBound ()) {
					result = new WebassemblyNumericInstruction (opcode, reader);
				} else {
					throw new Exception (String.Format ("Illegal instruction {0:X}", opcode));
				}

				if (result != null) {
					Add (result, ref depth);
				}
			}

			Console.WriteLine ("Parsed {0} wasm instructions", BodyBuilder.Count - start);
		}

		public void FinishParsing ()
		{
			this.body = BodyBuilder.ToArray ();
			this.finished = true;
		}
	}

	// Make parser return collection of basic blocks, not instructions

	public abstract class WebassemblyInstruction
	{
		public const byte End = 0x0b;
		public const byte Else = 0x05;

		public readonly byte Opcode;

		public WebassemblyInstruction (byte opcode)
		{
			this.Opcode = opcode;
		}

		public abstract string ToString ();
		public abstract void Emit (IEnumerator<WebassemblyInstruction> cursor, ILGenerator ilgen, WebassemblyCodeParser top_level);
	}

	public class WebassemblyControlInstruction : WebassemblyInstruction
	{
		Label label;
		public readonly int? LabelIndex;

		ulong [] table;
		ulong index;
		ulong default_target;
		Type block_type;
		ulong function_index;
		ulong type_index;
		public WebassemblyControlInstruction dest;

		bool loops;

		// These are the expressions that are the
		public readonly WebassemblyControlInstruction if_block;
		public readonly WebassemblyControlInstruction else_block;
		public readonly WebassemblyControlInstruction fallthrough_block;

		public override void Emit (IEnumerator<WebassemblyInstruction> cursor, ILGenerator ilgen, WebassemblyCodeParser top_level)
		{
			switch (Opcode) {
				case 0x0: // unreachable
					// Fixme: make this catchable / offer options at exception time
					ilgen.ThrowException (typeof (System.ExecutionEngineException));
					return;
				case 0x01: // nop
					ilgen.Emit (OpCodes.Nop);
					return;

				case 0x02: // block
					label = ilgen.DefineLabel ();
					ilgen.MarkLabel (label);
					return;

				case 0x03: // loop
					label = ilgen.DefineLabel ();
					ilgen.MarkLabel (label);
					return;

				case 0x04: // if 
					var fallthrough_label = ilgen.DefineLabel ();

					WebassemblyInstruction curr = cursor.Current;
					if (curr != this)
						throw new Exception (String.Format ("Cursor has passed us while we were emitting instruction"));
					if (curr != if_block)
						throw new Exception (String.Format ("if block limits not correctly parsed"));

					if (this.else_block != null) {
						Label else_label = ilgen.DefineLabel ();
						ilgen.Emit (OpCodes.Brfalse, else_label);

						Console.WriteLine ("GREP: if: {0}", this.if_block.ToString ());
						Console.WriteLine ("GREP: else: {0}", this.else_block.ToString ());
						Console.WriteLine ("GREP: fallthrough: {0}", this.fallthrough_block.ToString ());

						while (curr != this.else_block) {
							var good = cursor.MoveNext ();
							if (!good)
								throw new Exception ("if/else block limits not correctly parsed");
							curr = cursor.Current;
							curr.Emit (cursor, ilgen, top_level);
						}
						if (if_block == else_block)
							ilgen.Emit (OpCodes.Nop);

						ilgen.Emit (OpCodes.Br, fallthrough_label);


						// We emit the else block
						ilgen.MarkLabel (else_label);
						while (curr != this.fallthrough_block) {
							var good = cursor.MoveNext ();
							if (!good)
								throw new Exception ("Else/fallthrough block limits not correctly parsed");
							curr = cursor.Current;
							curr.Emit (cursor, ilgen, top_level);
						}
						if (else_block == fallthrough_block)
							ilgen.Emit (OpCodes.Nop);

						// Falls through to fallthrough by default
					} else {
						// No else block

						Console.WriteLine ("GREP: if: {0}", this.if_block.ToString ());
						Console.WriteLine ("GREP: fallthrough: {0}", this.fallthrough_block.ToString ());

						ilgen.Emit (OpCodes.Brfalse, fallthrough_label);
						if (if_block == fallthrough_block)
							ilgen.Emit (OpCodes.Nop);
						while (curr != this.fallthrough_block) {
								Console.WriteLine ("Between if and fallthrough: {0} < {1}", cursor.Current.ToString (), fallthrough_block.ToString ());
								var good = cursor.MoveNext ();
								if (!good)
									throw new Exception ("if/fallthrough block limits not correctly parsed");
								curr = cursor.Current;
								curr.Emit (cursor, ilgen, top_level);
						}
					}

					// Start fallthrough symbol, for rest of code
					ilgen.MarkLabel (fallthrough_label);
					return;

				case 0x05: // Else
					ilgen.Emit (OpCodes.Nop);
					return;

				case 0x0b: // End
					// loops fall through
					if (this.dest == null && !this.loops) {
						// ends function body, has implicit return
						ilgen.Emit (OpCodes.Ret);
					}
					//else {
						//ilgen.Emit (OpCodes.Nop);
					//}
					return;

				// Br
				case 0x0c:
					ilgen.Emit (OpCodes.Br, this.dest.GetLabel ());
					return;

				// Br_if
				case 0x0d:
					ilgen.Emit (OpCodes.Brtrue, this.dest.GetLabel ());
					return;

				// Br_table
				//case 0x0e:
					//return ilgen.Emit (OpCodes.Nop);

				case 0x0f:
					ilgen.Emit (OpCodes.Ret);
					return;

				//// Call
				case 0x10:
					ilgen.Emit (OpCodes.Nop);
					return;

				//case 0x11:
					//return ilgen.Emit (OpCodes.Nop);

				default:
					throw new Exception (String.Format("Should not be reached: {0:X}", Opcode));
			}
			return;
		}

		public override string ToString () 
		{
			switch (Opcode) {
				case 0x0:
					return "unreachable";
				case 0x01:
					return "nop";
				case 0x02:
					return String.Format ("block {0} {1}", block_type, GetLabelName ());
				case 0x03:
					return String.Format ("loop {0} {1}", block_type, GetLabelName ());
				case 0x04:
					var block_type_str = "";
					if (block_type != null)
						block_type_str = String.Format ("Type: {0}", block_type);

					return String.Format ("if {0} {1}", block_type, GetLabelName ());
				case 0x05:
					return "else";
				case 0x0b:
					if (this.dest == null)
						return String.Format ("end (Of Function)");
					else
						return String.Format ("end {0}", this.dest.GetLabelName ());
				case 0x0c:
					return String.Format ("br {0}", this.dest.GetLabelName ());
				case 0x0d:
					return String.Format ("br_if {0}", this.dest.GetLabelName ());
				case 0x0e:
					return "br_table";
				case 0x0f:
					return "return";
				case 0x10:
					return String.Format ("call {0}", this.function_index);
				case 0x11:
					return String.Format ("call_indirect {0}", this.type_index);
				default:
					throw new Exception (String.Format("Should not be reached: {0:X}", Opcode));
			}
		}

		public string GetLabelName ()
		{
			if (!this.StartsBlock ())
				throw new Exception ("Does not create label");

			if (Opcode == 0x05) {
				if (this.dest != null)
					return this.dest.GetLabelName ();
				else
					return "";
			}

			if (this.LabelIndex == null)
				throw new Exception (String.Format ("Did not create label for 0x{0:x}", Opcode));

			return String.Format ("@{0}", this.LabelIndex);
		}

		public Label GetLabel ()
		{
			if (label == null && this.StartsBlock ())
				throw new Exception ("Did not emit label when traversing this instruction");
			else if (label == null)
				throw new Exception ("Does not create label");
			else
				return label;
		}


		public bool StartsBlock ()
		{
			return Opcode == 0x02 || Opcode == 0x03 || Opcode == 0x04 || Opcode == 0x05;
		}

		public bool EndsBlock ()
		{
			return Opcode == 0x0b || Opcode == 0x05;
		}

		public static byte UpperBound ()
		{
			return 0x11;
		}

		public WebassemblyControlInstruction (byte opcode, BinaryReader reader, List<WebassemblyControlInstruction> labels, ref int labelIndex, WebassemblyCodeParser parser, ref int depth): base (opcode)
		{
			switch (this.Opcode) {
				case 0x0B: // end
				case 0x05: // else
					// dest set by if statement
					break;
				case 0x0: // unreachable
				case 0x1: // nop
				case 0x0F: // return
					// No args
					break;
				case 0x0C: // br
				case 0x0D: // br_if
					// So these indexes are labels
					// each loop, block, 

					// All branching is to previous labels
					// This means that the most foolproof way to emit things is
					// to preverse all the ordering in the initial instruction stream.
					// When we emit this instruction, dest will already have had a label emitted.
					this.index = Parser.ParseLEBUnsigned (reader, 32);
					Console.WriteLine ("index {0}", this.index);

					if (labels.Count == 0)
						throw new Exception ("Branching with empty label stack!");

					int offset = (labels.Count - 1) - (int) this.index;
					if (offset >= labels.Count || offset < 0)
						throw new Exception (String.Format ("branch label of {0} {1} {2} not acceptable", labels.Count, this.index, offset));

					this.dest = labels [offset];
					if (this.dest == null)
						throw new Exception ("Could not find destination of jump");

					break;

				case 0x02: // block
					// need to make label 
					this.block_type = WebassemblyResult.Convert (reader);
					this.LabelIndex = labelIndex++;
					this.loops = false;
					break;

				case 0x03: // loop 
					// need to make label 
					this.block_type = WebassemblyResult.Convert (reader);
					this.LabelIndex = labelIndex++;
					this.loops = true;
					break;

				case 0x04: // if
					// Ensure this opcode comes before nested/subsequent blocks
					this.LabelIndex = labelIndex++;
					parser.Add (this, ref depth);
					this.block_type = WebassemblyResult.Convert (reader);

					// We have a stack of "expressions" we are writing to
					//public readonly WebassemblyInstruction [] body;

					this.if_block = this;
					parser.ParseExpression (reader);

					if (parser.Current ().Opcode == 0x05) {
						this.else_block = (WebassemblyControlInstruction) parser.Current ();
						this.else_block.dest = this.if_block;
						Console.WriteLine ("Else is {0}", this.else_block.ToString ());
						parser.ParseExpression (reader);
					} 
					
					this.fallthrough_block = (WebassemblyControlInstruction) parser.Current ();
					if (this.fallthrough_block.Opcode != 0x0B)
						throw new Exception (String.Format ("If statement terminated with illegal opcode 0x0{0:X}", parser.Current ().Opcode));

					this.fallthrough_block.dest = this.if_block;
					Console.WriteLine ("Fallthrough is {0}", this.fallthrough_block.ToString ());

					return; // Don't double-add the instruction

				case 0x0e: // br_table
					// works by getting index from stack. If index is in range of table,
					// we jump to the label at that index. Else go to default.
					this.table = Parser.ParseLEBUnsignedArray (reader);
					this.default_target = Parser.ParseLEBUnsigned (reader, 32);
					break;
				case 0x10: //call
					this.function_index = Parser.ParseLEBUnsigned (reader, 32);
					break;
				case 0x11: //call indirect
					this.type_index = Parser.ParseLEBUnsigned (reader, 32);
					var endcap = Parser.ParseLEBUnsigned (reader, 32);
					if (endcap != 0x0)
						throw new Exception ("Call indirect call not ended with 0x0");
					break;
				default:
					throw new Exception (String.Format ("Control instruction out of range {0:X}", this.Opcode));
			}

			// Ensure this opcode comes before nested/subsequent blocks
			parser.Add (this, ref depth);
		}
	}

	public class WebassemblyParametricInstruction : WebassemblyInstruction
	{
		public static byte UpperBound ()
		{
			return 0x1B;
		}

		public override void Emit (IEnumerator<WebassemblyInstruction> cursor, ILGenerator ilgen, WebassemblyCodeParser top_level)
		{
			throw new Exception (String.Format("Should not be reached: {0:X}", Opcode));
		}

		public override string ToString () 
		{
			switch (Opcode) {
				case 0x1a:
					// CIL: pop
					return "drop";
				case 0x1b:
					// CIL: dup + pop
					return "select";
				default:
					throw new Exception (String.Format("Should not be reached: {0:X}", Opcode));
			}
		}

		public WebassemblyParametricInstruction (byte opcode, BinaryReader reader): base (opcode)
		{
			if (this.Opcode != 0x1A && this.Opcode <= 0x1B) {
				throw new Exception ("Parametric opcode out of range");
			}
		}
	}

	public class WebassemblyVariableInstruction : WebassemblyInstruction
	{
		// If the index is lower than the number of parameters, this is a
		// local reference, else it is a parameter reference
		ulong index;

		void EmitGetter (ILGenerator ilgen, int num_params)
		{
			// Fixme: use packed encodings (_s) and the opcodes that mention the index

			if ((int) index < num_params) {
				Console.WriteLine ("ldarg {0}", index);
				// The +1 is because the first argument is the "this" argument
				switch (index) {
					case 0:
						ilgen.Emit (OpCodes.Ldarg_1);
						break;
					case 1:
						ilgen.Emit (OpCodes.Ldarg_2);
						break;
					case 2:
						ilgen.Emit (OpCodes.Ldarg_3);
						break;
					default:
						ilgen.Emit (OpCodes.Ldarg, index + 1);
						break;
				}
			} else {
				//Console.WriteLine ("ldloc {0}", labelIndex);
				int labelIndex = (int) index - num_params;
				switch (labelIndex) {
					case 0:
						ilgen.Emit (OpCodes.Ldarg_0);
						break;
					case 1:
						ilgen.Emit (OpCodes.Ldarg_1);
						break;
					case 2:
						ilgen.Emit (OpCodes.Ldarg_2);
						break;
					case 3:
						ilgen.Emit (OpCodes.Ldarg_3);
						break;
					default:
						ilgen.Emit (OpCodes.Ldarg, labelIndex);
						break;
				}
				ilgen.Emit (OpCodes.Ldloc, labelIndex);
			}
		}

		void EmitSetter (ILGenerator ilgen, int num_params)
		{
			// Fixme: use packed encodings (_s) and the opcodes that mention the index

			if ((int) index < num_params) {
				ilgen.Emit (OpCodes.Starg, index);
				//Console.WriteLine ("starg {0}", index);
				return;
			} else {
				//Console.WriteLine ("stloc {0}", labelIndex);
				int labelIndex = (int) index - num_params;
				ilgen.Emit (OpCodes.Stloc, labelIndex);
			}
		}

		public override void Emit (IEnumerator<WebassemblyInstruction> cursor, ILGenerator ilgen, WebassemblyCodeParser top_level)
		{
			switch (Opcode) {
				case 0x20:
					EmitGetter (ilgen, top_level.ParamCount);
					return;
				case 0x21:
					EmitSetter (ilgen, top_level.ParamCount);
					return;
				case 0x22:
					EmitSetter (ilgen, top_level.ParamCount);
					EmitGetter (ilgen, top_level.ParamCount);
					return;
				//case 0x23:
					//return String.Format ("get_global {0}", index);
				//case 0x24:
					//return String.Format ("set_global {0}", index);
				default:
					throw new Exception (String.Format("Should not be reached: {0:X}", Opcode));
			}
		}

		public override string ToString () 
		{
			switch (Opcode) {
				case 0x20:
					return String.Format ("get_local {0}", index);
				case 0x21:
					return String.Format ("set_local {0}", index);
				case 0x22:
					return String.Format ("tee_local {0}", index);
				case 0x23:
					return String.Format ("get_global {0}", index);
				case 0x24:
					return String.Format ("set_global {0}", index);
				default:
					throw new Exception (String.Format("Should not be reached: {0:X}", Opcode));
			}
		}

		public static byte UpperBound ()
		{
			return 0x24;
		}

		public WebassemblyVariableInstruction (byte opcode, BinaryReader reader): base (opcode)
		{
			if (this.Opcode >= 0x20 && this.Opcode <= 0x24) {
				this.index = Parser.ParseLEBUnsigned (reader, 32);
			} else if (this.Opcode > 0x24) {
				throw new Exception ("Variable opcode out of range");
			}
		}
}

	public class WebassemblyMemoryInstruction : WebassemblyInstruction
	{
		ulong align;
		ulong offset;

		public override void Emit (IEnumerator<WebassemblyInstruction> cursor, ILGenerator ilgen, WebassemblyCodeParser top_level)
		{
			// Right now, we have an address on the top of the stack

			// Our opcode has an offset to apply to this address
			// FIXME: we ignore alignment
			ilgen.Emit (OpCodes.Ldc_I8, offset);
			ilgen.Emit (OpCodes.Add);

			// Get the object reference as the last argument to the call...
			ilgen.Emit (OpCodes.Ldarg_0);
			switch (Opcode) {
				case 0x28:
					ilgen.Emit(OpCodes.Call, typeof (WebassemblyModule).GetMethod ("Load32BitAsSigned32"));
					return;
				case 0x29:
					ilgen.Emit(OpCodes.Call, typeof (WebassemblyModule).GetMethod ("Load64BitAsSigned64"));
					return;
				case 0x2a:
					ilgen.Emit(OpCodes.Call, typeof (WebassemblyModule).GetMethod ("LoadSingle"));
					return;
				case 0x2b:
					ilgen.Emit(OpCodes.Call, typeof (WebassemblyModule).GetMethod ("LoadDouble"));
					return;
				case 0x2c:
					ilgen.Emit(OpCodes.Call, typeof (WebassemblyModule).GetMethod ("LoadSigned8BitAsSigned32"));
					return;
				case 0x2d:
					ilgen.Emit(OpCodes.Call, typeof (WebassemblyModule).GetMethod ("LoadUnsigned8BitAsSigned32"));
					return;
				case 0x2e:
					ilgen.Emit(OpCodes.Call, typeof (WebassemblyModule).GetMethod ("LoadSigned16BitAsSigned32"));
					return;
				case 0x2f:
					ilgen.Emit(OpCodes.Call, typeof (WebassemblyModule).GetMethod ("LoadUnsigned16BitAsSigned32"));
					return;
				case 0x30:
					ilgen.Emit(OpCodes.Call, typeof (WebassemblyModule).GetMethod ("LoadSigned8BitAsSigned64"));
					return;
				case 0x31:
					ilgen.Emit(OpCodes.Call, typeof (WebassemblyModule).GetMethod ("LoadUnsigned8BitAsSigned64"));
					return;
				case 0x32:
					ilgen.Emit(OpCodes.Call, typeof (WebassemblyModule).GetMethod ("LoadSigned16BitAsSigned64"));
					return;
				case 0x33:
					ilgen.Emit(OpCodes.Call, typeof (WebassemblyModule).GetMethod ("LoadUnsigned16BitAsSigned64"));
					return;
				case 0x34:
					ilgen.Emit(OpCodes.Call, typeof (WebassemblyModule).GetMethod ("LoadSigned32BitAsSigned64"));
					return;
				case 0x35:
					ilgen.Emit(OpCodes.Call, typeof (WebassemblyModule).GetMethod ("LoadUnsigned32BitAsSigned64"));
					return;

				case 0x36:
					ilgen.Emit(OpCodes.Call, typeof (WebassemblyModule).GetMethod ("Store32BitFrom32"));
					return;
				case 0x37:
					ilgen.Emit(OpCodes.Call, typeof (WebassemblyModule).GetMethod ("Store64BitFrom32"));
					return;
				case 0x38:
					ilgen.Emit(OpCodes.Call, typeof (WebassemblyModule).GetMethod ("StoreSingle"));
					return;
				case 0x39:
					ilgen.Emit(OpCodes.Call, typeof (WebassemblyModule).GetMethod ("StoreDouble"));
					return;

				case 0x3a:
					ilgen.Emit(OpCodes.Call, typeof (WebassemblyModule).GetMethod ("Store8BitFrom32"));
					return;
				case 0x3b:
					ilgen.Emit(OpCodes.Call, typeof (WebassemblyModule).GetMethod ("Store16BitFrom32"));
					return;
				case 0x3c:
					ilgen.Emit(OpCodes.Call, typeof (WebassemblyModule).GetMethod ("Store8BitFrom64"));
					return;
				case 0x3d:
					ilgen.Emit(OpCodes.Call, typeof (WebassemblyModule).GetMethod ("Store16BitFrom64"));
					return;
				case 0x3e:
					ilgen.Emit(OpCodes.Call, typeof (WebassemblyModule).GetMethod ("Store32BitFrom64"));
					return;

				case 0x3f:
					ilgen.Emit(OpCodes.Call, typeof (WebassemblyModule).GetMethod ("CurrentMemory"));
					return;
				case 0x40:
					ilgen.Emit(OpCodes.Call, typeof (WebassemblyModule).GetMethod ("GrowMemory"));
					return;
				default:
					throw new Exception (String.Format("Should not be reached: {0:X}", Opcode));
			}

			return;
		}

		public override string ToString () 
		{
			switch (Opcode) {
				case 0x28:
					return String.Format ("i32.load {0} {1}", this.align, this.offset);
				case 0x29:
					return String.Format ("i64.load {0} {1}", this.align, this.offset);
				case 0x2a:
					return String.Format ("f32.load {0} {1}", this.align, this.offset);
				case 0x2b:
					return String.Format ("f64.load {0} {1}", this.align, this.offset);
				case 0x2c:
					return String.Format ("i32.load8_s {0} {1}", this.align, this.offset);
				case 0x2d:
					return String.Format ("i32.load8_u {0} {1}", this.align, this.offset);
				case 0x2e:
					return String.Format ("i32.load16_s {0} {1}", this.align, this.offset);
				case 0x2f:
					return String.Format ("i32.load16_u {0} {1}", this.align, this.offset);
				case 0x30:
					return String.Format ("i64.load8_s {0} {1}", this.align, this.offset);
				case 0x31:
					return String.Format ("i64.load8_u {0} {1}", this.align, this.offset);
				case 0x32:
					return String.Format ("i64.load16_s {0} {1}", this.align, this.offset);
				case 0x33:
					return String.Format ("i64.load16_u {0} {1}", this.align, this.offset);
				case 0x34:
					return String.Format ("i64.load32_s {0} {1}", this.align, this.offset);
				case 0x35:
					return String.Format ("i64.load32_u {0} {1}", this.align, this.offset);
				case 0x36:
					return String.Format ("i32.store {0} {1}", this.align, this.offset);
				case 0x37:
					return String.Format ("i64.store {0} {1}", this.align, this.offset);
				case 0x38:
					return String.Format ("f32.store {0} {1}", this.align, this.offset);
				case 0x39:
					return String.Format ("f64.store {0} {1}", this.align, this.offset);
				case 0x3a:
					return String.Format ("i32.store8 {0} {1}", this.align, this.offset);
				case 0x3b:
					return String.Format ("i32.store16 {0} {1}", this.align, this.offset);
				case 0x3c:
					return String.Format ("i64.store8 {0} {1}", this.align, this.offset);
				case 0x3d:
					return String.Format ("i64.store16 {0} {1}", this.align, this.offset);
				case 0x3e:
					return String.Format ("i64.store32 {0} {1}", this.align, this.offset);
				case 0x3f:
					return "current_memory";
				case 0x40:
					return "grow_memory";
				default:
					throw new Exception (String.Format("Should not be reached: {0:X}", Opcode));
			}
		}

		public static byte UpperBound ()
		{
			return 0x40;
		}

		public WebassemblyMemoryInstruction (byte opcode, BinaryReader reader): base (opcode)
		{
			if (this.Opcode >= 0x28 && this.Opcode <= 0x3E) {
				this.align = Parser.ParseLEBUnsigned (reader, 32);
				this.offset = Parser.ParseLEBUnsigned (reader, 32);
			} else if (this.Opcode == 0x3F || this.Opcode == 0x40) {
				var endcap = reader.ReadByte ();
				if (endcap != 0x0)
					throw new Exception ("Memory size instruction lackend null endcap");
			} else if (this.Opcode > 0x40) {
				throw new Exception ("Memory opcode out of range");
			}
		}
	}

	public class WebassemblyNumericInstruction : WebassemblyInstruction
	{
		public override void Emit (IEnumerator<WebassemblyInstruction> cursor, ILGenerator ilgen, WebassemblyCodeParser top_level)
		{
			switch (Opcode) {
				case 0x41:
					switch (operand_i32) {
						case 0:
							ilgen.Emit (OpCodes.Ldc_I4_0);
							break;
						case 1:
							ilgen.Emit (OpCodes.Ldc_I4_1);
							break;
						case 2:
							ilgen.Emit (OpCodes.Ldc_I4_2);
							break;
						case 3:
							ilgen.Emit (OpCodes.Ldc_I4_3);
							break;
						case 4:
							ilgen.Emit (OpCodes.Ldc_I4_4);
							break;
						case 5:
							ilgen.Emit (OpCodes.Ldc_I4_5);
							break;
						case 6:
							ilgen.Emit (OpCodes.Ldc_I4_6);
							break;
						case 7:
							ilgen.Emit (OpCodes.Ldc_I4_7);
							break;
						case 8:
							ilgen.Emit (OpCodes.Ldc_I4_8);
							break;
						default:
							ilgen.Emit (OpCodes.Ldc_I4, operand_i32);
							break;
					}
					return;
				case 0x42:
					ilgen.Emit (OpCodes.Ldc_I8, operand_i64);
					return;
				case 0x43:
					ilgen.Emit (OpCodes.Ldc_R4, operand_f32);
					return;
				case 0x44:
					ilgen.Emit (OpCodes.Ldc_R8, operand_f64);
					return;

				case 0x45:
					ilgen.Emit (OpCodes.Ldc_I4_0);
					ilgen.Emit (OpCodes.Ceq);
					return;
				case 0x46:
					ilgen.Emit (OpCodes.Ceq);
					return;
				case 0x47:
					ilgen.Emit (OpCodes.Ceq);
					ilgen.Emit (OpCodes.Neg);
					return;

				case 0x48:
					ilgen.Emit (OpCodes.Clt);
					return;

				case 0x49:
					ilgen.Emit (OpCodes.Clt_Un);
					return;

				case 0x4a:
					ilgen.Emit (OpCodes.Cgt);
					return;

				case 0x4b:
					ilgen.Emit (OpCodes.Cgt_Un);
					return;

				//case 0x4c: 
					//// Less than or equal, signed
					//// first - last > 0
					//// sub.ovf.un
					//return "i32.le_s";

				//case 0x4d:
					//// Less than or equal, unsigned
					//// first - last > 0
					//// sub.ovf.un
					//return "i32.le_u";

				//case 0x4e:
					//return "i32.ge_s";

				//case 0x4f:
					//return "i32.ge_u";

				//case 0x50:
					//return "i64.eqz";
				//case 0x51:
					//return "i64.eq";
				//case 0x52:
					//return "i64.ne";
				//case 0x53:
					//return "i64.lt_s";
				//case 0x54:
					//return "i64.lt_u";
				case 0x55: // i64.gt_s
					ilgen.Emit (OpCodes.Cgt);
					return;

				//case 0x56:
					//return "i64.gt_u";
				//case 0x57:
					//return "i64.le_s";
				//case 0x58:
					//return "i64.le_u";
				//case 0x59:
					//return "i64.ge_s";
				//case 0x5a:
					//return "i64.ge_u";

				//case 0x5b:
					//return "f32.eq";
				//case 0x5c:
					//return "f32.ne";
				//case 0x5d:
					//return "f32.lt";
				case 0x5e:
					ilgen.Emit (OpCodes.Cgt);
					return;
				//case 0x5f:
					//return "f32.le";
				//case 0x60:
					//return "f32.ge";
				//case 0x61:
					//return "f64.eq";
				//case 0x62:
					//return "f64.ne";
				//case 0x63:
					//return "f64.lt";
				//case 0x64:
					//return "f64.gt";
				//case 0x65:
					//return "f64.le";
				//case 0x66:
					//return "f64.ge";
				//case 0x67:
					//return "i32.clz";
				case 0x68:
					// We will use Reiser's CTZ implementation here.
					//int lookup_table = new int [] { -1, 0, 1, 26, 2, 23, 27, 0, 3, 16, 24, 30, 28, 11, 0, 13, 4, 7, 17, 0, 25, 22, 31, 15, 29, 10, 12, 6, 0, 21, 14, 9, 5, 20, 8, 19, 18 };
					//return (-n & n) % 37
					//return "i32.ctz";
					
					// It's int->int so NOP is an okay mock
					ilgen.Emit (OpCodes.Nop);
					return;

				//case 0x69:
					//return "i32.popcnt";
				case 0x6a:
					ilgen.Emit (OpCodes.Add);
					return;
				case 0x6b:
					ilgen.Emit (OpCodes.Sub);
					return;
				case 0x6c:
					ilgen.Emit (OpCodes.Mul);
					return;
				//case 0x6d:
					//return "i32.div_s";
				//case 0x6e:
					//return "i32.div_u";
				//case 0x6f:
					//return "i32.rem_s";
				//case 0x70:
					//return "i32.rem_u";

				//case 0x71:
					//return "i32.and";
				//case 0x72:
					//return "i32.or";
				//case 0x73:
					//return "i32.xor";
				//case 0x74:
					//return "i32.shl";
				//case 0x75:
					//return "i32.shr_s";
				//case 0x76:
					//return "i32.shr_u";
				//case 0x77:
					//return "i32.rotl";
				//case 0x78:
					//return "i32.rotr";
				//case 0x79:
					//return "i64.clz";
				//case 0x7a:
					//return "i64.ctz";
				//case 0x7b:
					//return "i64.popcnt";
				case 0x7c:
					ilgen.Emit (OpCodes.Add);
					return;
				case 0x7d:
					ilgen.Emit (OpCodes.Sub);
					return;
				case 0x7e:
					ilgen.Emit (OpCodes.Mul);
					return;
				//case 0x7f:
					//return "i64.div_s";
				//case 0x80:
					//return "i64.div_u";
				//case 0x81:
					//return "i64.rem_s";
				//case 0x82:
					//return "i64.rem_u";
				//case 0x83:
					//return "i64.and";
				//case 0x84:
					//return "i64.or";
				//case 0x85:
					//return "i64.xor";
				//case 0x86:
					//return "i64.shl";
				//case 0x87:
					//return "i64.shr_s";
				//case 0x88:
					//return "i64.shr_u";
				//case 0x89:
					//return "i64.rotl";
				//case 0x8a:
					//return "i64.rotr";
				//case 0x8b:
					//return "f32.abs";
				//case 0x8c:
					//return "f32.neg";
				//case 0x8d:
					//return "f32.ceil";
				//case 0x8e:
					//return "f32.floor";
				//case 0x8f:
					//return "f32.trunc";
				//case 0x90:
					//return "f32.nearest";
				//case 0x91:
					//return "f32.sqrt";
				//case 0x92:
					//return "f32.add";
				//case 0x93:
					//return "f32.sub";
				//case 0x94:
					//return "f32.mul";
				//case 0x95:
					//return "f32.div";
				//case 0x96:
					//return "f32.min";
				//case 0x97:
					//return "f32.max";
				//case 0x98:
					//return "f32.copysign";
				//case 0x99:
					//return "f64.abs";
				//case 0x9a:
					//return "f64.neg";
				//case 0x9b:
					//return "f64.ceil";
				//case 0x9c:
					//return "f64.floor";
				//case 0x9d:
					//return "f64.trunc";
				//case 0x9e:
					//return "f64.nearest";
				//case 0x9f:
					//return "f64.sqrt";
				//case 0xa0:
					//return "f64.add";
				//case 0xa1:
					//return "f64.sub";
				//case 0xa2:
					//return "f64.mul";
				//case 0xa3:
					//return "f64.div";
				//case 0xa4:
					//return "f64.min";
				//case 0xa5:
					//return "f64.max";
				//case 0xa6:
					//return "f64.copysign";
				//case 0xa7:
					//return "i32.wrap/i64";
				//case 0xa8:
					//return "i32.trunc_s/f32";
				//case 0xa9:
					//return "i32.trunc_u/f32";
				//case 0xaa:
					//return "i32.trunc_s/f64";
				//case 0xab:
					//return "i32.trunc_u/f64";

				case 0xac:
					ilgen.Emit (OpCodes.Conv_Ovf_I8);
					return;

				//case 0xad:
					//return "i64.extend_u/i32";
				//case 0xae:
					//return "i64.trunc_s/f32";
				//case 0xaf:
					//return "i64.trunc_u/f32";
				//case 0xb0:
					//return "i64.trunc_s/f64";
				//case 0xb1:
					//return "i64.trunc_u/f64";
				//case 0xb2:
					//return "f32.convert_s/i32";
				//case 0xb3:
					//return "f32.convert_u/i32";
				//case 0xb4:
					//return "f32.convert_s/i64";
				//case 0xb5:
					//return "f32.convert_u/i64";
				//case 0xb6:
					//return "f32.demote/f64";
				//case 0xb7:
					//return "f64.convert_s/i32";
				//case 0xb8:
					//return "f64.convert_u/i32";
				case 0xb9:
					ilgen.Emit (OpCodes.Conv_R8);
					return;

				//case 0xba:
					//return "f64.convert_u/i64";
				//case 0xbb:
					//return "f64.promote/f32";
				//case 0xbc:
					//return "i32.reinterpret/f32";
				//case 0xbd:
					//return "i64.reinterpret/f64";
				//case 0xbe:
					//return "f32.reinterpret/i32";
				//case 0xbf:
					//return "f64.reinterpret/i64";
				default:
					throw new Exception (String.Format("Should not be reached: {0:X}", Opcode));
			}
		}

		public override string ToString () 
		{
			switch (Opcode) {
				case 0x41:
					return String.Format ("i32.const {0}", operand_i32);
				case 0x42:
					return String.Format ("i64.const {0}", operand_i64);
				case 0x43:
					return String.Format ("f32.const {0}", operand_f32);
				case 0x44:
					return String.Format ("f64.const {0}", operand_f64);
				case 0x45:
					return "i32.eqz";
				case 0x46:
					return "i32.eq";
				case 0x47:
					return "i32.ne";
				case 0x48:
					return "i32.lt_s";
				case 0x49:
					return "i32.lt_u";
				case 0x4a:
					return "i32.gt_s";
				case 0x4b:
					return "i32.gt_u";
				case 0x4c:
					return "i32.le_s";
				case 0x4d:
					return "i32.le_u";
				case 0x4e:
					return "i32.ge_s";
				case 0x4f:
					return "i32.ge_u";
				case 0x50:
					return "i64.eqz";
				case 0x51:
					return "i64.eq";
				case 0x52:
					return "i64.ne";
				case 0x53:
					return "i64.lt_s";
				case 0x54:
					return "i64.lt_u";
				case 0x55:
					return "i64.gt_s";
				case 0x56:
					return "i64.gt_u";
				case 0x57:
					return "i64.le_s";
				case 0x58:
					return "i64.le_u";
				case 0x59:
					return "i64.ge_s";
				case 0x5a:
					return "i64.ge_u";
				case 0x5b:
					return "f32.eq";
				case 0x5c:
					return "f32.ne";
				case 0x5d:
					return "f32.lt";
				case 0x5e:
					return "f32.gt";
				case 0x5f:
					return "f32.le";
				case 0x60:
					return "f32.ge";
				case 0x61:
					return "f64.eq";
				case 0x62:
					return "f64.ne";
				case 0x63:
					return "f64.lt";
				case 0x64:
					return "f64.gt";
				case 0x65:
					return "f64.le";
				case 0x66:
					return "f64.ge";
				case 0x67:
					return "i32.clz";
				case 0x68:
					return "i32.ctz";
				case 0x69:
					return "i32.popcnt";
				case 0x6a:
					return "i32.add";
				case 0x6b:
					return "i32.sub";
				case 0x6c:
					return "i32.mul";
				case 0x6d:
					return "i32.div_s";
				case 0x6e:
					return "i32.div_u";
				case 0x6f:
					return "i32.rem_s";
				case 0x70:
					return "i32.rem_u";
				case 0x71:
					return "i32.and";
				case 0x72:
					return "i32.or";
				case 0x73:
					return "i32.xor";
				case 0x74:
					return "i32.shl";
				case 0x75:
					return "i32.shr_s";
				case 0x76:
					return "i32.shr_u";
				case 0x77:
					return "i32.rotl";
				case 0x78:
					return "i32.rotr";
				case 0x79:
					return "i64.clz";
				case 0x7a:
					return "i64.ctz";
				case 0x7b:
					return "i64.popcnt";
				case 0x7c:
					return "i64.add";
				case 0x7d:
					return "i64.sub";
				case 0x7e:
					return "i64.mul";
				case 0x7f:
					return "i64.div_s";
				case 0x80:
					return "i64.div_u";
				case 0x81:
					return "i64.rem_s";
				case 0x82:
					return "i64.rem_u";
				case 0x83:
					return "i64.and";
				case 0x84:
					return "i64.or";
				case 0x85:
					return "i64.xor";
				case 0x86:
					return "i64.shl";
				case 0x87:
					return "i64.shr_s";
				case 0x88:
					return "i64.shr_u";
				case 0x89:
					return "i64.rotl";
				case 0x8a:
					return "i64.rotr";
				case 0x8b:
					return "f32.abs";
				case 0x8c:
					return "f32.neg";
				case 0x8d:
					return "f32.ceil";
				case 0x8e:
					return "f32.floor";
				case 0x8f:
					return "f32.trunc";
				case 0x90:
					return "f32.nearest";
				case 0x91:
					return "f32.sqrt";
				case 0x92:
					return "f32.add";
				case 0x93:
					return "f32.sub";
				case 0x94:
					return "f32.mul";
				case 0x95:
					return "f32.div";
				case 0x96:
					return "f32.min";
				case 0x97:
					return "f32.max";
				case 0x98:
					return "f32.copysign";
				case 0x99:
					return "f64.abs";
				case 0x9a:
					return "f64.neg";
				case 0x9b:
					return "f64.ceil";
				case 0x9c:
					return "f64.floor";
				case 0x9d:
					return "f64.trunc";
				case 0x9e:
					return "f64.nearest";
				case 0x9f:
					return "f64.sqrt";
				case 0xa0:
					return "f64.add";
				case 0xa1:
					return "f64.sub";
				case 0xa2:
					return "f64.mul";
				case 0xa3:
					return "f64.div";
				case 0xa4:
					return "f64.min";
				case 0xa5:
					return "f64.max";
				case 0xa6:
					return "f64.copysign";
				case 0xa7:
					return "i32.wrap/i64";
				case 0xa8:
					return "i32.trunc_s/f32";
				case 0xa9:
					return "i32.trunc_u/f32";
				case 0xaa:
					return "i32.trunc_s/f64";
				case 0xab:
					return "i32.trunc_u/f64";
				case 0xac:
					return "i64.extend_s/i32";
				case 0xad:
					return "i64.extend_u/i32";
				case 0xae:
					return "i64.trunc_s/f32";
				case 0xaf:
					return "i64.trunc_u/f32";
				case 0xb0:
					return "i64.trunc_s/f64";
				case 0xb1:
					return "i64.trunc_u/f64";
				case 0xb2:
					return "f32.convert_s/i32";
				case 0xb3:
					return "f32.convert_u/i32";
				case 0xb4:
					return "f32.convert_s/i64";
				case 0xb5:
					return "f32.convert_u/i64";
				case 0xb6:
					return "f32.demote/f64";
				case 0xb7:
					return "f64.convert_s/i32";
				case 0xb8:
					return "f64.convert_u/i32";
				case 0xb9:
					return "f64.convert_s/i64";
				case 0xba:
					return "f64.convert_u/i64";
				case 0xbb:
					return "f64.promote/f32";
				case 0xbc:
					return "i32.reinterpret/f32";
				case 0xbd:
					return "i64.reinterpret/f64";
				case 0xbe:
					return "f32.reinterpret/i32";
				case 0xbf:
					return "f64.reinterpret/i64";
				default:
					throw new Exception (String.Format("Should not be reached: {0:X}", Opcode));
			}
		}

		public static byte UpperBound ()
		{
			return 0xBF;
		}

		int operand_i32;
		long operand_i64;
		float operand_f32;
		double operand_f64;

		public WebassemblyNumericInstruction (byte opcode, BinaryReader reader): base (opcode)
		{
			if (this.Opcode > 0xBF) {
				throw new Exception ("Numerical opcode out of range");
			} else if (this.Opcode == 0x41) {
				operand_i32 = Convert.ToInt32 (Parser.ParseLEBSigned (reader, 32));
			} else if (this.Opcode == 0x42) {
				operand_i64 = Parser.ParseLEBSigned (reader, 64);
			} else if (this.Opcode == 0x43) {
				operand_f32 = reader.ReadSingle ();
			} else if (this.Opcode == 0x44) {
				operand_f64 = reader.ReadDouble ();
			}
		}
	}
}



