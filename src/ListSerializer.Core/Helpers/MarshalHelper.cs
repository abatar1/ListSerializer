using System;
using System.Runtime.InteropServices;
using System.Text;

namespace ListSerializer.Core.Helpers
{
    public class MarshalHelper
    {
        public static void WriteIntToBuffer(byte[] buffer, int value, int offset)
        {
            buffer[0 + offset] = (byte) value;
            buffer[1 + offset] = (byte) (value >> 8);
            buffer[2 + offset] = (byte) (value >> 16);
            buffer[3 + offset] = (byte) (value >> 24);
        }

        public static void WriteUtf8StringToBuffer(byte[] buffer, string value, int size, int offset)
        {
            var dataPtr = Marshal.StringToCoTaskMemUTF8(value);
            Marshal.Copy(dataPtr, buffer, offset, size);
            Marshal.FreeHGlobal(dataPtr);
        }

        public static int ReadIntFromBuffer(byte[] buffer, int offset)
        {
            return buffer[0 + offset] | (buffer[1 + offset] << 8) | (buffer[2 + offset] << 16) |
                   (buffer[3 + offset] << 24);
        }

        public static string ReadUtf8StringFromBuffer(byte[] bytes, int startIndex, int offset)
        {
            var ptr = Marshal.AllocHGlobal(offset);
            Marshal.Copy(bytes, startIndex, ptr, offset);
            var result = Marshal.PtrToStringUTF8(ptr);
            Marshal.FreeHGlobal(ptr);
            return result;
        }
    }
}
