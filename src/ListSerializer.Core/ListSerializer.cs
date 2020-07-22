using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ListSerializer.Core.Helpers;

namespace ListSerializer.Core
{
    public class ListSerializer : IListSerializer
    {
        private static IEnumerable<ListNode> GetAllNodes(ListNode headNode)
        {
            var currentNode = headNode;
            while (currentNode != null)
            {
                yield return currentNode;
                currentNode = currentNode.Next;
            }
        }

        private static async Task<byte[]> ReadBufferFromStream(Stream stream)
        {
            byte[] buffer;
            if (stream is MemoryStream ms)
            {
                buffer = ms.ToArray();
            }
            else
            {
                await using var memoryStream = new MemoryStream();
                stream.Position = 0;
                await stream.CopyToAsync(memoryStream);
                buffer = memoryStream.ToArray();
            }
            if (buffer.Length == 0) throw new ArgumentException("Buffer's length is 0.");
            return buffer;
        }

        private static IEnumerable<PackedListNode> ReadPackedNodesFromBuffer(ListNodePacker packer, byte[] buffer)
        {
            var initialOffset = 0;
            while (initialOffset < buffer.Length)
            {
                var packedNode = packer.FromBuffer(buffer, initialOffset);
                yield return packedNode;
                initialOffset += packedNode.ByteSize;
            }
        }

        public async Task Serialize(ListNode head, Stream s)
        {
            Debug.Assert(head.Previous == null, "Head node should be passed to this method.");
            var nodePacker = new ListNodePacker();
            var tasks = GetAllNodes(head)
                .Select(nodePacker.ToBytesAsync);
            var conversionTask = await Task.WhenAll(tasks);
            var bytes = ByteArrayHelper.Combine(conversionTask);
            s.Write(bytes);
            s.Position = 0;
        }

        public async Task<ListNode> Deserialize(Stream s)
        {
            var buffer = await ReadBufferFromStream(s);
            var nodePacker = new ListNodePacker();
            var nodesTasks = ReadPackedNodesFromBuffer(nodePacker, buffer)
                .Select(node => nodePacker.RestoreNodeAsync(node));
            var nodes = await Task.WhenAll(nodesTasks);
            return nodes[0];
        }

        public async Task<ListNode> DeepCopy(ListNode head)
        {
            Debug.Assert(head.Previous == null, "Head node should be passed to this method.");
            var nodePacker = new ListNodePacker();
            var cloningTasks = GetAllNodes(head)
                .Select(node => nodePacker.ToPackedListNode(node))
                .Select(node => nodePacker.RestoreNodeAsync(node));
            var clonedNodes = await Task.WhenAll(cloningTasks);
            return clonedNodes[0];
        }
    }
}
