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
        private readonly ListNodePacker _listNodePacker;

        public ListSerializer()
        {
            _listNodePacker = new ListNodePacker();
        }

        private static IEnumerable<ListNode> GetAllNodes(ListNode headNode)
        {
            var currentNode = headNode;
            while (currentNode != null)
            {
                yield return currentNode;
                currentNode = currentNode.Next;
            }
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

        private static ListNode CreateNodeFromHash(Dictionary<int, ListNode> passedNodes, int hash)
        {
            if (passedNodes.TryGetValue(hash, out var node)) return node;
            node = new ListNode();
            passedNodes.Add(hash, node);
            return node;
        }

        private static ListNode CreateNodeFromHash(ConcurrentDictionary<int, ListNode> passedNodes, int hash)
        {
            if (passedNodes.TryGetValue(hash, out var node)) return node;
            node = new ListNode();
            passedNodes.TryAdd(hash, node);
            return node;
        }

        private (ListNode, int byteSize) ReadNodeFromBuffer(byte[] buffer, Dictionary<int, ListNode> passedNodes, int offset)
        {
            var packedNode = _listNodePacker.FromBuffer(buffer, offset);
            if (packedNode.CurrentNodeHashcode == 0) throw new ArgumentException("Wrong data has given.");

            var currentNode = CreateNodeFromHash(passedNodes, packedNode.CurrentNodeHashcode);
            currentNode.Data ??= packedNode.Data;

            if (packedNode.NextNodeHashcode != 0)
            {
                var nextNode = CreateNodeFromHash(passedNodes, packedNode.NextNodeHashcode);
                nextNode.Previous ??= currentNode;
                currentNode.Next = nextNode;
            }

            if (packedNode.RandomNodeHashcode == 0) return (currentNode, packedNode.ByteSize);

            var randomNode = CreateNodeFromHash(passedNodes, packedNode.NextNodeHashcode);
            currentNode.Random = randomNode;
            return (currentNode, packedNode.ByteSize);
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

            var passedNodes = new Dictionary<int, ListNode>();
            var (headNode, totalOffset) = ReadNodeFromBuffer(buffer, passedNodes, 0);
            while (totalOffset < buffer.Length)
            {
                var (_, byteSize) = ReadNodeFromBuffer(buffer, passedNodes, totalOffset);
                totalOffset += byteSize;
            }
            return headNode;
        }

        private static Task<ListNode> CopyNode(ListNode node, ConcurrentDictionary<int, ListNode> passedNodes)
        {
            return Task.Run(() =>
            {
                var clonedNode = CreateNodeFromHash(passedNodes, node.GetHashCode());
                clonedNode.Data ??= node.Data;

                if (node.Next != null)
                {
                    var nextClonedNode = CreateNodeFromHash(passedNodes, node.Next.GetHashCode());
                    nextClonedNode.Previous ??= clonedNode;
                    clonedNode.Next = nextClonedNode;
                }

                if (node.Random == null) return clonedNode;

                var randomClonedNode = CreateNodeFromHash(passedNodes, node.Random.GetHashCode());
                clonedNode.Random = randomClonedNode;
                return clonedNode;
            });
        }

        public async Task<ListNode> DeepCopy(ListNode head)
        {
            Debug.Assert(head.Previous == null, "Head node should be passed to this method.");
            var passedNodes = new ConcurrentDictionary<int, ListNode>();
            var cloningTasks = GetAllNodes(head)
                .Select(node => CopyNode(node, passedNodes));
            var clonedNodes = await Task.WhenAll(cloningTasks);
            return clonedNodes[0];
        }
    }
}
