namespace Bot.Api
{
    public interface IColorBuilder
    {
        IColor Build(int rgb);
        IColor Build(byte r, byte g, byte b);

        IColor None { get; }
        IColor Black { get; }
        IColor White { get; }
        IColor Gray { get; }
        IColor DarkGray { get; }
        IColor LightGray { get; }
        IColor VeryDarkGray { get; }
        IColor Blurple { get; }
        IColor Grayple { get; }
        IColor DarkButNotBlack { get; }
        IColor NotQuiteBlack { get; }
        IColor Red { get; }
        IColor DarkRed { get; }
        IColor Green { get; }
        IColor DarkGreen { get; }
        IColor Blue { get; }
        IColor DarkBlue { get; }
        IColor Yellow { get; }
        IColor Cyan { get; }
        IColor Magenta { get; }
        IColor Teal { get; }
        IColor Aquamarine { get; }
        IColor Gold { get; }
        IColor Goldenrod { get; }
        IColor Azure { get; }
        IColor Rose { get; }
        IColor SpringGreen { get; }
        IColor Chartreuse { get; }
        IColor Orange { get; }
        IColor Purple { get; }
        IColor Violet { get; }
        IColor Brown { get; }
        IColor HotPink { get; }
        IColor Lilac { get; }
        IColor CornflowerBlue { get; }
        IColor MidnightBlue { get; }
        IColor Wheat { get; }
        IColor IndianRed { get; }
        IColor Turquoise { get; }
        IColor SapGreen { get; }
        IColor PhthaloBlue { get; }
        IColor PhthaloGreen { get; }
        IColor Sienna { get; }
    }
}
