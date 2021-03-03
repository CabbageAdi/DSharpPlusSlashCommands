using System;
using System.Collections.Generic;
using System.Text;

namespace DSharpPlus.SlashCommands
{
    /// <summary>
    /// Represents the flags in an interaction response
    /// </summary>
    public enum InteractionFlags
    {
        /// <summary>
        /// Sets the message to only be visible to the user which created the interaction
        /// </summary>
        Ephemeral = 64
    }
}
