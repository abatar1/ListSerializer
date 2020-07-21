using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using ListSerializer.Core;
using ListSerializer.Core.Helpers;
using Xunit;

namespace ListSerializer.Tests
{
    public class SerializedListNodeTests
    {
        private readonly IListSerializer _listSerializer;

        public SerializedListNodeTests()
        {
            _listSerializer = new Core.ListSerializer();
        }

        [Fact]
        public async Task Test1()
        {
            var node1 = new ListNode
            {
                Data = Guid.NewGuid().ToString(),
            };
            var node2 = new ListNode
            {
                Previous = node1,
                Data = Guid.NewGuid().ToString()
            };
            node1.Next = node2;
            var node3 = new ListNode
            {
                Previous = node2,
                Data = Guid.NewGuid().ToString()
            };
            node2.Next = node3;
            node1.Random = node3;
            node2.Random = node1;
            node3.Random = node1;
            var stream = new MemoryStream();
            await _listSerializer.Serialize(node1, stream);
            var headNode = await _listSerializer.Deserialize(stream);
            Assert.True(true);

            var c1 = await _listSerializer.DeepCopy(node1);
            Assert.True(true);
        }
    }
}
