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
		protected static long Load64BitAsSigned64 (int offset, WebassemblyModule self) {
			return BitConverter.ToInt64 (self.Copy (offset, 8), 0);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static long Load32BitAsSigned64 (int offset, WebassemblyModule self) {
			return BitConverter.ToInt64 (self.Copy (offset, 4), 0);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static long Load16BitAsSigned64 (int offset, WebassemblyModule self) {
			return BitConverter.ToInt64 (self.Copy (offset, 2), 0);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static long Load8BitAsSigned64 (int offset, WebassemblyModule self) {
			return BitConverter.ToInt64 (self.Copy (offset, 1), 0);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static ulong Load64BitAsUnsigned64 (int offset, WebassemblyModule self) {
			return BitConverter.ToUInt64 (self.Copy (offset, 8), 0);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static ulong Load32BitAsUnsigned64 (int offset, WebassemblyModule self) {
			return BitConverter.ToUInt64(self.Copy (offset, 4), 0);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static ulong Load16BitAsUnsigned64 (int offset, WebassemblyModule self) {
			return BitConverter.ToUInt64(self.Copy (offset, 2), 0);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static ulong Load8BitAsUnsigned64 (int offset, WebassemblyModule self) {
			return BitConverter.ToUInt64(self.Copy (offset, 1), 0);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static long Load32BitAsSigned32 (int offset, WebassemblyModule self) {
			return BitConverter.ToInt32(self.Copy (offset, 4), 0);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static long Load16BitAsSigned32 (int offset, WebassemblyModule self) {
			return BitConverter.ToInt32(self.Copy (offset, 2), 0);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static long Load8BitAsSigned32 (int offset, WebassemblyModule self) {
			return BitConverter.ToInt32(self.Copy (offset, 1), 0);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static ulong Load32BitAsUnsigned32 (int offset, WebassemblyModule self) {
			return BitConverter.ToUInt32(self.Copy (offset, 4), 0);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static ulong Load16BitAsUnsigned32 (int offset, WebassemblyModule self) {
			return BitConverter.ToUInt32(self.Copy (offset, 2), 0);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static ulong Load8BitAsUnsigned32 (int offset, WebassemblyModule self) {
			return BitConverter.ToUInt32(self.Copy (offset, 1), 0);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static void Store64BitFrom64 (long input, int offset, WebassemblyModule self) {
			var bytes = BitConverter.GetBytes (input);
			self.memory [offset + 0] = bytes [0];
			self.memory [offset + 1] = bytes [1];
			self.memory [offset + 2] = bytes [2];
			self.memory [offset + 3] = bytes [3];
			self.memory [offset + 4] = bytes [4];
			self.memory [offset + 5] = bytes [5];
			self.memory [offset + 6] = bytes [6];
			self.memory [offset + 7] = bytes [7];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static void Store32BitFrom64 (long input, int offset, WebassemblyModule self) {
			var bytes = BitConverter.GetBytes (input);
			self.memory [offset + 0] = bytes [0];
			self.memory [offset + 1] = bytes [1];
			self.memory [offset + 2] = bytes [2];
			self.memory [offset + 3] = bytes [3];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static void Store16BitFrom64 (long input, int offset, WebassemblyModule self) {
			var bytes = BitConverter.GetBytes (input);
			self.memory [offset + 0] = bytes [0];
			self.memory [offset + 1] = bytes [1];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static void Store8BitFrom64 (long input, int offset, WebassemblyModule self) {
			var bytes = BitConverter.GetBytes (input);
			self.memory [offset + 0] = bytes [0];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static void Store32BitFrom32 (int input, int offset, WebassemblyModule self) {
			var bytes = BitConverter.GetBytes (input);
			self.memory [offset + 0] = bytes [0];
			self.memory [offset + 1] = bytes [1];
			self.memory [offset + 2] = bytes [3];
			self.memory [offset + 3] = bytes [3];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static void Store16BitFrom32 (int input, int offset, WebassemblyModule self) {
			var bytes = BitConverter.GetBytes (input);
			self.memory [offset + 0] = bytes [0];
			self.memory [offset + 1] = bytes [1];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static void Store8BitFrom32 (int input, int offset, WebassemblyModule self) {
			var bytes = BitConverter.GetBytes (input);
			self.memory [offset + 0] = bytes [0];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static float LoadSingle (int offset, WebassemblyModule self) {
			return BitConverter.ToSingle (self.Copy (offset, 4), 0);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static void StoreSingle (float input, int offset, WebassemblyModule self) {
			var bytes = BitConverter.GetBytes (input);
			self.memory [offset + 0] = bytes [0];
			self.memory [offset + 1] = bytes [1];
			self.memory [offset + 2] = bytes [3];
			self.memory [offset + 3] = bytes [3];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static double LoadDouble (int offset, WebassemblyModule self) {
			return BitConverter.ToDouble (self.Copy (offset, 8), 0);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static void StoreDouble (double input, int align, int offset, WebassemblyModule self) {
			var bytes = BitConverter.GetBytes (input);
			self.memory [offset + 0] = bytes [0];
			self.memory [offset + 1] = bytes [1];
			self.memory [offset + 2] = bytes [2];
			self.memory [offset + 3] = bytes [3];
			self.memory [offset + 4] = bytes [4];
			self.memory [offset + 5] = bytes [5];
			self.memory [offset + 6] = bytes [6];
			self.memory [offset + 7] = bytes [7];
		}

		protected static uint CurrentMemory (WebassemblyModule self)
		{
			return Convert.ToUInt32 (self.memory.Length / PageSize);
		}

		protected static uint GrowMemory (uint addition, WebassemblyModule self)
		{
			var temp = self.memory;
			var curr_length = temp.Length / PageSize;
			var new_length = (curr_length + 1) * PageSize;

			// Fixme: check maximum length
			var new_memory = new byte [new_length];
	    Array.Copy(temp, self.memory, curr_length);
	    self.memory = new_memory;

			return Convert.ToUInt32 (curr_length);
		}

	}
}
