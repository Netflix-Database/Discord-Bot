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
            eb.WithDescription("use " + Program.prefix + "help {commandname} for specific information about the command");

            List<CommandInfo> commands = Program._commands.Commands.ToList();

            foreach (CommandInfo command in commands)
            {
                if (command.Name == "add")
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
            eb.WithTitle("`Prefix: " + Program.prefix + "`");
            eb.WithDescription("Soon");


            await ReplyAsync("", false, eb.Build());
        }

    }
}
