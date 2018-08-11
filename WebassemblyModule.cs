// Emitted classes derive from this class
using System.Runtime.CompilerServices;
using System;

namespace Wasm2CIL {
	public class WebassemblyModule
	{
		// The length of this memory is given in terms of the page size,
		// which is 2^^16 or 65536
		public const int PageSize = 65536;
		protected byte [] memory;

		public WebassemblyModule (int memory_default_size)
		{
			this.memory = new byte [PageSize * memory_default_size];
		}

		//private byte [] table;

		// All of this will eventually have to be replaced with
		// codegen that uses offsets from the memory array, to avoid
		// copying. That, or pointers and unsafe.
		//
		// Unsafe would also allow us to do stackalloc

		// We need to be able to call these memory functions when we
		// have a stack that already has the offset on it. The easiest
		// way to fix this and to fix the issue of wanting to *ensure*
		// we only read a certain number of bytes is to do this with
		// indirection with this protected function
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected byte [] Copy (int offset, int count)
		{
			byte [] result = new byte [count];
	    Array.Copy(memory, offset, result, 0, count);
	    return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected long Load64BitAsSigned64 (int offset) {
			return BitConverter.ToInt64 (this.Copy (offset, 8), 0);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected long Load32BitAsSigned64 (int offset) {
			return BitConverter.ToInt64 (this.Copy (offset, 4), 0);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected long LoadSigned16BitAsSigned64 (int offset) {
			return BitConverter.ToInt64 (this.Copy (offset, 2), 0);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected long LoadUnsigned16BitAsSigned64 (int offset) {
			return BitConverter.ToInt64 (this.Copy (offset, 2), 0);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected long LoadSigned8BitAsSigned64 (int offset) {
			return BitConverter.ToInt64 (this.Copy (offset, 1), 0);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected long LoadUnsigned8BitAsSigned64 (int offset) {
			return BitConverter.ToInt64 (this.Copy (offset, 1), 0);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected ulong Load64BitAsUnsigned64 (int offset) {
			return BitConverter.ToUInt64 (this.Copy (offset, 8), 0);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected ulong LoadUnsigned32BitAsUnsigned64 (int offset) {
			return BitConverter.ToUInt64(this.Copy (offset, 4), 0);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected ulong LoadSigned32BitAsUnsigned64 (int offset) {
			return BitConverter.ToUInt64(this.Copy (offset, 4), 0);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected ulong LoadUnsigned16BitAsUnsigned64 (int offset) {
			return BitConverter.ToUInt64(this.Copy (offset, 2), 0);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected ulong LoadSigned16BitAsUnsigned64 (int offset) {
			return BitConverter.ToUInt64(this.Copy (offset, 2), 0);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected ulong LoadSigned8BitAsUnsigned64 (int offset) {
			return BitConverter.ToUInt64(this.Copy (offset, 1), 0);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected ulong LoadUnsigned8BitAsUnsigned64 (int offset) {
			return BitConverter.ToUInt64(this.Copy (offset, 1), 0);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected long Load32BitAsSigned32 (int offset) {
			return BitConverter.ToInt32(this.Copy (offset, 4), 0);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected long LoadSigned16BitAsSigned32 (int offset) {
			return BitConverter.ToInt32(this.Copy (offset, 2), 0);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected long LoadUnsigned16BitAsSigned32 (int offset) {
			return BitConverter.ToInt32(this.Copy (offset, 2), 0);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected long LoadUnsigned8BitAsSigned32 (int offset) {
			return BitConverter.ToInt32(this.Copy (offset, 1), 0);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected long LoadSigned8BitAsSigned32 (int offset) {
			return BitConverter.ToInt32(this.Copy (offset, 1), 0);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected ulong Load32BitAsUnsigned32 (int offset) {
			return BitConverter.ToUInt32(this.Copy (offset, 4), 0);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected ulong LoadSigned16BitAsUnsigned32 (int offset) {
			return BitConverter.ToUInt32(this.Copy (offset, 2), 0);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected ulong LoadUnsigned16BitAsUnsigned32 (int offset) {
			return BitConverter.ToUInt32(this.Copy (offset, 2), 0);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected ulong LoadSigned8BitAsUnsigned32 (int offset) {
			return BitConverter.ToUInt32(this.Copy (offset, 1), 0);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected ulong LoadUnsigned8BitAsUnsigned32 (int offset) {
			return BitConverter.ToUInt32(this.Copy (offset, 1), 0);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected void Store64BitFrom64 (long input, int offset) {
			var bytes = BitConverter.GetBytes (input);
			this.memory [offset + 0] = bytes [0];
			this.memory [offset + 1] = bytes [1];
			this.memory [offset + 2] = bytes [2];
			this.memory [offset + 3] = bytes [3];
			this.memory [offset + 4] = bytes [4];
			this.memory [offset + 5] = bytes [5];
			this.memory [offset + 6] = bytes [6];
			this.memory [offset + 7] = bytes [7];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected void Store32BitFrom32 (long input, int offset) {
			var bytes = BitConverter.GetBytes (input);
			this.memory [offset + 0] = bytes [0];
			this.memory [offset + 1] = bytes [1];
			this.memory [offset + 2] = bytes [2];
			this.memory [offset + 3] = bytes [3];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected void Store16BitFrom64 (long input, int offset) {
			var bytes = BitConverter.GetBytes (input);
			this.memory [offset + 0] = bytes [0];
			this.memory [offset + 1] = bytes [1];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected void Store8BitFrom64 (long input, int offset) {
			var bytes = BitConverter.GetBytes (input);
			this.memory [offset + 0] = bytes [0];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected void Store16BitFrom32 (int input, int offset) {
			var bytes = BitConverter.GetBytes (input);
			this.memory [offset + 0] = bytes [0];
			this.memory [offset + 1] = bytes [1];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected void Store8BitFrom32 (int input, int offset) {
			var bytes = BitConverter.GetBytes (input);
			this.memory [offset + 0] = bytes [0];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected float LoadSingle (int offset) {
			return BitConverter.ToSingle (this.Copy (offset, 4), 0);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected void StoreSingle (float input, int offset) {
			var bytes = BitConverter.GetBytes (input);
			this.memory [offset + 0] = bytes [0];
			this.memory [offset + 1] = bytes [1];
			this.memory [offset + 2] = bytes [3];
			this.memory [offset + 3] = bytes [3];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected double LoadDouble (int offset) {
			return BitConverter.ToDouble (this.Copy (offset, 8), 0);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected void StoreDouble (double input, int align, int offset) {
			var bytes = BitConverter.GetBytes (input);
			this.memory [offset + 0] = bytes [0];
			this.memory [offset + 1] = bytes [1];
			this.memory [offset + 2] = bytes [2];
			this.memory [offset + 3] = bytes [3];
			this.memory [offset + 4] = bytes [4];
			this.memory [offset + 5] = bytes [5];
			this.memory [offset + 6] = bytes [6];
			this.memory [offset + 7] = bytes [7];
		}

		protected uint CurrentMemory ()
		{
			return Convert.ToUInt32 (this.memory.Length / PageSize);
		}

		protected uint GrowMemory (uint addition)
		{
			var temp = this.memory;
			var curr_length = temp.Length / PageSize;
			var new_length = (curr_length + 1) * PageSize;

			// Fixme: check maximum length
			var new_memory = new byte [new_length];
	    Array.Copy(temp, this.memory, curr_length);
	    this.memory = new_memory;

			return Convert.ToUInt32 (curr_length);
		}

	}
}
