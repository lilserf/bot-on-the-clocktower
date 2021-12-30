using Bot.Api;
using Bot.Database;
using Moq;
using System;
using Test.Bot.Base;
using Xunit;

namespace Test.Bot.Database
{
	public class TestDatabase : TestBase
	{
		[Fact]
		public void DatabaseConnect_NoConnString_ThrowsException()
		{
			var mockEnv = RegisterMock(new Mock<IEnvironment>());
			DatabaseFactory db = new(GetServiceProvider());

			Assert.Throws<DatabaseFactory.InvalidMongoConnectStringException>(() => db.Connect());
		}

		[Fact]
		public void DatabaseConnect_NoDb_ThrowsException()
		{
			var mockEnv = RegisterMock(new Mock<IEnvironment>());
			mockEnv.Setup(c => c.GetEnvironmentVariable("MONGO_CONNECT")).Returns("mock-conn-string");
			DatabaseFactory db = new(GetServiceProvider());

			Assert.Throws<DatabaseFactory.InvalidMongoDbException>(() => db.Connect());
		}

	}
}
