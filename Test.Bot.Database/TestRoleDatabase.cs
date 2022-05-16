using Bot.Database;
using MongoDB.Driver;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Test.Bot.Base;
using Xunit;

namespace Test.Bot.Database
{
    public class TestRoleDatabase : TestBase
    {
        private readonly Mock<IMongoDatabase> m_mockDatabase = new(MockBehavior.Strict);
        private readonly Mock<IMongoCollection<MongoLookupRoleRecord>> m_mockCollection = new(MockBehavior.Strict);

        public TestRoleDatabase()
        {
            m_mockDatabase.Setup(db => db.GetCollection<MongoLookupRoleRecord>(It.Is<string>(s => s == LookupRoleDatabase.CollectionName), It.IsAny<MongoCollectionSettings>())).Returns(m_mockCollection.Object);
        }

        [Fact]
        public void GetUrls_NullDoc_ReturnsEmpty()
        {
            SetupEmptyFind();

            var ld = new LookupRoleDatabase(m_mockDatabase.Object);

            var ret = AssertCompletedTask(() => ld.GetScriptUrlsAsync(123u));

            Assert.Empty(ret);
        }

        [Fact]
        public void Remove_NullDoc_DoesNothing()
        {
            SetupEmptyFind();

            var ld = new LookupRoleDatabase(m_mockDatabase.Object);

            AssertCompletedTask(() => ld.RemoveScriptUrlAsync(123u, "some url"));
        }

        private void SetupEmptyFind()
        {
            m_mockCollection.Setup(c => c.FindAsync(It.IsAny<FilterDefinition<MongoLookupRoleRecord>>(), It.IsAny<FindOptions<MongoLookupRoleRecord>>(), It.IsAny<CancellationToken>())).Returns(TestEmptyCursor<MongoLookupRoleRecord>.CreateAsync());
        }

        private class TestEmptyCursor<TDocument> : IAsyncCursor<TDocument>
        {
            public IEnumerable<TDocument> Current => throw new OverflowException();
            public void Dispose() {}
            public bool MoveNext(CancellationToken cancellationToken = default) => false;
            public Task<bool> MoveNextAsync(CancellationToken cancellationToken = default) => Task.FromResult(false);

            public static Task<IAsyncCursor<TDocument>> CreateAsync() => Task.FromResult<IAsyncCursor<TDocument>>(new TestEmptyCursor<TDocument>());
        }
    }
}
