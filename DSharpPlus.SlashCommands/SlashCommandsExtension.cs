using System;
using System.Reflection;
using System.Threading.Tasks;
using DSharpPlus.SlashCommands.Attributes;
using Newtonsoft.Json;
using DSharpPlus.SlashCommands.Entities;
using System.Collections.Generic;
using DSharpPlus.Entities;
using System.Linq;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;
using Emzi0767.Utilities;
using DSharpPlus.SlashCommands.EventArgs;
using DSharpPlus.Net;
using DSharpPlus.Net.Serialization;
using DSharpPlus.SlashCommands.Exceptions;

namespace DSharpPlus.SlashCommands
{
    /// <summary>
    /// A class that handles slash commands for a client
    /// </summary>
    public class SlashCommandsExtension : BaseExtension
    {
        internal List<CommandMethod> CommandMethods { get; set; } = new List<CommandMethod>();
        internal List<GroupCommand> GroupCommands { get; set; } = new List<GroupCommand>();
        internal List<SubGroupCommand> SubGroupCommands { get; set; } = new List<SubGroupCommand>();

        internal SlashCommandsExtension() { }

        /// <summary></summary>
        protected internal override void Setup(DiscordClient client)
        {
            if (this.Client != null)
                throw new InvalidOperationException("What did I tell you?");

            this.Client = client;

            _error = new AsyncEvent<SlashCommandsExtension, SlashCommandErrorEventArgs>("SLASHCOMMAND_ERRORED", TimeSpan.Zero, Client.EventErrorHandler);
            _executed = new AsyncEvent<SlashCommandsExtension, SlashCommandExecutedEventArgs>("SLASHCOMMAND_EXECUTED", TimeSpan.Zero, Client.EventErrorHandler);

            Client.InteractionCreated += InteractionHandler;
        }

        //Register

        /// <summary>
        /// Registers a command class
        /// </summary>
        /// <typeparam name="T">The command class to register</typeparam>
        public void RegisterCommands<T>(ulong? guildid = null) where T : SlashCommandModule
        {
            RegisterCommands(typeof(T), guildid);
        }

