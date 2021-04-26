using Discord;
using Discord.Commands;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Netdb
{
    [Group("help")]
    [Summary("Lists all commands with their description")]
    public class Helpcommand : ModuleBase<SocketCommandContext>
    {
        public event ErrorOccoured HandleError = Program.HandleError;

        [Command]
        public async Task Help()
        {
            EmbedBuilder eb = new EmbedBuilder();
            eb.WithColor(Color.Gold);

            string id = PrefixManager.GetPrefixFromGuildId(Context.Channel);

            eb.WithTitle("`Prefix: " + id + "`");
            eb.WithDescription("use `" + id + "help [command]` for detailed help");

            List<CommandInfo> commands = Program._commands.Commands.ToList();

            foreach (CommandInfo command in commands)
            {
                if (command.Name == "Help")
                {
                    break;
                }

                if (command.Name != "botstats" && command.Name != "commands")
                {
                    // Get the command Summary attribute information
                    string embedFieldText = command.Summary ?? "No description available\n";

                    eb.AddField(command.Name, embedFieldText);
                }
            }

            eb.AddField("botstats", "Shows stats about the bot");

            await ReplyAsync("", false, eb.Build());
        }

        [Command]
        public async Task Help(string command)
        {
            if (Tools.ValidateSQLValues(command, Context.Channel))
            {
                return;
            }

            if (CommandDB.GetCommandData(command, out string name, out string alias, out string syntax, out string desc, out bool modReq, out int uses))
            {
                if (modReq && !Tools.IsModerator(Context.User))
                {
                    Tools.Embedbuilder($"No command found. Use `{PrefixManager.GetPrefixFromGuildId(Context.Channel)}help` to get a overview over all commands.", Color.DarkRed, Context.Channel);
                    return;
                }

                EmbedBuilder eb = new EmbedBuilder();
                eb.WithColor(Color.Gold);
                eb.WithTitle("**" + name + "**");
                eb.WithDescription(string.IsNullOrEmpty(desc) ? "No description available" : desc);
                eb.AddField("Alias", string.IsNullOrEmpty(alias) ? "-" : alias);
                eb.AddField("Syntax",syntax == "-" ? $"`{PrefixManager.GetPrefixFromGuildId(Context.Channel) + name}`" : $"`{PrefixManager.GetPrefixFromGuildId(Context.Channel) + name} " + syntax + "`");
                eb.AddField("Used", uses + " times");

                await ReplyAsync("", false, eb.Build());
            }
            else
            {
                Tools.Embedbuilder($"No command found. Use `{PrefixManager.GetPrefixFromGuildId(Context.Channel)}help` to get a overview over all commands.", Color.DarkRed, Context.Channel);
            }
        }

    }
}
