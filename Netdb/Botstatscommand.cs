using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace Netdb
{
    [Group("botstats")]
    [Alias("bs")]
    [Summary("Shows stats about the bot")]
    public class Botstatscommand : ModuleBase<SocketCommandContext>
    {
        [Command]
        [Summary("Shows stats about the bot")]
        public async Task Botstats()
        {
            int commandsexecutedlifetime = 0;

                var cmd = Program._con.CreateCommand();
                cmd.CommandText = $"select * from commands;";
                var reader = await cmd.ExecuteReaderAsync();

                while (reader.Read())
                {
                    commandsexecutedlifetime += (int)reader["uses"];
                }
                reader.Close();

                await reader.DisposeAsync();
                await cmd.DisposeAsync();

            EmbedBuilder eb = new EmbedBuilder();

            eb.WithColor(Color.DarkTeal);
            eb.WithTitle("Botstats");

            eb.AddField("Bot started", Program.startedAt.ToString("HH:mm:ss") + " CET");
            eb.AddField("Commands executed since start", Program.commandsexecuted);
            eb.AddField("Commands executed lifetime", commandsexecutedlifetime);
            eb.AddField("Database connection", Program._con.State);
            eb.AddField("Subscriber", Program.subscribers);
            eb.AddField("Movies", Program.movies);
            eb.AddField("Series", Program.series);
            eb.AddField("Reviews", Program.reviews);
            eb.AddField("Server", Program._client.Guilds.Count);
            eb.AddField("Users", Program.memberCount);

            int dailymessage = (int)Program.dailymessagetime.TimeOfDay.Subtract(DateTime.Now.TimeOfDay).TotalMinutes;

            eb.AddField("Daily Message", dailymessage + " min");

            await Context.Channel.SendMessageAsync("", false, eb.Build());
        }

        [Command ("commands")]
        [Alias("cmds")]
        [Summary("Shows stats about the bot")]
        public async Task Commands()
        {
            EmbedBuilder eb = new EmbedBuilder();

            eb.WithColor(Color.DarkTeal);

            if (Program._con.State == System.Data.ConnectionState.Open)
            {
                eb.WithTitle("Command stats");
                CommandDB.GetCommandDataOfAllCommands(out string[] name, out _, out _, out _, out _, out int[] uses);

                for (int i = 0; i < name.Length; i++)
                {
                    if (name[i] != "disconnect")
                    {
                        eb.AddField(name[i], "Uses: " + uses[i], true);
                    }
                }

                await Context.Channel.SendMessageAsync("", false, eb.Build());
                return;
            }

            eb.WithTitle("Unknow argument.");
            await Context.Channel.SendMessageAsync("", false, eb.Build());
        }
    }
}
