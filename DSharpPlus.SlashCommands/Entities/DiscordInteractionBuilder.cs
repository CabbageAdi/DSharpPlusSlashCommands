using DSharpPlus.Entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DSharpPlus.SlashCommands.Entities
{
    /// <summary>
    /// Constructs an interaction response
    /// </summary>
    public class DiscordInteractionBuilder
    {
        /// <summary>
        /// Gets or sets whether this response should be TTS
        /// </summary>
        [JsonProperty("tts", NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsTTS { get; set; }

        /// <summary>
        /// Gets or sets the content of this interaction response
        /// </summary>
        [JsonProperty("content", NullValueHandling = NullValueHandling.Ignore)]
        public string Content { get; set; }

        /// <summary>
        /// Gets the embeds in this interaction response
        /// </summary>
        [JsonProperty("embeds", NullValueHandling = NullValueHandling.Ignore)]
        public IReadOnlyList<DiscordEmbed> Embeds { get; internal set; } = new List<DiscordEmbed>();

        /// <summary>
        /// Gets or sets the mentions to send with this interaction response
        /// </summary>
        [JsonProperty("mentions", NullValueHandling = NullValueHandling.Ignore)]
        public IEnumerable<IMention> Mentions { get; set; } = new List<IMention>();
        
        /// <summary>
        /// Gets or sets the flags with this interaction response
        /// </summary>
        [JsonProperty("flags", NullValueHandling = NullValueHandling.Ignore)]
        public InteractionFlags? Flags { get; set; }

        /// <summary>
        /// Adds an embed to the interaction response
        /// </summary>
        /// <param name="embed">The embed to add to the interaction response</param>
        /// <returns>The modified interaction builder</returns>
        public DiscordInteractionBuilder WithEmbed(DiscordEmbed embed)
        {
            var list = Embeds.ToList();
            list.Add(embed);
            Embeds = list;
            return this;
        }

        /// <summary>
        /// Adds embeds to the interaction response
        /// </summary>
        /// <param name="embeds">The embeds to add to this interaction response</param>
        /// <returns>The modified interaction builder</returns>
        public DiscordInteractionBuilder WithEmbeds(IEnumerable<DiscordEmbed> embeds)
        {
            var list = Embeds.ToList();
            list.AddRange(embeds);
            Embeds = list;
            return this;
        }

        /// <summary>
        /// Sets the content of this interaction response
        /// </summary>
        /// <param name="content">The content of this interaction response</param>
        /// <returns>The modified interaction builder</returns>
        public DiscordInteractionBuilder WithContent(string content)
        {
            Content = content;
            return this;
        }

        /// <summary>
        /// Sets whether this interaction response should be TTS
        /// </summary>
        /// <param name="tts">If the message should be TTS</param>
        /// <returns>The modified interaction builder</returns>
        public DiscordInteractionBuilder WithTTS(bool tts)
        {
            IsTTS = tts;
            return this;
        }

        /// <summary>
        /// Adds a mention to this interaction response
        /// </summary>
        /// <param name="mention">The mention to be added</param>
        /// <returns>The modified interaction builder</returns>
        public DiscordInteractionBuilder WithMention(IMention mention)
        {
            var list = Mentions.ToList();
            list.Add(mention);
            Mentions = list;
            return this;
        }

        /// <summary>
        /// Adds mentions to this interaction response
        /// </summary>
        /// <param name="mentions">The mentions to be added</param>
        /// <returns>The modified interaction builder</returns>
        public DiscordInteractionBuilder WithMentions(IEnumerable<IMention> mentions)
        {
            var list = Mentions.ToList();
            list.AddRange(mentions);
            Mentions = list;
            return this;
        }

        /// <summary>
        /// Sets the flags of this interaction response
        /// </summary>
        /// <param name="flags">The flags of this interaction response</param>
        /// <returns>The modified interaction builder</returns>
        public DiscordInteractionBuilder WithFlags(InteractionFlags flags)
        {
            Flags = flags;
            return this;
        }
    }
}
