using System;
using System.Runtime.InteropServices;
using System.Text;

namespace ListSerializer.Core.Helpers
{
    public class MarshalHelper
    {
        public static void WriteIntToBuffer(byte[] buffer, int value, int offset)
        {
            for (var i = 0; i < sizeof(int); i++)
            {
                buffer[i + offset] = (byte)(value >> i * 8);
            }
        }

        public static void WriteLongToBuffer(byte[] buffer, long value, int offset)
        {
            for (var i = 0; i < sizeof(long); i++)
            {
                buffer[i + offset] = (byte) (value >> i * 8);
            }
        }

        public static void WriteUtf8StringToBuffer(byte[] buffer, string value, int size, int offset)
        {
            if (value.Length == 0) return;
            var dataPtr = Marshal.StringToCoTaskMemUTF8(value);
            Marshal.Copy(dataPtr, buffer, offset, size);
            Marshal.FreeHGlobal(dataPtr);
        }

        public static long ReadLongFromBuffer(byte[] buffer, int offset)
        {
            return BitConverter.ToInt64(buffer, offset);
        }

        public static int ReadIntFromBuffer(byte[] buffer, int offset)
        {
            return BitConverter.ToInt32(buffer, offset);
        }

        public static string ReadUtf8StringFromBuffer(byte[] bytes, int startIndex, int offset)
        {
            if (offset == 0) return string.Empty;
            var ptr = Marshal.AllocHGlobal(offset);
            Marshal.Copy(bytes, startIndex, ptr, offset);
            var result = Marshal.PtrToStringUTF8(ptr);
            Marshal.FreeHGlobal(ptr);
            return result;
        }
    }
}
