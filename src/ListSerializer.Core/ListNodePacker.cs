using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using ListSerializer.Core.Helpers;

namespace ListSerializer.Core
{
    public struct PackedListNode
    {
        public long CurrentNodeId;
        public long NextNodeId;
        public long RandomNodeId;
        public string Data;
        public int ByteSize;
    }

    public class ListNodePacker
    {
        private readonly UniqueIdGenerator _idGenerator = new UniqueIdGenerator();

        private long GetNodeId(ListNode node) => _idGenerator.GetId(node);

        /// <summary>
        /// Method marshals <see cref="ListNode"/> into the byte array.
        /// The structure is as follows: 
        /// <list type="bullet">
        /// <item>
        /// <description>Current Node Identifier - 8 bytes.</description>
        /// </item>
        /// <item>
        /// <description>Next Node Identifier - 8 bytes.</description>
        /// </item>
        /// <item>
        /// <description>Random Node Identifier - 8 bytes.</description>
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
            var resultBytes = new byte[sizeof(long) * 3 + sizeof(int) + dataSize];
            MarshalHelper.WriteLongToBuffer(resultBytes, GetNodeId(node), 0);
            MarshalHelper.WriteLongToBuffer(resultBytes, GetNodeId(node.Next), sizeof(long));
            MarshalHelper.WriteLongToBuffer(resultBytes, GetNodeId(node.Random), sizeof(long) * 2);
            MarshalHelper.WriteIntToBuffer(resultBytes, dataSize, sizeof(long) * 3);
            MarshalHelper.WriteUtf8StringToBuffer(resultBytes, node.Data, dataSize, sizeof(long) * 3 + sizeof(int));
            return resultBytes;
        }

        /// <summary>
        /// Async version of <see cref="ToBytes"/> method.
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
                var currentId = MarshalHelper.ReadLongFromBuffer(buffer, offset);
                var nextId = MarshalHelper.ReadLongFromBuffer(buffer, sizeof(long) + offset);
                var randomId = MarshalHelper.ReadLongFromBuffer(buffer, sizeof(long) * 2 + offset);
                var dataSize = MarshalHelper.ReadIntFromBuffer(buffer, sizeof(long) * 3 + offset);
                var data = MarshalHelper.ReadUtf8StringFromBuffer(buffer, sizeof(long) * 3 + sizeof(int) + offset, dataSize);
                return new PackedListNode
                {
                    CurrentNodeId = currentId,
                    NextNodeId = nextId,
                    RandomNodeId = randomId,
                    Data = data,
                    ByteSize = sizeof(long) * 3 + sizeof(int) + dataSize
                };
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                throw new ArgumentException("Invalid data has given", e);
            }
        }

        /// <summary>
        /// Async version of <see cref="FromBuffer"/> method.
        /// </summary>
        public async Task<PackedListNode> FromBufferAsync(byte[] buffer, int offset)
        {
            return await Task.Run(() => FromBuffer(buffer, offset));
        }

        /// <summary>
        /// Method pack <see cref="ListNode"/> into <see cref="PackedListNode"/> without setting <see cref="PackedListNode.ByteSize"/>
        /// </summary>
        public PackedListNode ToPackedListNode(ListNode node)
        {
            return new PackedListNode
            {
                CurrentNodeId = _idGenerator.GetId(node),
                NextNodeId = _idGenerator.GetId(node.Next),
                RandomNodeId = _idGenerator.GetId(node.Random),
                Data = node.Data,
            };
        }

        private readonly ConcurrentDictionary<long, ListNode> _passedNodes = new ConcurrentDictionary<long, ListNode>();

        private ListNode CreateNodeFromHash(long hash)
        {
            var lazy = new Lazy<ListNode>(() => new ListNode());
            return _passedNodes.GetOrAdd(hash, _ => lazy.Value);
        }

        public Task<ListNode> RestoreNodeAsync(PackedListNode packedNode)
        {
            return Task.Run(() =>
            {
                var currentNode = CreateNodeFromHash(packedNode.CurrentNodeId);
                currentNode.Data = packedNode.Data;

                if (packedNode.NextNodeId != 0)
                {
                    var nextNode = CreateNodeFromHash(packedNode.NextNodeId);
                    nextNode.Previous = currentNode;
                    currentNode.Next = nextNode;
                }

                if (packedNode.RandomNodeId == 0) return currentNode;

                var randomNode = CreateNodeFromHash(packedNode.RandomNodeId);
                currentNode.Random = randomNode;
                return currentNode;
            });
        }
    }
}
