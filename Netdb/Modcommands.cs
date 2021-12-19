using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace Netdb
{
    public class Modcommands : ModuleBase<SocketCommandContext>
    {
        public event ErrorOccoured HandleError = Program.HandleError;

        [Command("update")]
        [Alias("u")]
        [Summary("Updates something in the Db")]
        public async Task Update()
        {
            if (Tools.IsModerator(Context.User))
            {
                Program.Perform60MinuteUpdate();

                await Context.Message.AddReactionAsync(new Emoji("✅"));
            }
            else
            {
                Tools.Embedbuilder("You have to be a moderator to do this", Color.DarkRed, Context.Channel);
            }
        }

        [Command("op")]
        [Summary("Op's a user")]
        public async Task Op(IGuildUser user)
        {
            if (Context.User.Id == 487265499785199616)
            {
                if (Tools.IsModerator(user))
                {
                    Tools.Embedbuilder(user.Username + " is already a moderator", Color.DarkRed, Context.Channel);
                    return;
                }

                Tools.RunCommand($"insert into moderation (userid, ismod, since,contentadded) values ('{user.Id}','1', '{DateTime.Now.Date:yyyy-MM-dd}','0');");
                await Context.Message.AddReactionAsync(new Emoji("✅"));
            }
            else
            {
                Tools.Embedbuilder("You are not allowed to do this", Color.DarkRed, Context.Channel);
            }
        }

        [Command("deop")]
        [Summary("Deop's a user")]
        public async Task Deop(IUser user)
        {
            if (Context.User.Id == 487265499785199616)
            {
                if (!Tools.IsModerator(user))
                {
                    Tools.Embedbuilder(user.Username + " is not a moderator", Color.DarkRed, Context.Channel);
                    return;
                }

                Tools.RunCommand($"update moderation set ismod = '{0}' where userid = '{user.Id}';");
                await Context.Message.AddReactionAsync(new Emoji("✅"));
            }
            else
            {
                Tools.Embedbuilder("You are not allowed to do this", Color.DarkRed, Context.Channel);
            }
        }

        [Command("mystats")]
        [Alias("ms", "stats")]
        [Summary("Shows stats from a moderator")]
        public async Task Mystats(IUser user = null)
        {
            ulong id;
            EmbedBuilder eb = new EmbedBuilder();

            if (user != null)
            {
                id = user.Id;

                eb.WithColor(Color.Blue);
                eb.WithAuthor(user);
                eb.WithTitle(user.Username + "'s stats");
            }
            else
            {
                id = Context.User.Id;

                eb.WithColor(Color.Blue);
                eb.WithAuthor(Context.User);
                eb.WithTitle(Context.User.Username + "'s stats");
            }

            var cmd = Program._con.CreateCommand();
            cmd.CommandText = $"select * from moderation where userid = '{id}';";
            var reader = await cmd.ExecuteReaderAsync();

            if (reader.Read())
            {
                DateTime since = (DateTime)reader["since"];

                eb.AddField("Moderator since", since.ToString("dddd, dd MMMM yyyy"));
                eb.AddField("Content added", reader["contentadded"]);

                reader.Close();

                cmd = Program._con.CreateCommand();
                cmd.CommandText = $"select null from reviews where userid = '{id}';";
                reader = await cmd.ExecuteReaderAsync();

                int reviews = 0;

                while (reader.Read())
                {
                    reviews++;
                }

                reader.Close();

                eb.AddField("Movie/series reviewed", reviews);

                await Context.Channel.SendMessageAsync("", false, eb.Build());
            }
            else
            {
                reader.Close();
                Tools.Embedbuilder("You are not a moderator", Color.DarkRed, Context.Channel);
            }

            reader.Dispose();
            cmd.Dispose();
        }

        [Command("disconnect")]
        [Alias("dc")]
        [Summary("Disconnects the bot")]
        public async Task Disconnect()
        {
            if (Context.User.Id == 487265499785199616 || Context.User.Id == 300571683507404800)
            {
                Tools.Embedbuilder("Disconnected", Color.DarkRed, Context.Channel);

                await Program._client.StopAsync();
            }
            else
            {
                Tools.Embedbuilder("I don't think i will", Color.DarkRed, Context.Channel);
            }

        }
    }
}
