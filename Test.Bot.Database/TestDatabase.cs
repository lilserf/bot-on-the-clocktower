using Bot.Api;
using Bot.Api.Database;
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
		public void ConnectToMongo_NoConnString_ThrowsException()
		{
			var mockEnv = RegisterMock(new Mock<IEnvironment>());
			DatabaseFactory db = new(GetServiceProvider());

			Assert.Throws<DatabaseFactory.InvalidMongoConnectStringException>(db.ConnectToMongoClient);
		}

		[Fact]
		public void ConnectToMongo_NoClient_ThrowsException()
        {
			var mockEnv = RegisterMock(new Mock<IEnvironment>());
			mockEnv.Setup(e => e.GetEnvironmentVariable(It.Is<string>(s => s == DatabaseFactory.MongoConnectEnvironmentVar))).Returns(MockConnectionString);

			RegisterMock(new Mock<IMongoClientFactory>());
			DatabaseFactory db = new(GetServiceProvider());

			Assert.Throws<DatabaseFactory.MongoClientNotCreatedException>(db.ConnectToMongoClient);
		}

		[Fact]
		public void ConnectToMongo_ClientMade_Works()
        {
			var mockEnv = RegisterMock(new Mock<IEnvironment>());
			mockEnv.Setup(e => e.GetEnvironmentVariable(It.Is<string>(s => s == DatabaseFactory.MongoConnectEnvironmentVar))).Returns(MockConnectionString);

			var mockClient = new Mock<IMongoClient>();

			var clientFactory = RegisterMock(new Mock<IMongoClientFactory>());
			clientFactory.Setup(cf => cf.CreateClient(It.Is<string>(s => s == MockConnectionString))).Returns(mockClient.Object);
			DatabaseFactory db = new(GetServiceProvider());

			var result = db.ConnectToMongoClient();

			Assert.Equal(mockClient.Object, result);
		}

		[Fact]
		public void ConnectToDb_NoDbString_ThrowsException()
		{
			var mockClient = new Mock<IMongoClient>();

			var mockEnv = RegisterMock(new Mock<IEnvironment>());
			DatabaseFactory db = new(GetServiceProvider());

			Assert.Throws<DatabaseFactory.InvalidMongoDbException>(() => db.ConnectToMongoDatabase(mockClient.Object));
		}

		[Fact]
		public void ConnectToDb_NoDbFound_ThrowsException()
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
		public void ConnectToDb_DbMade_Works()
		{
			var mockEnv = RegisterMock(new Mock<IEnvironment>());
			mockEnv.Setup(e => e.GetEnvironmentVariable(It.Is<string>(s => s == DatabaseFactory.MongoDbEnvironmentVar))).Returns(MockDbString);

			var mockDb = new Mock<IMongoDatabase>();
			var mockClient = new Mock<IMongoClient>();
			mockClient.Setup(c => c.GetDatabase(It.Is<string>(s => s == MockDbString), It.IsAny<MongoDatabaseSettings>())).Returns(mockDb.Object);
			var mockClientFactory = RegisterMock(new Mock<IMongoClientFactory>());
			mockClientFactory.Setup(cf => cf.CreateClient(It.Is<string>(s => s == MockConnectionString))).Returns(mockClient.Object);
			DatabaseFactory db = new(GetServiceProvider());

			var result = db.ConnectToMongoDatabase(mockClient.Object);

			Assert.Equal(mockDb.Object, result);
		}

		[Fact]
		public void CreateDbServices_CreatesTownLookup()
		{
			var mockDatabase = new Mock<IMongoDatabase>(MockBehavior.Strict);
			var mockTownLookup = new Mock<ITownDatabase>(MockBehavior.Strict);
			var mockGameActivityDb = new Mock<IGameActivityDatabase>(MockBehavior.Strict);
			var mockLookupRoleDb = new Mock<ILookupRoleDatabase>(MockBehavior.Strict);
			var mockLookupRoleDbFactory = RegisterMock(new Mock<ILookupRoleDatabaseFactory>(MockBehavior.Strict));
			var mockTownLookupFactory = RegisterMock(new Mock<ITownDatabaseFactory>(MockBehavior.Strict));
			var mockGameActivityDbFactory = RegisterMock(new Mock<IGameActivityDatabaseFactory>(MockBehavior.Strict));

			mockGameActivityDbFactory.Setup(gadbf => gadbf.CreateGameActivityDatabase(It.Is<IMongoDatabase>(md => md == mockDatabase.Object))).Returns(mockGameActivityDb.Object);
			mockLookupRoleDbFactory.Setup(lrdbf => lrdbf.CreateLookupRoleDatabase(It.Is<IMongoDatabase>(md => md == mockDatabase.Object))).Returns(mockLookupRoleDb.Object);

			mockTownLookupFactory.Setup(tlf => tlf.CreateTownLookup(It.Is<IMongoDatabase>(md => md == mockDatabase.Object))).Returns(mockTownLookup.Object);
			DatabaseFactory db = new(GetServiceProvider());

			var result = db.CreateDatabaseServices(mockDatabase.Object);

			mockTownLookupFactory.Verify(tlf => tlf.CreateTownLookup(It.Is<IMongoDatabase>(md => md == mockDatabase.Object)), Times.Once);
			Assert.Equal(mockTownLookup.Object, result.GetService<ITownDatabase>());
		}
	}
}
