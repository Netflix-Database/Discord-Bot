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
            eb.WithDescription("use " + Program.prefix + "help [command] for more detailed help");

            CommandDB.GetCommandDataOfAllCommands(out string[] commands, out string[] aliase, out string[] desc, out string[] short_desc, out bool[] modReq, out int[] uses);

            for (int i = 0; i < commands.Length; i++)
            {
                if (!modReq[i])
                {
                    string description = $"Alias: {aliase[i]} \n{short_desc[i]}";

                    eb.AddField(commands[i], description);
                }
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
                eb.AddField("Uses", uses);
                eb.AddField("Mod", modReq);
            }
            else
            {
                eb.AddField("No command found.", $"Use {Program.prefix}help to get a overview over all commands.");
            }

            await ReplyAsync("", false, eb.Build());
        }

    }
}
