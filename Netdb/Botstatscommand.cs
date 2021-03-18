using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace Netdb
{
    [Group("botstats")]
    [Alias("bs")]
    [Summary("Show stats about the bot")]
    public class Botstatscommand : ModuleBase<SocketCommandContext>
    {
        [Command]
        public async Task botstats()
        {
            int reviews = 0;
            int subscribtions = 0;
            int movies = 0;
            int series = 0;

            if (!(Program._con.State.ToString() == "Closed"))
            {
                var cmd = Program._con.CreateCommand();
                cmd.CommandText = $"select * from reviewsdata;";
                var reader = await cmd.ExecuteReaderAsync();

                while (reader.Read())
                {
                    reviews++;
                }

                reader.Close();

                cmd = Program._con.CreateCommand();
                cmd.CommandText = $"select * from subscriberlist;";
                reader = await cmd.ExecuteReaderAsync();

                while (reader.Read())
                {
                    subscribtions++;
                }

                reader.Close();

                cmd = Program._con.CreateCommand();
                cmd.CommandText = $"select * from moviedata where type = '{0}';";
                reader = await cmd.ExecuteReaderAsync();

                while (reader.Read())
                {
                    movies++;
                }
                reader.Close();

                cmd = Program._con.CreateCommand();
                cmd.CommandText = $"select * from moviedata where type = '{1}';";
                reader = await cmd.ExecuteReaderAsync();

                while (reader.Read())
                {
                    series++;
                }
                reader.Close();
            }

            EmbedBuilder eb = new EmbedBuilder();

            eb.WithColor(Color.DarkTeal);
            eb.WithTitle("Botstats");

            eb.AddField("Bot started", Program.startedAt.ToString("HH:mm:ss") + " CET");
            eb.AddField("Commands executed since start", Program.commandsexecuted);
            eb.AddField("Commands executed lifetime", "Soon");
            eb.AddField("Database connection", Program._con.State);
            eb.AddField("Subscriber", subscribtions);
            eb.AddField("Movies", movies);
            eb.AddField("Series", series);
            eb.AddField("Reviews", reviews);

            await Context.Channel.SendMessageAsync("", false, eb.Build());
        }

        [Command]
        public async Task botstats(string command)
        {
            EmbedBuilder eb = new EmbedBuilder();

            eb.WithColor(Color.DarkTeal);

            bool modd = Tools.IsModerator(Context.User);

            if (!(Program._con.State.ToString() == "Closed") && (command == "commands" || command == "cmds"))
            {
                eb.WithTitle("Command stats");

                CommandDB.GetCommandDataOfAllCommands(out string[] name, out string[] alias, out string[] desc, out string[] shoert_Desc, out bool[] mod, out int[] uses);

                for (int i = 0; i < name.Length; i++)
                {
                    if (mod[i] && !modd)
                    {
                        continue;
                    }

                    eb.AddField(name[i], "Uses: " + uses[i], true);
                }

                await Context.Channel.SendMessageAsync("", false, eb.Build());
                return;
            }

            eb.WithTitle("Unknow argument.");
            await Context.Channel.SendMessageAsync("", false, eb.Build());
        }
    }
}
