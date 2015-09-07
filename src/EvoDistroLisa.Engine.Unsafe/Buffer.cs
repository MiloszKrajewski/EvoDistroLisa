using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvoDistroLisa.Engine.Unsafe
{
	public static class Buffer
	{
		public static unsafe void Copy(
			uint* source, int sourceOffset,
			uint* target, int targetOffset,
			int length)
		{
			var offset = 0;
			source += sourceOffset;
			target += targetOffset;
			while (offset < length)
			{
				*(target + offset) = *(source + offset);
				offset++;
			}
		}

		public static unsafe void Copy(
			IntPtr source, int sourceOffset,
			uint[] target, int targetOffset, 
			int length)
		{
			var sourcePtr = (uint*)source.ToPointer();
			fixed (uint* targetPtr = &target[0])
			{
				Copy(sourcePtr, sourceOffset, targetPtr, targetOffset, length);
			}
		}
	}
}
