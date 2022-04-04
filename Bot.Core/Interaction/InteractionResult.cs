using System;
using System.Collections.Generic;
using Bot.Api;

namespace Bot.Core.Interaction
{
    public class InteractionResult
    {
        public IReadOnlyCollection<string> LogMessages => m_logMessages;
        public string Message { get; private set; }
        public bool IncludeComponents { get; private set; } = false;
        public IBotComponent[][] ComponentSets { get; private set; } = new IBotComponent[][] {};
        public IEmbed[] Embeds { get; private set; } = new IEmbed[] {};

        public InteractionResult AddLogMessages(IEnumerable<string> logMessages)
        {
            m_logMessages.AddRange(logMessages);
            return this;
        }

        public InteractionResult WithComponents(params IBotComponent[][] componentSets)
        {
            IncludeComponents = true;
            ComponentSets = componentSets;
            return this;
        }

        public InteractionResult WithEmbeds(params IEmbed[] embeds)
        {
            Embeds = embeds;
            return this;
        }

        private readonly List<string> m_logMessages = new();

        public static InteractionResult FromMessage(string message)
        {
            return new InteractionResult(message);
        }

        public static InteractionResult FromMessageAndComponents(string message, params IBotComponent[][] componentSets)
        {
            var ret = new InteractionResult(message);
            ret.IncludeComponents = true;
            ret.ComponentSets = componentSets;
            return ret;
        }

        public static InteractionResult FromMessageAndEmbeds(string message, params IEmbed[] embeds)
        {
            var ret = new InteractionResult(message);
            ret.Embeds = embeds;
            return ret;
        }


        public static implicit operator InteractionResult(string message)
        {
            return FromMessage(message);
        }

        private InteractionResult(string message)
        {
            Message = message;
        }
    }
}
