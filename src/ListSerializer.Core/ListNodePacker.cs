using System;
using System.Diagnostics;
using System.Threading.Tasks;
using ListSerializer.Core.Helpers;

namespace ListSerializer.Core
{
    public struct PackedListNode
    {
        public int CurrentNodeHashcode;
        public int NextNodeHashcode;
        public int RandomNodeHashcode;
        public string Data;
        public int ByteSize;
    }

    public class ListNodePacker
    {
        /// <summary>
        /// Method marshals <see cref="ListNode"/> into the byte array.
        /// The structure is as follows: 
        /// <list type="bullet">
        /// <item>
        /// <description>Current Node Hashcode - 4 bytes.</description>
        /// </item>
        /// <item>
        /// <description>Next Node Hashcode - 4 bytes.</description>
        /// </item>
        /// <item>
        /// <description>Random Node Hashcode - 4 bytes.</description>
        /// </item>
        /// <item>
        /// <description>Data Size - 4 bytes.</description>
        /// </item>
        /// <item>
        /// <description>Data - {Data Size} bytes.</description>
        /// </item>
        /// </list>
        /// </summary>
        public byte[] ToBytes(ListNode node)
        {
            var dataSize = node.Data.Length * sizeof(char);
            var resultBytes = new byte[sizeof(int) * 4 + dataSize];
            MarshalHelper.WriteIntToBuffer(resultBytes, node.GetHashCode(), 0);
            MarshalHelper.WriteIntToBuffer(resultBytes, node.Next?.GetHashCode() ?? 0, sizeof(int));
            MarshalHelper.WriteIntToBuffer(resultBytes, node.Random?.GetHashCode() ?? 0, sizeof(int) * 2);
            MarshalHelper.WriteIntToBuffer(resultBytes, dataSize, sizeof(int) * 3);
            MarshalHelper.WriteUtf8StringToBuffer(resultBytes, node.Data, dataSize, sizeof(int) * 4);
            return resultBytes;
        }

        /// <summary>
        /// Method marshals <see cref="ListNode"/> into the byte array.
        /// The structure is as follows: 
        /// <list type="bullet">
        /// <item>
        /// <description>Current Node Hashcode - 4 bytes.</description>
        /// </item>
        /// <item>
        /// <description>Next Node Hashcode - 4 bytes.</description>
        /// </item>
        /// <item>
        /// <description>Random Node Hashcode - 4 bytes.</description>
        /// </item>
        /// <item>
        /// <description>Data Size - 4 bytes.</description>
        /// </item>
        /// <item>
        /// <description>Data - {Data Size} bytes.</description>
        /// </item>
        /// </list>
        /// </summary>
        public async Task<byte[]> ToBytesAsync(ListNode node)
        {
            return await Task.Run(() => ToBytes(node));
        }

        /// <summary>
        /// Method marshals byte array back into <see cref="ListNode"/>.
        /// Take a look at <see cref="ToBytesAsync"/> description to understand <see cref="PackedListNode"/> structure.
        /// </summary>
        public PackedListNode FromBuffer(byte[] buffer, int offset)
        {
            try
            {
                var currentHashCode = MarshalHelper.ReadIntFromBuffer(buffer, offset);
                var nextHashCode = MarshalHelper.ReadIntFromBuffer(buffer, sizeof(int) + offset);
                var randomHashCode = MarshalHelper.ReadIntFromBuffer(buffer, sizeof(int) * 2 + offset);
                var dataSize = MarshalHelper.ReadIntFromBuffer(buffer, sizeof(int) * 3 + offset);
                var data = MarshalHelper.ReadUtf8StringFromBuffer(buffer, sizeof(int) * 4 + offset, dataSize);
                return new PackedListNode
                {
                    CurrentNodeHashcode = currentHashCode,
                    NextNodeHashcode = nextHashCode,
                    RandomNodeHashcode = randomHashCode,
                    Data = data,
                    ByteSize = sizeof(int) * 4 + dataSize
                };
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                throw new ArgumentException("Invalid data has given", e);
            }
        }

        /// <summary>
        /// Method marshals byte array back into <see cref="ListNode"/>.
        /// Take a look at <see cref="ToBytesAsync"/> description to understand <see cref="PackedListNode"/> structure.
        /// </summary>
        public async Task<PackedListNode> FromBufferAsync(byte[] buffer, int offset)
        {
            return await Task.Run(() => FromBuffer(buffer, offset));
        }
    }
}
