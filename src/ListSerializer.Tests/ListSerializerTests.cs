using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ListSerializer.Core;
using Xunit;

namespace ListSerializer.Tests
{
    public class ListSerializerTests
    {
        private readonly IListSerializer _listSerializer;
        private readonly Random _random;

        public ListSerializerTests()
        {
            _listSerializer = new Core.ListSerializer();
            _random = new Random();
        }

        private ListNode GenerateList(int size, double randomNodeProbability)
        {
            var createdNodes = new List<ListNode>();
            var headNode = new ListNode {Data = Guid.NewGuid().ToString()};
            var currentNode = headNode;
            for (var i = 0; i < size; i++)
            {
                var nextNode = new ListNode {Data = Guid.NewGuid().ToString(), Previous = currentNode};
                currentNode.Next = nextNode;
                createdNodes.Add(nextNode);
                currentNode = nextNode;
            }

            var selectedNodesWithRandom = createdNodes
                .Where(node => _random.NextDouble() < randomNodeProbability);
            foreach (var node in selectedNodesWithRandom)
            {
                node.Random = createdNodes[_random.Next(createdNodes.Count)];
            }

            return headNode;
        }

        private bool CheckListsForEquality(ListNode leftHead, ListNode rightHead)
        {
            var currentLeftNode = leftHead;
            var currentRightNode = rightHead;
            while (currentLeftNode != null)
            {
                if (currentLeftNode.Data != currentRightNode?.Data) return false;
                if (currentLeftNode.Random?.Data != currentRightNode?.Random?.Data) return false;
                if (currentLeftNode.Previous?.Data != currentRightNode?.Previous?.Data) return false;
                if (currentLeftNode.Next?.Data != currentRightNode?.Next?.Data) return false;
                currentLeftNode = currentLeftNode.Next;
                currentRightNode = currentRightNode?.Next;
            }

            return true;
        }

        private bool CheckListsForObjectNonEquality(ListNode leftHead, ListNode rightHead)
        {
            var currentLeftNode = leftHead;
            var currentRightNode = rightHead;
            while (currentLeftNode != null)
            {
                if (currentLeftNode.GetHashCode() == currentRightNode.GetHashCode()) return false;
                currentLeftNode = currentLeftNode.Next;
                currentRightNode = currentRightNode.Next;
            }

            return true;
        }

        [Fact]
        public async Task Serializer_SerializeAndDeserialize_TwoHeadsAreEqual()
        {
            // Arrange
            var initialHeadNode = GenerateList(100, 1);

            // Act
            await using var stream = new MemoryStream();
            await _listSerializer.Serialize(initialHeadNode, stream);
            var resultHeadNode = await _listSerializer.Deserialize(stream);

            // Assert
            Assert.True(CheckListsForEquality(initialHeadNode, resultHeadNode));
        }

        [Fact]
        public async Task Serializer_DeepCopy_HashesNotEqualDataEqual()
        {
            // Arrange
            var initialHeadNode = GenerateList(100, 1);

            // Act
            var resultHeadNode = await _listSerializer.DeepCopy(initialHeadNode);

            // Assert
            Assert.True(CheckListsForEquality(initialHeadNode, resultHeadNode));
            Assert.True(CheckListsForObjectNonEquality(initialHeadNode, resultHeadNode));
        }
    }
}
