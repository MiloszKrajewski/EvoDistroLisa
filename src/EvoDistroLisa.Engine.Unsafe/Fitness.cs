using System;

namespace EvoDistroLisa.Engine.Unsafe
{
	/// <summary>
	/// Helper class to calculate fitness. 
	/// For performance reason it has been implemented using 'unsafe' code and therefore
	/// needed to be implemented in C# (I miss inline functions already).
	/// NOTE: this approach would overflow for images containing more than 2^46 pixels.
	/// </summary>
	public static class Fitness
	{
		private static unsafe ulong SumDev(
			uint* source, int sourceOffset,
			uint* target, int targetOffset,
			int length)
		{
			var sum = 0uL;
			source += sourceOffset;
			target += targetOffset;
			var offset = 0;

			while (offset < length)
			{
				var o = (int)*(source + offset);
				var oR = (o >> 16) & 0xFF;
				var oG = (o >> 8) & 0xFF;
				var oB = o & 0xFF;

				var r = (int)*(target + offset);
				var rR = (r >> 16) & 0xFF;
				var rG = (r >> 8) & 0xFF;
				var rB = r & 0xFF;

				var dR = oR - rR;
				var dG = oG - rG;
				var dB = oB - rB;

				// NOTE: 
				// - ulong for a sum won't overflow as: ulong.MaxValue / (255^2*3) > int.MaxValue
				// - uint for partial result won't overflow as 255^2*3 < int.MaxValue
				sum += (uint)(dR * dR + dG * dG + dB * dB);

				offset++;
			}

			return sum;
		}

		public static unsafe ulong SumDev(
			uint[] source, int sourceOffset, 
			uint[] target, int targetOffset, 
			int length)
		{
			fixed (uint* sourcePtr = &source[0])
			fixed (uint* targetPtr = &target[0])
			{
				return SumDev(sourcePtr, sourceOffset, targetPtr, targetOffset, length);
			}
		}

		public static unsafe ulong SumDev(
			uint[] source, int sourceOffset,
			IntPtr target, int targetOffset,
			int length)
		{
			fixed (uint* sourcePtr = &source[0])
			{
				var targetPtr = (uint*)target.ToPointer();
				return SumDev(sourcePtr, sourceOffset, targetPtr, targetOffset, length);
			}
		}
	}
}
