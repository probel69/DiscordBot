using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.VoiceNext;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace ProBot.Commands
{
    public class VoiceCommands : BaseCommandModule
    {
        // TODO : Add the url playback support
        private static void _initVNext(CommandContext ctx, out VoiceNextExtension vn, out VoiceNextConnection vnc)
        {
            vn = ctx.Client.GetVoiceNext();
            if (vn == null)
                // Not enabled
                ctx.RespondAsync("VoiceNext is not enabled or configured.");

            vnc = vn?.GetConnection(ctx.Guild);
            if (vnc == null) return;

            // Already connected
            ctx.RespondAsync("Already connected in this server.");
        }


        [Command("join"), Description("Joins a voice channel")]
        public async Task Join(CommandContext ctx, DiscordChannel channel = null)
        {
            // Check whether VoiceNext is enabled
            _initVNext(ctx, out var vNext, out _);

            // Get member's voice state
            var vStat = ctx.Member?.VoiceState;
            if (vStat?.Channel == null && channel == null)
            {
                // They did not specify a channel and are not in one
                await ctx.RespondAsync("You are not in a voice channel.");
                return;
            }

            // Channel not specified, use user's current
            if (channel == null)
                channel = vStat.Channel;

            // Connect
            await vNext.ConnectAsync(channel);
            await ctx.RespondAsync($"Connected to `{channel.Name}`").ConfigureAwait(false);
        }

        [Command("leave"), Description("Leaves a voice channel.")]
        public async Task Leave(CommandContext ctx)
        {
            // Check whether VoiceNext is enabled
            _initVNext(ctx, out _, out var vNextConnection);

            // Disconnect
            vNextConnection.Disconnect();
            await ctx.RespondAsync("Disconnected");
        }

        [Command("play"), Description("Plays an audio file.")]
        public async Task Play(CommandContext ctx, [RemainingText, Description("Full path on disk to the file to play.")] string filename)
        {
            // Check whether VoiceNext is enabled
            _initVNext(ctx, out _, out var vNextConnection);

            // Check if file exists
            if (!File.Exists(filename))
            {
                // File does not exist
                await ctx.RespondAsync($"File `{filename}` does not exist.");
                return;
            }

            // Wait for current playback to finish
            while (vNextConnection.IsPlaying)
                await vNextConnection.WaitForPlaybackFinishAsync();

            // Play
            Exception exc = null;
            await ctx.Message.RespondAsync($"Playing `{filename}`");

            try
            {
                await vNextConnection.SendSpeakingAsync();

                var psi = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = $@"-i ""{filename}"" -ac 2 -f s16le -ar 48000 pipe:1",
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                };
                var ffmpeg = Process.Start(psi);
                if (ffmpeg != null)
                {
                    var ffout = ffmpeg.StandardOutput.BaseStream;

                    var txStream = vNextConnection.GetTransmitSink();
                    await ffout.CopyToAsync(txStream);
                    await txStream.FlushAsync();
                }

                await vNextConnection.WaitForPlaybackFinishAsync();
            }
            catch (Exception ex) { exc = ex; }
            finally
            {
                await vNextConnection.SendSpeakingAsync(false);
                await ctx.Message.RespondAsync($"Finished playing `{filename}`");
            }

            if (exc != null)
                await ctx.RespondAsync($"An exception occurred during playback: `{exc.GetType()}: {exc.Message}`");
        }

        //TODO : Add pause/resume commands
    }
}
