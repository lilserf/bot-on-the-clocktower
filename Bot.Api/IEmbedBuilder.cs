namespace Bot.Api
{
    public interface IEmbedBuilder
    {
        IEmbed Build();

        IEmbedBuilder WithTitle(string title);
        IEmbedBuilder WithDescription(string description);
        IEmbedBuilder WithImageUrl(string url);
        IEmbedBuilder WithAuthor(string? name = null, string? url = null, string? iconUrl = null);
        IEmbedBuilder WithFooter(string? text = null, string? iconUrl = null);
        IEmbedBuilder WithColor(IColor color);
        IEmbedBuilder AddField(string name, string value, bool inline = false);
    }
}
