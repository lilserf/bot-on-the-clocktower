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
			mockEnv.Setup(c => c.GetEnvironmentVariable(DatabaseFactory.MongoDbEnvironmentVar)).Returns(MockDbString);
			DatabaseFactory db = new(GetServiceProvider());

			Assert.Throws<DatabaseFactory.InvalidMongoConnectStringException>(db.Connect);
		}

		[Fact]
		public void DatabaseConnect_NoDb_ThrowsException()
		{
			var mockEnv = RegisterMock(new Mock<IEnvironment>());
			mockEnv.Setup(c => c.GetEnvironmentVariable(DatabaseFactory.MongoConnectEnvironmentVar)).Returns(MockConnectionString);
			DatabaseFactory db = new(GetServiceProvider());

			Assert.Throws<DatabaseFactory.InvalidMongoDbException>(db.Connect);
		}

		private void RegisterValidEnvironment()
		{
			var mockEnv = RegisterMock(new Mock<IEnvironment>());
			mockEnv.Setup(c => c.GetEnvironmentVariable(DatabaseFactory.MongoDbEnvironmentVar)).Returns(MockDbString);
			mockEnv.Setup(c => c.GetEnvironmentVariable(DatabaseFactory.MongoConnectEnvironmentVar)).Returns(MockConnectionString);
		}

		[Fact]
		public void DatabaseConnect_NoClient_ThrowsException()
        {
			RegisterValidEnvironment();
			RegisterMock(new Mock<IMongoClientFactory>());
			DatabaseFactory db = new(GetServiceProvider());

			Assert.Throws<DatabaseFactory.MongoClientNotCreatedException>(db.Connect);
		}

		[Fact]
		public void DatabaseConnect_NoDatabase_ThrowsException()
        {
			RegisterValidEnvironment();
			var mockClient = new Mock<IMongoClient>();
			var mockClientFactory = RegisterMock(new Mock<IMongoClientFactory>());
			mockClientFactory.Setup(cf => cf.CreateClient(It.Is<string>(s => s == MockConnectionString))).Returns(mockClient.Object);
			DatabaseFactory db = new(GetServiceProvider());

			Assert.Throws<DatabaseFactory.MongoClientNotCreatedException>(db.Connect);
		}

		[Fact]
		public void DatabaseConnect_HappyPath_CreatesTownLookup()
		{
			RegisterValidEnvironment();
			var mockDatabase = new Mock<IMongoDatabase>();
			var mockTownLookup = new Mock<ITownLookup>();
			var mockTownLookupFactory = RegisterMock(new Mock<ITownLookupFactory>());
			mockTownLookupFactory.Setup(tlf => tlf.CreateTownLookup(It.Is<IMongoDatabase>(md => md == mockDatabase.Object))).Returns(mockTownLookup.Object);
			var mockClient = new Mock<IMongoClient>();
			mockClient.Setup(c => c.GetDatabase(It.Is<string>(s => s == MockDbString), It.IsAny<MongoDatabaseSettings>())).Returns(mockDatabase.Object);
			var mockClientFactory = RegisterMock(new Mock<IMongoClientFactory>());
			mockClientFactory.Setup(cf => cf.CreateClient(It.Is<string>(s => s == MockConnectionString))).Returns(mockClient.Object);
			DatabaseFactory db = new(GetServiceProvider());

			var result = db.Connect();

			mockTownLookupFactory.Verify(tlf => tlf.CreateTownLookup(It.Is<IMongoDatabase>(md => md == mockDatabase.Object)), Times.Once);
			Assert.Equal(mockTownLookup.Object, result.GetService<ITownLookup>());
		}
	}
}
