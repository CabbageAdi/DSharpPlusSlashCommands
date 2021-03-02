using System;
using System.Collections.Generic;
using System.Text;

namespace DSharpPlus.SlashCommands.Exceptions
{
    /// <summary>
    /// Thrown when a command cannot be found for an interaction
    /// </summary>
    public class SlashCommandNotFoundException : Exception
    {
        internal SlashCommandNotFoundException(string message) : base (message) { }
    }
}
