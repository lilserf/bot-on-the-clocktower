using System.Collections.Generic;

namespace Bot.Api
{
    public interface IInteractionResponseBuilder
	{
		IInteractionResponseBuilder WithTitle(string title);
		IInteractionResponseBuilder WithCustomId(string customId);
		IInteractionResponseBuilder WithContent(string content);

		IInteractionResponseBuilder AddComponents(params IBotComponent[] components);
		IInteractionResponseBuilder AddEmbeds(IEnumerable<IEmbed> embeds);
	}

	public static class IInteractionResponseBuilderExtensions
    {
		public static IInteractionResponseBuilder AddEmbed(IInteractionResponseBuilder @this, IEmbed embed)
        {
			return @this.AddEmbeds(new[] { embed });
        }
	}
}
