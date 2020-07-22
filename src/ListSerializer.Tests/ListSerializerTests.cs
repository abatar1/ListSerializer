using ListSerializer.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ListSerializer.Tests
{
    public class ListSerializerTests
    {
        private readonly IListSerializer _listSerializer = new Core.ListSerializer();
        private readonly Random _random = new Random();
        private const int ListSize = 50000;
        private static string RandomData => Guid.NewGuid().ToString();

        private ListNode GenerateList(int size, double randomNodeProbability)
        {
            var createdNodes = new List<ListNode>();
            var headNode = new ListNode { Data = RandomData };
            var currentNode = headNode;
            for (var i = 0; i < size - 1; i++)
            {
                var nextNode = new ListNode { Data = RandomData, Previous = currentNode };
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

        private static bool CheckListsForEquality(ListNode expectedHead, ListNode actualHead)
        {
            var currentLeftNode = expectedHead;
            var currentRightNode = actualHead;
            while (currentLeftNode != null)
            {
                Assert.Equal(currentLeftNode.Data, currentRightNode?.Data);
                Assert.Equal(currentLeftNode.Random?.Data, currentRightNode?.Random?.Data);
                Assert.Equal(currentLeftNode.Previous?.Data, currentRightNode?.Previous?.Data);
                Assert.Equal(currentLeftNode.Next?.Data, currentRightNode?.Next?.Data);
                currentLeftNode = currentLeftNode.Next;
                currentRightNode = currentRightNode?.Next;
            }

            return true;
        }

        private static bool CheckListsForObjectNonEquality(ListNode expectedHead, ListNode actualHead)
        {
            var currentLeftNode = expectedHead;
            var currentRightNode = actualHead;
            while (currentLeftNode != null)
            {
                Assert.NotEqual(currentLeftNode.GetHashCode(), currentRightNode.GetHashCode());
                currentLeftNode = currentLeftNode.Next;
                currentRightNode = currentRightNode.Next;
            }

            return true;
        }

        [Fact]
        public async Task Serializer_SerializeAndDeserialize_TwoHeadsAreEqual()
        {
            // Arrange
            var initialHeadNode = GenerateList(ListSize, 1);

            // Act
            await using var stream = new MemoryStream();
            await _listSerializer.Serialize(initialHeadNode, stream);
            var resultHeadNode = await _listSerializer.Deserialize(stream);

            // Assert
            Assert.True(CheckListsForEquality(initialHeadNode, resultHeadNode));
        }

        [Fact]
        public async Task Serializer_SerializeAndDeserialize_SingleHeadNode()
        {
            // Arrange
            var initialHeadNode = GenerateList(1, 1);

            // Act
            await using var stream = new MemoryStream();
            await _listSerializer.Serialize(initialHeadNode, stream);
            var resultHeadNode = await _listSerializer.Deserialize(stream);

            // Assert
            Assert.True(CheckListsForEquality(initialHeadNode, resultHeadNode));
        }

        [Fact]
        public async Task Serializer_DeserializeWithInvalidBuffer_ArgumentException()
        {
            // Arrange
            var stream = new MemoryStream();
            var buffer = new byte[_random.Next(10)];
            _random.NextBytes(buffer);
            await stream.WriteAsync(buffer);

            // Act
            async Task Func() => await _listSerializer.Deserialize(stream);

            // Assert
            await Assert.ThrowsAsync<ArgumentException>(Func);
            await stream.DisposeAsync();
        }

        [Fact]
        public async Task Serializer_DeserializeEmptyBuffer_ArgumentException()
        {
            // Arrange
            var stream = new MemoryStream();

            // Act
            async Task Func() => await _listSerializer.Deserialize(stream);

            // Assert
            await Assert.ThrowsAsync<ArgumentException>(Func);
            await stream.DisposeAsync();
        }

        [Fact]
        public async Task Serializer_DeepCopy_HashesNotEqualDataEqual()
        {
            // Arrange
            var initialHeadNode = GenerateList(ListSize, 1);

            // Act
            var resultHeadNode = await _listSerializer.DeepCopy(initialHeadNode);

            // Assert
            Assert.True(CheckListsForEquality(initialHeadNode, resultHeadNode));
            Assert.True(CheckListsForObjectNonEquality(initialHeadNode, resultHeadNode));
        }

        [Fact]
        public async Task Serializer_DeepCopy_SingleHeadNode()
        {
            // Arrange
            var initialHeadNode = GenerateList(1, 1);

            // Act
            var resultHeadNode = await _listSerializer.DeepCopy(initialHeadNode);

            // Assert
            Assert.True(CheckListsForEquality(initialHeadNode, resultHeadNode));
            Assert.True(CheckListsForObjectNonEquality(initialHeadNode, resultHeadNode));
        }
    }
}
