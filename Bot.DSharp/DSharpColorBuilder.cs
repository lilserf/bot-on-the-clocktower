using System;
using Bot.Api;
using DSharpPlus.Entities;

namespace Bot.DSharp
{
    public class DSharpColorBuilder : IColorBuilder
    {
        public IColor Build(int rgb) => new DSharpColor(new DiscordColor(rgb));
        public IColor Build(byte r, byte g, byte b) => new DSharpColor(new DiscordColor(r, g, b));

        public IColor None { get; } = new DSharpColor(DiscordColor.None);

        public IColor Black { get; } = new DSharpColor(DiscordColor.Black);

        public IColor White { get; } = new DSharpColor(DiscordColor.White);

        public IColor Gray { get; } = new DSharpColor(DiscordColor.Gray);

        public IColor DarkGray { get; } = new DSharpColor(DiscordColor.DarkGray);

        public IColor LightGray { get; } = new DSharpColor(DiscordColor.LightGray);

        public IColor VeryDarkGray { get; } = new DSharpColor(DiscordColor.VeryDarkGray);

        public IColor Blurple { get; } = new DSharpColor(DiscordColor.Blurple);

        public IColor Grayple { get; } = new DSharpColor(DiscordColor.Grayple);

        public IColor DarkButNotBlack { get; } = new DSharpColor(DiscordColor.DarkButNotBlack);

        public IColor NotQuiteBlack { get; } = new DSharpColor(DiscordColor.NotQuiteBlack);

        public IColor Red { get; } = new DSharpColor(DiscordColor.Red);

        public IColor DarkRed { get; } = new DSharpColor(DiscordColor.DarkRed);

        public IColor Green { get; } = new DSharpColor(DiscordColor.Green);

        public IColor DarkGreen { get; } = new DSharpColor(DiscordColor.DarkGreen);

        public IColor Blue { get; } = new DSharpColor(DiscordColor.Blue);

        public IColor DarkBlue { get; } = new DSharpColor(DiscordColor.DarkBlue);

        public IColor Yellow { get; } = new DSharpColor(DiscordColor.Yellow);

        public IColor Cyan { get; } = new DSharpColor(DiscordColor.Cyan);

        public IColor Magenta { get; } = new DSharpColor(DiscordColor.Magenta);

        public IColor Teal { get; } = new DSharpColor(DiscordColor.Teal);

        public IColor Aquamarine { get; } = new DSharpColor(DiscordColor.Aquamarine);

        public IColor Gold { get; } = new DSharpColor(DiscordColor.Gold);

        public IColor Goldenrod { get; } = new DSharpColor(DiscordColor.Goldenrod);

        public IColor Azure { get; } = new DSharpColor(DiscordColor.Azure);

        public IColor Rose { get; } = new DSharpColor(DiscordColor.Rose);

        public IColor SpringGreen { get; } = new DSharpColor(DiscordColor.SpringGreen);

        public IColor Chartreuse { get; } = new DSharpColor(DiscordColor.Chartreuse);

        public IColor Orange { get; } = new DSharpColor(DiscordColor.Orange);

        public IColor Purple { get; } = new DSharpColor(DiscordColor.Purple);

        public IColor Violet { get; } = new DSharpColor(DiscordColor.Violet);

        public IColor Brown { get; } = new DSharpColor(DiscordColor.Brown);

        public IColor HotPink { get; } = new DSharpColor(DiscordColor.HotPink);

        public IColor Lilac { get; } = new DSharpColor(DiscordColor.Lilac);

        public IColor CornflowerBlue { get; } = new DSharpColor(DiscordColor.CornflowerBlue);

        public IColor MidnightBlue { get; } = new DSharpColor(DiscordColor.MidnightBlue);

        public IColor Wheat { get; } = new DSharpColor(DiscordColor.Wheat);

        public IColor IndianRed { get; } = new DSharpColor(DiscordColor.IndianRed);

        public IColor Turquoise { get; } = new DSharpColor(DiscordColor.Turquoise);

        public IColor SapGreen { get; } = new DSharpColor(DiscordColor.SapGreen);

        public IColor PhthaloBlue { get; } = new DSharpColor(DiscordColor.PhthaloBlue);

        public IColor PhthaloGreen { get; } = new DSharpColor(DiscordColor.PhthaloGreen);

        public IColor Sienna { get; } = new DSharpColor(DiscordColor.Sienna);
    }
}
