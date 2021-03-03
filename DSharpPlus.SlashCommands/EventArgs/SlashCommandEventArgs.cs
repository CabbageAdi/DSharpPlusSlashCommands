using System;
using System.Collections.Generic;
using System.Text;
using DSharpPlus.SlashCommands.Entities;
using Emzi0767.Utilities;

namespace DSharpPlus.SlashCommands.EventArgs
{
    /// <summary>
    /// Base class for the slash command extension related events
    /// </summary>
    public class SlashCommandEventArgs : AsyncEventArgs
    {
        /// <summary>
        /// The context in which the interaction was created
        /// </summary>
        public InteractionContext Context { get; internal set; }
    }
}
