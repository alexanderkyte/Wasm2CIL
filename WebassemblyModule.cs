// Emitted classes derive from this class
using System.Runtime.CompilerServices;
using System;

namespace Wasm2CIL {
	public class WebassemblyModule
	{
		// The length of this memory is given in terms of the page size,
		// which is 2^^16 or 65536
		public const int PageSize = 65536;
		private byte [] memory;

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

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected int Align (int align, int offset)
		{
			// Fixme?
			return offset;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected byte [] Copy (int offset, int count)
		{
			byte [] result = new byte [count];
	    Array.Copy(memory, offset, result, 0, count);
	    return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected long Load64BitAsSigned64 (int align, int offset) {
			return BitConverter.ToInt64(Copy (offset, 8), Align (align, offset));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected long Load32BitAsSigned64 (int align, int offset) {
			return BitConverter.ToInt64(Copy (offset, 4), Align (align, offset));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected long Load16BitAsSigned64 (int align, int offset) {
			return BitConverter.ToInt64(Copy (offset, 2), Align (align, offset));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected long Load8BitAsSigned64 (int align, int offset) {
			return BitConverter.ToInt64(Copy (offset, 1), Align (align, offset));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected ulong Load64BitAsUnsigned64 (int align, int offset) {
			return BitConverter.ToUInt64(Copy (offset, 8), Align (align, offset));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected ulong Load32BitAsUnsigned64 (int align, int offset) {
			return BitConverter.ToUInt64(Copy (offset, 4), Align (align, offset));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected ulong Load16BitAsUnsigned64 (int align, int offset) {
			return BitConverter.ToUInt64(Copy (offset, 2), Align (align, offset));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected ulong Load8BitAsUnsigned64 (int align, int offset) {
			return BitConverter.ToUInt64(Copy (offset, 1), Align (align, offset));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected long Load32BitAsSigned32 (int align, int offset) {
			return BitConverter.ToInt32(Copy (offset, 4), Align (align, offset));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected long Load16BitAsSigned32 (int align, int offset) {
			return BitConverter.ToInt32(Copy (offset, 2), Align (align, offset));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected long Load8BitAsSigned32 (int align, int offset) {
			return BitConverter.ToInt32(Copy (offset, 1), Align (align, offset));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected ulong Load32BitAsUnsigned32 (int align, int offset) {
			return BitConverter.ToUInt32(Copy (offset, 4), Align (align, offset));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected ulong Load16BitAsUnsigned32 (int align, int offset) {
			return BitConverter.ToUInt32(Copy (offset, 2), Align (align, offset));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected ulong Load8BitAsUnsigned32 (int align, int offset) {
			return BitConverter.ToUInt32(Copy (offset, 1), Align (align, offset));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected void Store64BitFrom64 (long input, int align, int offset) {
			var bytes = BitConverter.GetBytes (input);
			memory [offset + 0] = bytes [0];
			memory [offset + 1] = bytes [1];
			memory [offset + 2] = bytes [2];
			memory [offset + 3] = bytes [3];
			memory [offset + 4] = bytes [4];
			memory [offset + 5] = bytes [5];
			memory [offset + 6] = bytes [6];
			memory [offset + 7] = bytes [7];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected void Store32BitFrom64 (long input, int align, int offset) {
			var bytes = BitConverter.GetBytes (input);
			memory [offset + 0] = bytes [0];
			memory [offset + 1] = bytes [1];
			memory [offset + 2] = bytes [2];
			memory [offset + 3] = bytes [3];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected void Store16BitFrom64 (long input, int align, int offset) {
			var bytes = BitConverter.GetBytes (input);
			memory [offset + 0] = bytes [0];
			memory [offset + 1] = bytes [1];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected void Store8BitFrom64 (long input, int align, int offset) {
			var bytes = BitConverter.GetBytes (input);
			memory [offset + 0] = bytes [0];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected void Store32BitFrom32 (int input, int align, int offset) {
			var bytes = BitConverter.GetBytes (input);
			memory [offset + 0] = bytes [0];
			memory [offset + 1] = bytes [1];
			memory [offset + 2] = bytes [3];
			memory [offset + 3] = bytes [3];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected void Store16BitFrom32 (int input, int align, int offset) {
			var bytes = BitConverter.GetBytes (input);
			memory [offset + 0] = bytes [0];
			memory [offset + 1] = bytes [1];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected void Store8BitFrom32 (int input, int align, int offset) {
			var bytes = BitConverter.GetBytes (input);
			memory [offset + 0] = bytes [0];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected float LoadSingle (int align, int offset) {
			return BitConverter.ToSingle (Copy (offset, 4), Align (align, offset));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected void StoreSingle (float input, int align, int offset) {
			var bytes = BitConverter.GetBytes (input);
			memory [offset + 0] = bytes [0];
			memory [offset + 1] = bytes [1];
			memory [offset + 2] = bytes [3];
			memory [offset + 3] = bytes [3];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected double LoadDouble (int align, int offset) {
			return BitConverter.ToDouble (Copy (offset, 8), Align (align, offset));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected void StoreDouble (double input, int align, int offset) {
			var bytes = BitConverter.GetBytes (input);
			memory [offset + 0] = bytes [0];
			memory [offset + 1] = bytes [1];
			memory [offset + 2] = bytes [2];
			memory [offset + 3] = bytes [3];
			memory [offset + 4] = bytes [4];
			memory [offset + 5] = bytes [5];
			memory [offset + 6] = bytes [6];
			memory [offset + 7] = bytes [7];
		}

		public uint CurrentMemory ()
		{
			return Convert.ToUInt32 (memory.Length / PageSize);
		}

		protected uint GrowMemory (uint addition)
		{
			var temp = this.memory;
			var curr_length = this.memory.Length / PageSize;
			var new_length = (curr_length + 1) * PageSize;

			// Fixme: check maximum length
			this.memory = new byte [new_length];
	    Array.Copy(temp, this.memory, curr_length);

			return Convert.ToUInt32 (curr_length);
		}

	}
}
