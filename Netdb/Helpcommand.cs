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

            string id = Program.mainPrefix;
            if (Context.Channel.GetType() == typeof(IGuildChannel))
            {
                id = PrefixManager.GetPrefixFromGuildId(Context.Guild.Id);
            }

            eb.WithTitle("`Prefix: " + id + "`");
            eb.WithDescription("use " + id + "help [command] for detailed help");

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
            if (CommandDB.GetCommandData(command, out string name, out string alias, out string syntax, out string desc, out bool modReq, out int uses))
            {
                if (modReq && !Tools.IsModerator(Context.User))
                {
                    Tools.Embedbuilder($"No command found. Use `{PrefixManager.GetPrefixFromGuildId(Context.Guild.Id)}help` to get a overview over all commands.", Color.DarkRed, Context.Channel);
                    return;
                }

                EmbedBuilder eb = new EmbedBuilder();
                eb.WithColor(Color.Gold);

                eb.WithTitle("**" + name + "**");
                eb.WithDescription(string.IsNullOrEmpty(desc) ? "No description available" : desc);
                eb.AddField("Alias", string.IsNullOrEmpty(alias) ? "-" : alias);

                syntax = syntax.Trim();

                eb.AddField("Syntax",string.IsNullOrEmpty(syntax) ? "No syntax available" : $"`{PrefixManager.GetPrefixFromGuildId(Context.Guild.Id) + name} " + syntax + "`");

                await ReplyAsync("", false, eb.Build());
            }
            else
            {
                Tools.Embedbuilder($"No command found. Use `{PrefixManager.GetPrefixFromGuildId(Context.Guild.Id)}help` to get a overview over all commands.", Color.DarkRed, Context.Channel);
            }
        }

    }
}
