using System;
using System.Collections.Concurrent;
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
        private readonly ListNodePacker _listNodePacker = new ListNodePacker();

        private static IEnumerable<ListNode> GetAllNodes(ListNode headNode)
        {
            var currentNode = headNode;
            while (currentNode != null)
            {
                yield return currentNode;
                currentNode = currentNode.Next;
            }
        }

        private static ListNode CreateNodeFromHash(ConcurrentDictionary<long, ListNode> passedNodes, long hash)
        {
            var lazy = new Lazy<ListNode>(() => new ListNode());
            return passedNodes.GetOrAdd(hash, _ => lazy.Value);
        }

        private IEnumerable<PackedListNode> ReadPackedNodesFromBuffer(byte[] buffer, int initialOffset = 0)
        {
            while (initialOffset < buffer.Length)
            {
                var packedNode = _listNodePacker.FromBuffer(buffer, initialOffset);
                yield return packedNode;
                initialOffset += packedNode.ByteSize;
            }
        }

        private static Task<ListNode> UnpackNode(PackedListNode packedNode, ConcurrentDictionary<long, ListNode> passedNodes)
        {
            return Task.Run(() =>
            {
                var currentNode = CreateNodeFromHash(passedNodes, packedNode.CurrentNodeId);
                currentNode.Data = packedNode.Data;

                if (packedNode.NextNodeId != 0)
                {
                    var nextNode = CreateNodeFromHash(passedNodes, packedNode.NextNodeId);
                    nextNode.Previous = currentNode;
                    currentNode.Next = nextNode;
                }

                if (packedNode.RandomNodeId == 0) return currentNode;

                var randomNode = CreateNodeFromHash(passedNodes, packedNode.RandomNodeId);
                currentNode.Random = randomNode;
                return currentNode;
            });
        }

        public async Task Serialize(ListNode head, Stream s)
        {
            Debug.Assert(head.Previous == null, "Head node should be passed to this method.");
            var tasks = GetAllNodes(head)
                .Select(_listNodePacker.ToBytesAsync);
            var conversionTask = await Task.WhenAll(tasks);
            var bytes = ByteArrayHelper.Combine(conversionTask);
            s.Write(bytes);
            s.Position = 0;
        }

        public async Task<ListNode> Deserialize(Stream s)
        {
            byte[] buffer;
            if (s is MemoryStream ms)
            {
                buffer = ms.ToArray();
            }
            else
            {
                await using var memoryStream = new MemoryStream();
                s.Position = 0;
                await s.CopyToAsync(memoryStream);
                buffer = memoryStream.ToArray();
            }
            if (buffer.Length == 0) throw new ArgumentException("Buffer's length is 0.");

            var passedNodes = new ConcurrentDictionary<long, ListNode>();
            var nodesTasks = ReadPackedNodesFromBuffer(buffer)
                .Select(packedNode => UnpackNode(packedNode, passedNodes));
            var nodes = await Task.WhenAll(nodesTasks);
            return nodes[0];
        }

        public async Task<ListNode> DeepCopy(ListNode head)
        {
            Debug.Assert(head.Previous == null, "Head node should be passed to this method.");
            var passedNodes = new ConcurrentDictionary<long, ListNode>();
            var cloningTasks = GetAllNodes(head)
                .Select(node => _listNodePacker.ToPackedListNode(node))
                .Select(node => UnpackNode(node, passedNodes));
            var clonedNodes = await Task.WhenAll(cloningTasks);
            return clonedNodes[0];
        }
    }
}
