using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Netdb
{
    [Group("help")]
    public class Helpcommand : ModuleBase<SocketCommandContext>
    {
        [Command]
        public async Task Help()
        {
            EmbedBuilder eb = new EmbedBuilder();
            eb.WithColor(Color.Gold);
            eb.WithTitle("`Prefix: " + Program.prefix + "`");
            eb.WithDescription("use " + Program.prefix + "help [command] for detailed help");

            List<CommandInfo> commands = Program._commands.Commands.ToList();

            foreach (CommandInfo command in commands)
            {
                if (command.Name == "Help")
                {
                    break;
                }

                // Get the command Summary attribute information
                string embedFieldText = command.Summary ?? "No description available\n";

                eb.AddField(command.Name, embedFieldText);
            }

            await ReplyAsync("", false, eb.Build());
        }

        [Command]
        public async Task Help(string command)
        {
            if (CommandDB.GetCommandData(command, out string alias, out string desc, out string short_desc, out bool modReq, out int uses))
            {
                if (modReq && !Tools.IsModerator(Context.User))
                {
                    Tools.Embedbuilder($"No command found. Use {Program.prefix}help to get a overview over all commands.", Color.DarkRed, Context.Channel);
                    return;
                }

                EmbedBuilder eb = new EmbedBuilder();
                eb.WithColor(Color.Gold);

                eb.WithTitle("**" + command + "**");
                eb.WithDescription(desc);
                eb.AddField("Alias", string.IsNullOrEmpty(alias) ? "-" : alias);
                eb.WithDescription(string.IsNullOrEmpty(short_desc) ? "No description available" : short_desc);

                await ReplyAsync("", false, eb.Build());
            }
            else
            {
                Tools.Embedbuilder($"No command found. Use {Program.prefix}help to get a overview over all commands.", Color.DarkRed, Context.Channel);
            }
        }

    }
}
