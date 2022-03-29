using Bot.Api.Lookup;
using Bot.Core.Lookup;
using System;
using Test.Bot.Base;
using Xunit;

namespace Test.Bot.Core.Lookup
{
    public class TestLookupServices : TestBase
    {
        [Theory]
        [InlineData(typeof(ICustomScriptParser), typeof(CustomScriptParser))]
        [InlineData(typeof(IStringDownloader), typeof(StringDownloader))]
        [InlineData(typeof(ICharacterStorage), typeof(CharacterStorage))]
        [InlineData(typeof(ICharacterLookup), typeof(CharacterLookup))]
        [InlineData(typeof(IOfficialCharacterCache), typeof(OfficialCharacterCache))]
        [InlineData(typeof(ICustomScriptCache), typeof(CustomScriptCache))]
        [InlineData(typeof(IOfficialUrlProvider), typeof(OfficialUrlProvider))]
        [InlineData(typeof(IOfficialScriptParser), typeof(OfficialScriptParser))]
        public void RegisterLookupServices_CreatesAllRequiredServices(Type serviceInterface, Type serviceImpl)
        {
            var newSp = LookupServiceFactory.RegisterLookupServices(GetServiceProvider());
            var service = newSp.GetService(serviceInterface);

            Assert.NotNull(service);
            Assert.IsType(serviceImpl, service);
        }
    }
}
