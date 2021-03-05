using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.VoiceNext;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ProBot.Commands;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ProBot
{
    public class ProBot
    {
        public readonly EventId BotEventId = new EventId(42, "Bot-Ex04");
        public DiscordClient Client { get; private set; }
        public CommandsNextExtension Commands { get; private set; }
        public InteractivityConfiguration Interactivity { get; set; }
        public VoiceNextExtension Voice { get; set; }

        public async Task RunAsync()
        {
            string json;

            // Read the config.json file
            await using (var fs = File.OpenRead("config.json"))
            using (var sr = new StreamReader(fs, new UTF8Encoding(false)))
                json = await sr.ReadToEndAsync().ConfigureAwait(false);

            // Deserialize
            var configJson = JsonConvert.DeserializeObject<ConfigJson>(json);

            // Pass in the token to the config
            var config = new DiscordConfiguration
            {
                Token = configJson.Token,
                TokenType = TokenType.Bot,
                AutoReconnect = true,
                MinimumLogLevel = LogLevel.Debug
                //UseInternalLogHandler = true
            };

            // Create the client with the config
            Client = new DiscordClient(config);

            // Add event handlers
            Client.Ready += OnClientReady;
            Client.GuildAvailable += Client_GuildAvailable;
            Client.ClientErrored += Client_ClientError;

            // Create an instance of interactivity extension 
            Client.UseInteractivity(new InteractivityConfiguration
            {
                Timeout = TimeSpan.FromMinutes(2)
            });

            // Create the commandNext config
            var commandConfigs = new CommandsNextConfiguration
            {
                StringPrefixes = new[] { configJson.Prefix },
                EnableDms = false,
                EnableMentionPrefix = true,
                DmHelp = true

            };

            // Configure command next module
            Commands = Client.UseCommandsNext(commandConfigs);

            Commands.CommandExecuted += Commands_CommandExecuted;
            Commands.CommandErrored += Commands_CommandThrewError;

            // Register commands
            Commands.RegisterCommands<UsefulCommands>();
            Commands.RegisterCommands<VoiceCommands>();

            // Enable voice
            Voice = Client.UseVoiceNext();

            // Connect
            await Client.ConnectAsync();
            await Task.Delay(-1);
        }

        private Task OnClientReady(DiscordClient sender, ReadyEventArgs e)
        {
            // Log the fact that this event occurred
            sender.Logger.LogInformation(BotEventId, "Client is ready to process events.");

            return Task.CompletedTask;
        }

        private Task Client_GuildAvailable(DiscordClient sender, GuildCreateEventArgs e)
        {
            // Log the name of the server that was just
            // sent to our client
            sender.Logger.LogInformation(BotEventId, $"Guild available: {e.Guild.Name}");

            // Since this method is not async, it shall return
            // a completed task, so that no more additional work
            // is done
            return Task.CompletedTask;
        }

        private Task Commands_CommandExecuted(CommandsNextExtension sender, CommandExecutionEventArgs e)
        {
            // Log the name of the command and user
            e.Context.Client.Logger.LogInformation(BotEventId, $"{e.Context.User.Username} successfully executed '{e.Command.QualifiedName}'");

            // Since this method is not async, it shall return
            // a completed task, so that no more additional work
            // is done
            return Task.CompletedTask;
        }

        private async Task Commands_CommandThrewError(CommandsNextExtension sender, CommandErrorEventArgs e)
        {
            // Log the error details
            e.Context.Client.Logger.LogError(BotEventId,
                $"{e.Context.User.Username} tried executing '{e.Command?.QualifiedName ?? "<unknown command>"}' but it threw error: {e.Exception.GetType()}: {e.Exception.Message}",
                DateTime.Now);

            // Check if the error is a due to lack
            // of required permissions
            if (e.Exception is ChecksFailedException)
            {
                // If yes, the user doesn't have the required permissions, 
                // Notify
                var emojis = DiscordEmoji.FromName(e.Context.Client, ":no_entry:");

                // Wrap the response into an embed
                var embed = new DiscordEmbedBuilder
                {
                    Title = "Access denied",
                    Description = $"{emojis} You do not have the permissions required to execute this command.",
                    Color = new DiscordColor(0xFF0000) // Red
                };
                await e.Context.RespondAsync(embed);
            }
        }

        private Task Client_ClientError(DiscordClient sender, ClientErrorEventArgs e)
        {
            // Log the details of the error that just 
            // occurred in our client
            sender.Logger.LogError(BotEventId, e.Exception, "Exception occurred");

            // Since this method is not async, it shall return
            // a completed task, so that no more additional work
            // is done
            return Task.CompletedTask;
        }
    }
}

