using System;
using System.Collections.Generic;
using System.Text;

namespace DSharpPlus.SlashCommands.EventArgs
{
    /// <summary>
    /// Represents arguments for a <see cref="SlashCommandsExtension.SlashCommandErrored"/> event
    /// </summary>
    public class SlashCommandErrorEventArgs : SlashCommandEventArgs
    {
        /// <summary>
        /// The exception thrown
        /// </summary>
        public Exception Exception { get; internal set; }
    }
}
