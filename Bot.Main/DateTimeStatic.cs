using Bot.Api;
using System;

namespace Bot.Main
{
    public class DateTimeStatic : IDateTime
    {
        public DateTime Now => DateTime.Now;
    }
}
