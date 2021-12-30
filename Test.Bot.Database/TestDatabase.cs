using Bot.Api;
using Bot.Database;
using MongoDB.Driver;
using Moq;
using System;
using Test.Bot.Base;
using Xunit;

namespace Test.Bot.Database
{
	public class TestDatabase : TestBase
	{
		private const string MockConnectionString = "mock-conn-string";
		private const string MockDbString = "mock-db-string";

		[Fact]
		public void DatabaseConnect_NoConnString_ThrowsException()
		{
			var mockEnv = RegisterMock(new Mock<IEnvironment>());
			DatabaseFactory db = new(GetServiceProvider());

			Assert.Throws<DatabaseFactory.InvalidMongoConnectStringException>(db.ConnectToMongoClient);
		}

		[Fact]
		public void DatabaseConnect_NoClient_ThrowsException()
        {
			var mockEnv = RegisterMock(new Mock<IEnvironment>());
			mockEnv.Setup(e => e.GetEnvironmentVariable(It.Is<string>(s => s == DatabaseFactory.MongoConnectEnvironmentVar))).Returns(MockConnectionString);

			RegisterMock(new Mock<IMongoClientFactory>());
			DatabaseFactory db = new(GetServiceProvider());

			Assert.Throws<DatabaseFactory.MongoClientNotCreatedException>(db.ConnectToMongoClient);
		}

		[Fact]
		public void DatabaseConnect_NoDb_ThrowsException()
		{
			var mockClient = new Mock<IMongoClient>();

			var mockEnv = RegisterMock(new Mock<IEnvironment>());
			DatabaseFactory db = new(GetServiceProvider());

			Assert.Throws<DatabaseFactory.InvalidMongoDbException>(() => db.ConnectToMongoDatabase(mockClient.Object));
		}

		[Fact]
		public void DatabaseConnect_NoDatabase_ThrowsException()
		{
			var mockEnv = RegisterMock(new Mock<IEnvironment>());
			mockEnv.Setup(e => e.GetEnvironmentVariable(It.Is<string>(s => s == DatabaseFactory.MongoDbEnvironmentVar))).Returns(MockDbString);

			var mockClient = new Mock<IMongoClient>();
			var mockClientFactory = RegisterMock(new Mock<IMongoClientFactory>());
			mockClientFactory.Setup(cf => cf.CreateClient(It.Is<string>(s => s == MockConnectionString))).Returns(mockClient.Object);
			DatabaseFactory db = new(GetServiceProvider());

			Assert.Throws<DatabaseFactory.MongoDbNotFoundException>(() => db.ConnectToMongoDatabase(mockClient.Object));
		}

		[Fact]
		public void DatabaseConnect_CreateServices_CreatesTownLookup()
		{
			var mockDatabase = new Mock<IMongoDatabase>();
			var mockTownLookup = new Mock<ITownLookup>();
			var mockTownLookupFactory = RegisterMock(new Mock<ITownLookupFactory>());
			mockTownLookupFactory.Setup(tlf => tlf.CreateTownLookup(It.Is<IMongoDatabase>(md => md == mockDatabase.Object))).Returns(mockTownLookup.Object);
			DatabaseFactory db = new(GetServiceProvider());

			var result = db.CreateDatabaseServices(mockDatabase.Object);

			mockTownLookupFactory.Verify(tlf => tlf.CreateTownLookup(It.Is<IMongoDatabase>(md => md == mockDatabase.Object)), Times.Once);
			Assert.Equal(mockTownLookup.Object, result.GetService<ITownLookup>());
		}
	}
}
