using System.Threading;
using System.Threading.Tasks;
using Discord.Commands;
using SysBot.Base;

namespace SysBot.AnimalCrossing
{
    // ReSharper disable once UnusedType.Global
    public class ControlModule : ModuleBase<SocketCommandContext>
    {
        [Command("detach")]
        [Summary("Detaches the virtual controller so the operator can use their own handheld controller temporarily.")]
        [RequireSudo]
        public async Task DetachAsync()
        {
            await ReplyAsync("A controller detach request will be executed momentarily.").ConfigureAwait(false);
            var bot = Globals.Bot;
            await bot.Connection.SendAsync(SwitchCommand.DetachController(), CancellationToken.None).ConfigureAwait(false);
        }

        [Command("setCode")]
        [Summary("Tells the user to use the correct dodo fetch function.")]
        [RequireSudo]
        public async Task DodoHelpAsync([Summary("Anything")][Remainder]string code)
        {
            await ReplyAsync($"Dodo code does not need to be set manually, please use fetchDodo to pull the current Dodo code from RAM, or use overrideCode to set it manually if this fails.").ConfigureAwait(false);
        }

        [Command("fetchDodo")]
        [Alias("fetchCode", "updateDodo")]
        [Summary("Pulls the current dodo from memory.")]
        [RequireSudo]
        public async Task FetchDodo()
        {
            var bot = Globals.Bot;
            await bot.UpdateDodo(CancellationToken.None).ConfigureAwait(false);
            var code = bot.DodoCode;
            await ReplyAsync($"The dodo code for the bot has been set to {code}.").ConfigureAwait(false);
        }

        [Command("overrideCode")]
        [Alias("override", "overrideDodo")]
        [Summary("Sets a string to the Dodo Code property for users to call via the associated command.")]
        [RequireSudo]
        public async Task SetDodoCodeAsync([Summary("Current Dodo Code for the island.")][Remainder] string code)
        {
            var bot = Globals.Bot;
            bot.DodoCode = code;
            await ReplyAsync($"The dodo code for the bot has been set to {code}.").ConfigureAwait(false);
        }

        [Command("toggleRequests")]
        [Summary("Toggles accepting drop requests.")]
        [RequireSudo]
        public async Task ToggleRequestsAsync()
        {
            bool value = (Globals.Bot.Config.AcceptingCommands ^= true);
            await ReplyAsync($"Accepting drop requests: {value}.").ConfigureAwait(false);
        }
    }
}
