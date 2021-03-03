using System;
using System.Collections.Generic;
using System.Text;

namespace DSharpPlus.SlashCommands
{
    /// <summary>
    /// Respresents the type of interaction response
    /// </summary>
    public enum DiscordInteractionResponseType
    {
        /// <summary>
        /// A pong for a ping
        /// </summary>
        Pong = 1,

        /// <summary>
        /// Responds with a message, showing the user's input
        /// </summary>
        ChannelMessageWithSource = 4,

        /// <summary>
        /// Acknowledge an interaction and edit to a response later, the user sees a loading state
        /// </summary>
        DeferredChannelMessageWithSource
    }
}