        private void RegisterCommands(Type t, ulong? guildid)
        {
            CommandMethod[] InternalCommandMethods = Array.Empty<CommandMethod>();
            GroupCommand[] InternalGroupCommands = Array.Empty<GroupCommand>();
            SubGroupCommand[] InternalSubGroupCommands = Array.Empty<SubGroupCommand>();

            Client.Ready += (s, e) =>
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        List<CommandCreatePayload> ToUpdate = new List<CommandCreatePayload>();

                        var ti = t.GetTypeInfo();

                        var classes = ti.DeclaredNestedTypes;
                        foreach (var tti in classes.Where(x => x.GetCustomAttribute<SlashCommandGroupAttribute>() != null))
                        {
                            var groupatt = tti.GetCustomAttribute<SlashCommandGroupAttribute>();
                            var submethods = tti.DeclaredMethods;
                            var subclasses = tti.DeclaredNestedTypes;
                            if (subclasses.Any(x => x.GetCustomAttribute<SlashCommandGroupAttribute>() != null) && submethods.Any(x => x.GetCustomAttribute<SlashCommandAttribute>() != null))
                            {
                                throw new ArgumentException("Slash command groups cannot have both subcommands and subgroups!");
                            }
                            var payload = new CommandCreatePayload
                            {
                                Name = groupatt.Name,
                                Description = groupatt.Description
                            };
                            var commandmethods = new Dictionary<string, MethodInfo>();
                            foreach (var submethod in submethods.Where(x => x.GetCustomAttribute<SlashCommandAttribute>() != null))
                            {
                                var commandattribute = submethod.GetCustomAttribute<SlashCommandAttribute>();
                                var subpayload = new DiscordApplicationCommandOption(commandattribute.Name, commandattribute.Description);

                                var parameters = submethod.GetParameters();
                                if (!ReferenceEquals(parameters.First().ParameterType, typeof(InteractionContext)))
                                    throw new ArgumentException($"The first argument must be an InteractionContext!");
                                parameters = parameters.Skip(1).ToArray();
                                foreach (var parameter in parameters)
                                {
                                    var optionattribute = parameter.GetCustomAttribute<OptionAttribute>();
                                    if (optionattribute == null)
                                        throw new ArgumentException("Arguments must have the Option attribute!");

                                    var type = parameter.ParameterType;
                                    ApplicationCommandOptionType parametertype;
                                    if (ReferenceEquals(type, typeof(string)))
                                        parametertype = ApplicationCommandOptionType.String;
                                    else if (ReferenceEquals(type, typeof(long)))
                                        parametertype = ApplicationCommandOptionType.Integer;
                                    else if (ReferenceEquals(type, typeof(bool)))
                                        parametertype = ApplicationCommandOptionType.Boolean;
                                    else if (ReferenceEquals(type, typeof(DiscordChannel)))
                                        parametertype = ApplicationCommandOptionType.Channel;
                                    else if (ReferenceEquals(type, typeof(DiscordUser)))
                                        parametertype = ApplicationCommandOptionType.User;
                                    else if (ReferenceEquals(type, typeof(DiscordRole)))
                                        parametertype = ApplicationCommandOptionType.Role;
                                    else
                                        throw new ArgumentException("Cannot convert type! Argument types must be string, long, bool, DiscordChannel, DiscordUser or DiscordRole.");

                                    DiscordApplicationCommandOptionChoice[] choices = null;
                                    var choiceattributes = parameter.GetCustomAttributes<ChoiceAttribute>();
                                    if (choiceattributes.Any())
                                    {
                                        choices = Array.Empty<DiscordApplicationCommandOptionChoice>();
                                        foreach (var att in choiceattributes)
                                        {
                                            choices = choices.Append(new DiscordApplicationCommandOptionChoice(att.Name, att.Value)).ToArray();
                                        }
                                    }

                                    var list = subpayload.Options?.ToList() ?? new List<DiscordApplicationCommandOption>();

                                    list.Add(new DiscordApplicationCommandOption(optionattribute.Name, optionattribute.Description)
                                    {
                                        Required = !parameter.IsOptional,
                                        Type = parametertype,
                                        Choices = choices
                                    });

                                    subpayload.Options = list;
                                }

                                commandmethods.Add(commandattribute.Name, submethod);

                                payload.Options.Add(new DiscordApplicationCommandOption(commandattribute.Name, commandattribute.Description)
                                {
                                    Required = null,
                                    Options = subpayload.Options,
                                    Type = ApplicationCommandOptionType.SubCommand
                                });

                                InternalGroupCommands = InternalGroupCommands.Append(new GroupCommand { Name = groupatt.Name, ParentClass = tti, Methods = commandmethods }).ToArray();
                            }
                            foreach (var subclass in subclasses.Where(x => x.GetCustomAttribute<SlashCommandGroupAttribute>() != null))
                            {
                                var subgroupatt = subclass.GetCustomAttribute<SlashCommandGroupAttribute>();
                                var subsubmethods = subclass.DeclaredMethods.Where(x => x.GetCustomAttribute<SlashCommandAttribute>() != null);

                                var command = new SubGroupCommand { Name = groupatt.Name };

                                var subpayload = new DiscordApplicationCommandOption(subgroupatt.Name, subgroupatt.Description)
                                {
                                    Required = null,
                                    Type = ApplicationCommandOptionType.SubCommandGroup,
                                    Options = new List<DiscordApplicationCommandOption>()
                                };

                                foreach (var subsubmethod in subsubmethods)
                                {
                                    var commatt = subsubmethod.GetCustomAttribute<SlashCommandAttribute>();
                                    var subsubpayload = new DiscordApplicationCommandOption(commatt.Name, commatt.Description)
                                    {
                                        Type = ApplicationCommandOptionType.SubCommand,
                                    };
                                    var parameters = subsubmethod.GetParameters();
                                    if (!ReferenceEquals(parameters.First().ParameterType, typeof(InteractionContext)))
                                        throw new ArgumentException($"The first argument must be an InteractionContext!");
                                    parameters = parameters.Skip(1).ToArray();
                                    foreach (var parameter in parameters)
                                    {
                                        var optionattribute = parameter.GetCustomAttribute<OptionAttribute>();
                                        if (optionattribute == null)
                                            throw new ArgumentException("Arguments must have the Option attribute!");

                                        var type = parameter.ParameterType;
                                        ApplicationCommandOptionType parametertype;
                                        if (ReferenceEquals(type, typeof(string)))
                                            parametertype = ApplicationCommandOptionType.String;
                                        else if (ReferenceEquals(type, typeof(long)))
                                            parametertype = ApplicationCommandOptionType.Integer;
                                        else if (ReferenceEquals(type, typeof(bool)))
                                            parametertype = ApplicationCommandOptionType.Boolean;
                                        else if (ReferenceEquals(type, typeof(DiscordChannel)))
                                            parametertype = ApplicationCommandOptionType.Channel;
                                        else if (ReferenceEquals(type, typeof(DiscordUser)))
                                            parametertype = ApplicationCommandOptionType.User;
                                        else if (ReferenceEquals(type, typeof(DiscordRole)))
                                            parametertype = ApplicationCommandOptionType.Role;
                                        else
                                            throw new ArgumentException("Cannot convert type! Argument types must be string, long, bool, DiscordChannel, DiscordUser or DiscordRole.");

                                        DiscordApplicationCommandOptionChoice[] choices = null;
                                        var choiceattributes = parameter.GetCustomAttributes<ChoiceAttribute>();
                                        if (choiceattributes.Any())
                                        {
                                            choices = Array.Empty<DiscordApplicationCommandOptionChoice>();
                                            foreach (var att in choiceattributes)
                                            {
                                                choices = choices.Append(new DiscordApplicationCommandOptionChoice(att.Name, att.Value)).ToArray();
                                            }
                                        }

                                        var list = subsubpayload.Options?.ToList() ?? new List<DiscordApplicationCommandOption>();

                                        list.Add(new DiscordApplicationCommandOption(optionattribute.Name, optionattribute.Description)
                                        {
                                            Required = !parameter.IsOptional,
                                            Type = parametertype,
                                            Choices = choices
                                        });

                                        subsubpayload.Options = list;
                                    }
                                    subpayload.Options = subpayload.Options.ToArray().Append(subsubpayload).ToList();
                                    commandmethods.Add(commatt.Name, subsubmethod);
                                }
                                command.SubCommands.Add(new GroupCommand { Name = subgroupatt.Name, ParentClass = subclass, Methods = commandmethods });
                                InternalSubGroupCommands = InternalSubGroupCommands.Append(command).ToArray();
                                payload.Options.Add(subpayload);
                            }
                            ToUpdate.Add(payload);
                        }

                        var methods = ti.DeclaredMethods;
                        foreach (var method in methods.Where(x => x.GetCustomAttribute<SlashCommandAttribute>() != null))
                        {
                            var commandattribute = method.GetCustomAttribute<SlashCommandAttribute>();
                            var payload = new CommandCreatePayload
                            {
                                Name = commandattribute.Name,
                                Description = commandattribute.Description
                            };

                            var parameters = method.GetParameters();
                            if (!ReferenceEquals(parameters.First().ParameterType, typeof(InteractionContext)))
                                throw new ArgumentException($"The first argument must be an InteractionContext!");
                            parameters = parameters.Skip(1).ToArray();
                            foreach (var parameter in parameters)
                            {
                                var optionattribute = parameter.GetCustomAttribute<OptionAttribute>();
                                if (optionattribute == null)
                                    throw new ArgumentException("Arguments must have the SlashOption attribute!");

                                var type = parameter.ParameterType;
                                ApplicationCommandOptionType parametertype;
                                if (ReferenceEquals(type, typeof(string)))
                                    parametertype = ApplicationCommandOptionType.String;
                                else if (ReferenceEquals(type, typeof(long)))
                                    parametertype = ApplicationCommandOptionType.Integer;
                                else if (ReferenceEquals(type, typeof(bool)))
                                    parametertype = ApplicationCommandOptionType.Boolean;
                                else if (ReferenceEquals(type, typeof(DiscordChannel)))
                                    parametertype = ApplicationCommandOptionType.Channel;
                                else if (ReferenceEquals(type, typeof(DiscordUser)))
                                    parametertype = ApplicationCommandOptionType.User;
                                else if (ReferenceEquals(type, typeof(DiscordRole)))
                                    parametertype = ApplicationCommandOptionType.Role;
                                else
                                    throw new ArgumentException($"Cannot convert type! Argument types must be string, long, bool, DiscordChannel, DiscordUser or DiscordRole.");

                                DiscordApplicationCommandOptionChoice[] choices = null;
                                var choiceattributes = parameter.GetCustomAttributes<ChoiceAttribute>();
                                if (choiceattributes.Any())
                                {
                                    choices = Array.Empty<DiscordApplicationCommandOptionChoice>();
                                    foreach (var att in choiceattributes)
                                    {
                                        choices = choices.Append(new DiscordApplicationCommandOptionChoice(att.Name, att.Value)).ToArray();
                                    }
                                }

                                payload.Options.Add(new DiscordApplicationCommandOption(optionattribute.Name, optionattribute.Description)
                                {
                                    Required = !parameter.IsOptional,
                                    Type = parametertype,
                                    Choices = choices
                                });
                            }
                            InternalCommandMethods = InternalCommandMethods.Append(new CommandMethod { Method = method, Name = commandattribute.Name, ParentClass = t }).ToArray();
                            ToUpdate.Add(payload);
                        }

                        var commands = await BulkCreateCommandsAsync(ToUpdate, guildid);
                        foreach(var command in commands)
                        {
                            if (InternalCommandMethods.Any(x => x.Name == command.Name))
                                InternalCommandMethods.First(x => x.Name == command.Name).Id = command.Id;

                            else if (InternalGroupCommands.Any(x => x.Name == command.Name))
                                InternalGroupCommands.First(x => x.Name == command.Name).Id = command.Id;

                            else if (InternalSubGroupCommands.Any(x => x.Name == command.Name))
                                InternalSubGroupCommands.First(x => x.Name == command.Name).Id = command.Id;
                        }
                        CommandMethods.AddRange(InternalCommandMethods);
                        GroupCommands.AddRange(InternalGroupCommands);
                        SubGroupCommands.AddRange(InternalSubGroupCommands);
                    }
                    catch (Exception ex)
                    {
                        Client.Logger.LogError(ex, $"There was an error registering slash commands");
                        Environment.Exit(-1);
                    }
                });

                return Task.CompletedTask;
            };
        }

        //Handler
        internal Task InteractionHandler(DiscordClient client, InteractionCreateEventArgs e)
        {
            _ = Task.Run(async () =>
            {
                InteractionContext context = new InteractionContext
                {
                    Interaction = e.Interaction,
                    Channel = e.Interaction.Channel,
                    Guild = e.Interaction.Guild,
                    Member = e.Interaction.Member,
                    Client = client,
                    ScommExtension = this,
                    CommandName = e.Interaction.Data.Name,
                    InteractionId = e.Interaction.Id,
                    Token = e.Interaction.Token
                };

                try
                {
                    var methods = CommandMethods.Where(x => x.Id == e.Interaction.Data.Id);
                    var groups = GroupCommands.Where(x => x.Id == e.Interaction.Data.Id);
                    var subgroups = SubGroupCommands.Where(x => x.Id == e.Interaction.Data.Id);
                    if (!methods.Any() && !groups.Any() && !subgroups.Any())
                        throw new SlashCommandNotFoundException("An interaction was created, but no command was registered for it");
                    if (methods.Any())
                    {
                        var method = methods.First();

                        List<object> args = new List<object> { context };
                        var parameters = method.Method.GetParameters().Skip(1);

                        for (int i = 0; i < parameters.Count(); i++)
                        {
                            var parameter = parameters.ElementAt(i);
                            if (parameter.IsOptional && (e.Interaction.Data.Options == null || e.Interaction.Data.Options?.ElementAtOrDefault(i) == default))
                                args.Add(parameter.DefaultValue);
                            else
                            {
                                var option = e.Interaction.Data.Options.ElementAt(i);

                                if (ReferenceEquals(parameter.ParameterType, typeof(string)))
                                    args.Add(option.Value.ToString());
                                else if (ReferenceEquals(parameter.ParameterType, typeof(long)))
                                    args.Add((long)option.Value);
                                else if (ReferenceEquals(parameter.ParameterType, typeof(bool)))
                                    args.Add((bool)option.Value);
                                else if (ReferenceEquals(parameter.ParameterType, typeof(DiscordUser)))
                                    args.Add(await Client.GetUserAsync((ulong)option.Value));
                                else if (ReferenceEquals(parameter.ParameterType, typeof(DiscordChannel)))
                                    args.Add(e.Interaction.Guild.GetChannel((ulong)option.Value));
                                else if (ReferenceEquals(parameter.ParameterType, typeof(DiscordRole)))
                                    args.Add(e.Interaction.Guild.GetRole((ulong)option.Value));
                                else
                                    throw new ArgumentException($"How on earth did that happen");
                            }
                        }
                        var classinstance = Activator.CreateInstance(method.ParentClass);
                        var task = (Task)method.Method.Invoke(classinstance, args.ToArray());
                        await task;
                    }
                    else if(groups.Any())
                    {
                        var command = e.Interaction.Data.Options.First();
                        var method = groups.First().Methods.First(x => x.Key == command.Name).Value;

                        List<object> args = new List<object> { context };
                        var parameters = method.GetParameters().Skip(1);

                        for (int i = 0; i < parameters.Count(); i++)
                        {
                            var parameter = parameters.ElementAt(i);
                            if (parameter.IsOptional && (command.Options == null || command.Options?.ElementAtOrDefault(i) == default))
                                args.Add(parameter.DefaultValue);
                            else
                            {
                                var option = command.Options.ElementAt(i);

                                if (ReferenceEquals(parameter.ParameterType, typeof(string)))
                                    args.Add(option.Value.ToString());
                                else if (ReferenceEquals(parameter.ParameterType, typeof(long)))
                                    args.Add((long)option.Value);
                                else if (ReferenceEquals(parameter.ParameterType, typeof(bool)))
                                    args.Add((bool)option.Value);
                                else if (ReferenceEquals(parameter.ParameterType, typeof(DiscordUser)))
                                    args.Add(await Client.GetUserAsync((ulong)option.Value));
                                else if (ReferenceEquals(parameter.ParameterType, typeof(DiscordChannel)))
                                    args.Add(e.Interaction.Guild.GetChannel((ulong)option.Value));
                                else if (ReferenceEquals(parameter.ParameterType, typeof(DiscordRole)))
                                    args.Add(e.Interaction.Guild.GetRole((ulong)option.Value));
                                else
                                    throw new ArgumentException($"How on earth did that happen");
                            }
                        }
                        var classinstance = Activator.CreateInstance(groups.First().ParentClass);
                        var task = (Task)method.Invoke(classinstance, args.ToArray());
                        await task;
                    }
                    else if (subgroups.Any())
                    {
                        var command = e.Interaction.Data.Options.First();
                        var group = subgroups.First(x => x.SubCommands.Any(y => y.Name == command.Name)).SubCommands.First(x => x.Name == command.Name);

                        var method = group.Methods.First(x => x.Key == command.Options.First().Name).Value;

                        List<object> args = new List<object> { context };
                        var parameters = method.GetParameters().Skip(1);

                        for (int i = 0; i < parameters.Count(); i++)
                        {
                            var parameter = parameters.ElementAt(i);
                            if (parameter.IsOptional && (command.Options == null || command.Options?.ElementAtOrDefault(i) == default))
                                args.Add(parameter.DefaultValue);
                            else
                            {
                                var option = command.Options.ElementAt(i);

                                if (ReferenceEquals(parameter.ParameterType, typeof(string)))
                                    args.Add(option.Value.ToString());
                                else if (ReferenceEquals(parameter.ParameterType, typeof(long)))
                                    args.Add((long)option.Value);
                                else if (ReferenceEquals(parameter.ParameterType, typeof(bool)))
                                    args.Add((bool)option.Value);
                                else if (ReferenceEquals(parameter.ParameterType, typeof(DiscordUser)))
                                    args.Add(await Client.GetUserAsync((ulong)option.Value));
                                else if (ReferenceEquals(parameter.ParameterType, typeof(DiscordChannel)))
                                    args.Add(e.Interaction.Guild.GetChannel((ulong)option.Value));
                                else if (ReferenceEquals(parameter.ParameterType, typeof(DiscordRole)))
                                    args.Add(e.Interaction.Guild.GetRole((ulong)option.Value));
                                else
                                    throw new ArgumentException($"How on earth did that happen");
                            }
                        }
                        var classinstance = Activator.CreateInstance(group.ParentClass);
                        var task = (Task)method.Invoke(classinstance, args.ToArray());
                        await task;
                    }

                    await _executed.InvokeAsync(this, new SlashCommandExecutedEventArgs { Context = context });
                }
                catch (Exception ex)
                {
                    await _error.InvokeAsync(this, new SlashCommandErrorEventArgs { Context = context, Exception = ex });
                }
            });
            return Task.CompletedTask;
        }

        //REST methods
        internal async Task<DiscordApplicationCommand> CreateCommandAsync(CommandCreatePayload pld, ulong? guildid)
        {
            string route;
            if(guildid == null)
                route = $"/applications/{Client.CurrentApplication.Id}/commands";
            else
                route = $"/applications/{Client.CurrentApplication.Id}/guilds/{guildid}/commands";              

            var bucket = Client.ApiClient.Rest.GetBucket(RestRequestMethod.POST, route, new { }, out var path);

            var url = Utilities.GetApiUriFor(path);
            var res = await Client.ApiClient.DoRequestAsync(Client, bucket, url, RestRequestMethod.POST, route, payload: DiscordJson.SerializeObject(pld));

            var ret = JsonConvert.DeserializeObject<DiscordApplicationCommand>(res.Response);
            ret.Discord = Client;

            return ret;
        }

        internal async Task<IReadOnlyList<DiscordApplicationCommand>> BulkCreateCommandsAsync(List<CommandCreatePayload> pld, ulong? guildid = null)
        {
            string route;
            if (guildid == null)
                route = $"/applications/{Client.CurrentApplication.Id}/commands";
            else
                route = $"/applications/{Client.CurrentApplication.Id}/guilds/{guildid}/commands";

            var bucket = Client.ApiClient.Rest.GetBucket(RestRequestMethod.PUT, route, new { }, out var path);

            var url = Utilities.GetApiUriFor(path);
            var res = await Client.ApiClient.DoRequestAsync(Client, bucket, url, RestRequestMethod.PUT, route, payload: DiscordJson.SerializeObject(pld));

            var ret = JsonConvert.DeserializeObject<List<DiscordApplicationCommand>>(res.Response);
            foreach (var app in ret)
                app.Discord = Client;

            return ret;
        }

        /// <summary>
        /// Gets a list of all slash commands associated with this extension
        /// </summary>
        /// <param name="guildid">The guild to get commands for, leave as null to get global commands</param>
        /// <returns>A list of DiscordApplicationCommand</returns>
        public async Task<IReadOnlyList<DiscordApplicationCommand>> GetAllCommandsAsync(ulong? guildid = null)
        {
            string route;
            if (guildid == null)
               route = $"/applications/{Client.CurrentApplication.Id}/commands";
            else
               route = $"/applications/{Client.CurrentApplication.Id}/guilds/{guildid}/commands";

            var bucket = Client.ApiClient.Rest.GetBucket(RestRequestMethod.GET, route, new { }, out var path);

            var url = Utilities.GetApiUriFor(path);
            var res = await Client.ApiClient.DoRequestAsync(Client, bucket, url, RestRequestMethod.GET, route);

            return JsonConvert.DeserializeObject<IReadOnlyList<DiscordApplicationCommand>>(res.Response);
        }

        /// <summary>
        /// Gets a slash command
        /// </summary>
        /// <param name="commandId">The id of the command to get</param>
        /// <param name="guildid">The guild the command is in, leave as null if it's a global command</param>
        /// <returns>The command requested</returns>
        public async Task<DiscordApplicationCommand> GetCommandAsync(ulong commandId, ulong? guildid = null)
        {
            string route;
            if (guildid == null)
                route = $"/applications/{Client.CurrentApplication.Id}/commands/{commandId}";
            else
                route = $"/applications/{Client.CurrentApplication.Id}/guilds/{guildid}/commands/{commandId}";

            var bucket = Client.ApiClient.Rest.GetBucket(RestRequestMethod.GET, route, new { }, out var path);

            var url = Utilities.GetApiUriFor(path);
            var res = await Client.ApiClient.DoRequestAsync(Client, bucket, url, RestRequestMethod.GET, route);

            return JsonConvert.DeserializeObject<DiscordApplicationCommand>(res.Response);
        }

        /// <summary>
        /// Deleted a slash command
        /// </summary>
        /// <param name="commandId">The id of the command to delete</param>
        /// <param name="guildid">The guild the command is in, leave as null if it's a global command</param>
        /// <returns></returns>
        public async Task DeleteCommandAsync(ulong commandId, ulong? guildid = null)
        {
            string route;
            if (guildid == null)
                route = $"/applications/{Client.CurrentApplication.Id}/commands/{commandId}";
            else
                route = $"/applications/{Client.CurrentApplication.Id}/guilds/{guildid}/commands/{commandId}";

            var bucket = Client.ApiClient.Rest.GetBucket(RestRequestMethod.DELETE, route, new { }, out var path);

            var url = Utilities.GetApiUriFor(path);
            await Client.ApiClient.DoRequestAsync(Client, bucket, url, RestRequestMethod.DELETE, route);
        }
        /// <summary>
        /// Deleted a slash command
        /// </summary>
        /// <param name="command">The command to delete</param>
        /// <returns></returns>
        public Task DeleteCommandAsync(DiscordApplicationCommand command)
            => DeleteCommandAsync(command.Id);

        //Respond methods

        /// <summary>
        /// Creates a response to an interaction
        /// </summary>
        /// <param name="interactionId">The id of the interaction</param>
        /// <param name="token">The token of the interaction</param>
        /// <param name="type">The type to respond with</param>
        /// <param name="builder">The data, if any, to send</param>
        /// <returns></returns>
        public async Task CreateInteractionResponseAsync(ulong interactionId, string token, DiscordInteractionResponseType type,  DiscordInteractionBuilder builder = null)
        {
            var pld = new InteractionCreatePayload
            {
                Type = type,
                Data = builder
            };

            var route = $"/interactions/{interactionId}/{token}/callback";
            var bucket = Client.ApiClient.Rest.GetBucket(RestRequestMethod.POST, route, new { }, out var path);

            var url = Utilities.GetApiUriFor(path);
            await Client.ApiClient.DoRequestAsync(Client, bucket, url, RestRequestMethod.POST, route, payload: DiscordJson.SerializeObject(pld));
        }

        /// <summary>
        /// Edits the interaction response
        /// </summary>
        /// <param name="token">The token of the interaction</param>
        /// <param name="builder">The data, if any, to edit the response with</param>
        /// <returns></returns>
        public async Task EditInteractionResponseAsync(string token, DiscordInteractionBuilder builder)
        {
            var route = $"/webhooks/{Client.CurrentApplication.Id}/{token}/messages/@original";
            var bucket = Client.ApiClient.Rest.GetBucket(RestRequestMethod.PATCH, route, new { }, out var path);

            var url = Utilities.GetApiUriFor(path);
            await Client.ApiClient.DoRequestAsync(Client, bucket, url, RestRequestMethod.PATCH, route, payload: DiscordJson.SerializeObject(builder));
        }

        /// <summary>
        /// Deletes the interaction response
        /// </summary>
        /// <param name="token">The token of the interaction</param>
        /// <returns></returns>
        public async Task DeleteInteractionResponseAsync(string token)
        {
            var route = $"/webhooks/{Client.CurrentApplication.Id}/{token}/messages/@original";
            var bucket = Client.ApiClient.Rest.GetBucket(RestRequestMethod.DELETE, route, new { }, out var path);

            var url = Utilities.GetApiUriFor(path);
            await Client.ApiClient.DoRequestAsync(Client, bucket, url, RestRequestMethod.DELETE, route);
        }

        /// <summary>
        /// Created a follow up message to the interaction
        /// </summary>
        /// <param name="token">The token of the interaction</param>
        /// <param name="webhook">The data to send</param>
        /// <returns>The returned <see cref="DiscordMessage"/></returns>
        public Task<DiscordMessage> CreateFollowupMessageAsync(string token, DiscordWebhookBuilder webhook)
            => Client.ApiClient.ExecuteWebhookAsync(Client.CurrentApplication.Id, token, webhook);

        //Events

        /// <summary>
        /// Fires whenver the execution of a slash command fails
        /// </summary>
        public event AsyncEventHandler<SlashCommandsExtension, SlashCommandErrorEventArgs> SlashCommandErrored
        {
            add { _error.Register(value); }
            remove { _error.Unregister(value); }
        }
        private AsyncEvent<SlashCommandsExtension, SlashCommandErrorEventArgs> _error;

        /// <summary>
        /// Fires when the execution of a slash command is successful
        /// </summary>
        public event AsyncEventHandler<SlashCommandsExtension, SlashCommandExecutedEventArgs> SlashCommandExecuted
        {
            add { _executed.Register(value); }
            remove { _executed.Unregister(value); }
        }
        private AsyncEvent<SlashCommandsExtension, SlashCommandExecutedEventArgs> _executed;
    }

    internal class CommandCreatePayload
    {
        [JsonProperty("name")]
        public string Name;

        [JsonProperty("description")]
        public string Description;

        [JsonProperty("options")]
        public List<DiscordApplicationCommandOption> Options = new List<DiscordApplicationCommandOption>();
    }

    internal class InteractionCreatePayload
    {
        [JsonProperty("type")]
        public DiscordInteractionResponseType Type { get; set; }

        [JsonProperty("data")]
        public DiscordInteractionBuilder Data { get; set; }
    }

    internal class CommandMethod
    {
        public ulong Id;

        public string Name;
        public MethodInfo Method;
        public Type ParentClass;
    }

    internal class GroupCommand
    {
        public ulong Id;

        public string Name;
        public Dictionary<string, MethodInfo> Methods = null;
        public Type ParentClass;
    }

    internal class SubGroupCommand
    {
        public ulong Id;

        public string Name;
        public List<GroupCommand> SubCommands = new List<GroupCommand>();
    }
}