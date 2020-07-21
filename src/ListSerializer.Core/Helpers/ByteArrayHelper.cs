using System;
using System.Linq;

namespace ListSerializer.Core.Helpers
{
    public class ByteArrayHelper
    {
        public static byte[] Combine(byte[][] byteArrays)
        {
            var result = new byte[byteArrays.Sum(a => a.Length)];
            var offset = 0;
            foreach (var array in byteArrays)
            {
                Buffer.BlockCopy(array, 0, result, offset, array.Length);
                offset += array.Length;
            }
            return result;
        }
    }
}
