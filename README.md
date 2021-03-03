# DSharpPlusSlashCommands
An unofficial slash commands implementation for [DSharpPlus](https://github.com/DSharpPlus/DSharpPlus)

For any help/discussion, join my [discord server](https://discord.gg/2ZhXXVJYhU)

# Installing
This hasn't been pushed to nuget, so you'll have to build it yourself. A basic guide:
* Clone the repository
* If using visual studio, do `Build -> Build Solution` or just F6 (if using something else/command line, google it)
* DLLs should be created in the `bin` folders
* In your bot project, in the solution explorer, right click on dependencies -> Add Project Reference, and add the DLLs for whatever modules of D#+ you use

You CANNOT use this library in conjunction with the nightlies or the release candidate, or any official build for that matter. You must use the dlls you get when you build this project, since there are some modifications to the main project as well. I will make sure to keep this repository up to date with the official one so you don't miss out on any other features.

If you need any help with this, the discord server is always open.

# Documentation
The slash command API is still in the beta stages. As such, they won't be officially implemented into D#+ until they're stable enough. You can use this library to somewhat get an idea of how you can implement slash commands into your bot.

I have done my best to make this as similar to CommandsNext as possible to make it a smooth experience. However, there are several limitations, that are either because of the unfinished and limited features of the slash command api itself, or for simplicity. This implementation does NOT support:
* Registering or editing commands at runtime
* Using dependency injection or module lifespans
* Registering multiple command classes
* Sharding
* Any pre execution checks

You should ideally not use this library in production, especially for large and already established bots. I highly recommend only using this to check out what slash commands can do right now, and do some basic testing, and to wait until slash commands are fully released for any actual features.

Now, on to the actual guide:
## Authorizing your bot
For a bot to make slash commands in a server, it must be authorized with the `applications.commands` scope as well. In the OAuth2 section of the developer portal, you can check the `applications.commands` box to generate an invite link. You can check the `bot` box as well to generate a link that authorizes both. If a bot is already authorized with the bot scope, you can still authorize with just the `applications.commands` scope without having to kick out the bot.

Some advice: it is likely that slash commands will become a staple part of every bot's features eventually, so it might be better to update your bot's default invite link (the one you tell users to use) to include the `applications.commands` scope, as future proofing. Discord will soon do a scope migration that adds the `applications.commands` scope to every server that's authorized with `bot` as well, so you don't have to worry about old guilds not being able to use slash commands without adding the bot again.
## Setup
Add the using reference to your bot class (you must have referenced the dll beforehand):

```cs
using DSharpPlus.SlashCommands;
```

You can then register a `SlashCommandsExtension` on your `DiscordClient`, similar to how you register a `CommandsNextExtension`

```cs
var slash = discord.UseSlashCommands();
```

## Making a command class
Similar to CommandsNext, you can make a module for slash commands and make it inherit from `SlashCommandModule`
```cs
public class SlashCommands : SlashCommandModule
{
  //commands
}
```
You have to then register it with your `SlashCommandsExtension`.

Slash commands can be registered either globally or for a certain guild. However, if you try to register them globally, they can take up to an hour to cache across all guilds. So, it is recommended that you only register them for a certain guild for testing, and only register them globally once they're ready to be used.

To register your command class,
```cs
//To register them for a single server, recommended for testing
slash.RegisterCommands<SlashCommands>(guild_id);

//To register them globally, once you're confident that they're ready to be used by everyone
slash.RegisterCommands<SlashCommands>();
```
*Make sure that you register them before your `ConnectAsync`*

Note: you can only register ONE class for a single guild, and only one class globally as well. Trying to register more than one will overwrite the previous. However, if you find some use for it, you can register a different class for different guilds.

# Making Commands!
On to the exciting part. 
TODO

# Questions?
Join my [discord server](https://discord.gg/2ZhXXVJYhU)!
