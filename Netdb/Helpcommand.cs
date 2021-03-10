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
            EmbedBuilder eb = new EmbedBuilder();
            eb.WithColor(Color.Gold);

            if (CommandDB.GetCommandData(command, out string alias, out string desc, out string short_desc, out bool modReq, out int uses))
            {
                eb.WithTitle("**" + command + "**");
                eb.WithDescription(desc);
                eb.AddField("Alias", alias);

                string descritpion = desc ?? "No description available\n";

                eb.AddField("Syntax", descritpion);

                if (!modReq)
                {
                   

            
                }
                else if (Tools.IsModerator(Context.User))
                {
                    eb.WithTitle("**" + command + "**");
                    eb.WithDescription(string.IsNullOrEmpty(desc) ? "-" : desc);
                    eb.AddField("Alias", string.IsNullOrEmpty(alias) ? "-" : alias);
                }
            }
            else
            {
                eb.AddField("No command found.", $"Use {Program.prefix}help to get a overview over all commands.");
            }

            await ReplyAsync("", false, eb.Build());
        }

    }
}
