using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using System.IO;
using MySql.Data.MySqlClient;
using System.Reflection;

namespace Netdb
{
    public class Modcommands : ModuleBase<SocketCommandContext>
    {
        public event ErrorOccoured HandleError = Program.HandleError;

        [Command("add")]
        [Summary("Adds a movie or series to the database")]
        public async Task Add([Remainder]string id)
        {
            
        }

        [Command("remove")]
        [Alias("re")]
        [Summary("Removes a movie from the library")]
        public async Task Remove([Remainder] string moviename)
        {
            if (Tools.ValidateSQLValues(moviename, Context.Channel))
            {
                return;
            }

            if (!Tools.IsModerator(Context.User))
            {
                Tools.Embedbuilder("You have to be a moderator to use this command", Color.DarkRed, Context.Channel);
                return;
            }

            if (Tools.IsAvailable(moviename))
            {
                int id = Tools.Getid(moviename);

                Tools.RunCommand($"update moviedata set name = 'null',description = 'null',age = '0',genres = 'null',movieLength = '0',searchcounter = '0',releaseDate = '0',reviews = '0',reviewpoints = '0',image = 'null' where movieName = '{moviename}';");

                Tools.RunCommand($"delete from watchlistdata where netflixid = '{id}';");

                Tools.RunCommand($"delete from reviews where netflixid = '{id}';");

                Tools.RunCommand($"delete from totalreviews where netflixid = '{id}';");

                await Context.Message.AddReactionAsync(new Emoji("✅"));
            }
            else if (Tools.IsAvailableId(moviename))
            {
                Tools.RunCommand($"update netflixdata set name = 'null',description = 'null',age = '0',genres = 'null',movieLength = '0',searchcounter = '0',releaseDate = '0',reviews = '0',reviewpoints = '0',image = 'null' where id = '{moviename}';");

                Tools.RunCommand($"delete from watchlistdata where netflixid = '{moviename}';");

                Tools.RunCommand($"delete from reviews where netflixid = '{moviename}';");

                await Context.Message.AddReactionAsync(new Emoji("✅"));
            }
            else
            {
                Tools.Embedbuilder("This movie is not available", Color.DarkRed, Context.Channel);
            }
        }

        [Command("next")]
        [Alias("n")]
        [Summary("Add movies/series that are coming to Netflix")]
        public async Task Next(string date, [Remainder] string moviename)
        {
            if (Tools.ValidateSQLValues(moviename, Context.Channel))
            {
                return;
            }

            if (Tools.ValidateSQLValues(date, Context.Channel))
            {
                return;
            }


            if (!Tools.IsModerator(Context.User))
            {
                Tools.Embedbuilder("You have to be a moderator to use this command", Color.DarkRed, Context.Channel);
                return;
            }

            if (!date.EndsWith("."))
            {
                date += ".";
            }

            if (!DateTime.TryParse(date + DateTime.Now.Year.ToString(), out DateTime realeasedate))
            {
                Tools.Embedbuilder("That's not a valid date",Color.DarkRed,Context.Channel);
                return;
            }
           
            if (Tools.IsAvailable(moviename))
            {
                Tools.Embedbuilder("This movie is already available", Color.DarkRed, Context.Channel);
                return;
            }

            var cmd = Program._con.CreateCommand();
            cmd.CommandText = $"select null from comingsoon where moviename = '{moviename}';";
            var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                Tools.Embedbuilder("This movie is already in the What's next list", Color.DarkRed, Context.Channel);
                reader.Close();
                reader.Dispose();
                cmd.Dispose();
                return;
            }

            reader.Close();

            Tools.UpdateContentadded(Context.User);
            Tools.RunCommand($"insert into comingsoon (moviename,releasedate) values ('{moviename}','{realeasedate:yyyy-MM-dd}');");
            await Context.Message.AddReactionAsync(new Emoji("✅"));

            reader.Dispose();
            cmd.Dispose();
        }

        [Command("update")]
        [Alias("u")]
        [Summary("Updates something in the Db")]
        public async Task Update()
        {
            if (Tools.IsModerator(Context.User))
            {
                Program.Perform5MinuteUpdate();

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

        [Command("backup")]
        [Alias("b")]
        [Summary("Creates a database backup")]
        public async Task Backup()
        {
            if (Context.User.Id == 487265499785199616 || Context.User.Id == 300571683507404800)
            {
                Program.BackupDB();
                await Context.Message.AddReactionAsync(new Emoji("✅"));
            }
            else
            {
                Tools.Embedbuilder("You are not allowed to do this", Color.DarkRed, Context.Channel);
            }
        }

        [Command("listbackups")]
        [Alias("lb")]
        [Summary("Lists all Backups")]
        public async Task ListBackups()
        {
            if (Context.User.Id == 487265499785199616 || Context.User.Id == 300571683507404800)
            {
                string[] files = Directory.GetFiles(Program.filepath + "DB_Backups");
                FileInfo fi;
                string backups = "";
                long size = 0;

                if (files.Length == 0)
                {
                    Tools.Embedbuilder("No backups available", Color.DarkRed, Context.Channel);
                    return;
                }

                for (int i = 0; i < files.Length; i++)
                {
                    fi = new FileInfo(files[i]);

                    backups += "`" + fi.Name + "`\n";
                    size += fi.Length;
                }

                EmbedBuilder eb = new EmbedBuilder();
                eb.WithTitle("Netdb's Backups");
                eb.AddField("Most recent Backups", backups);
                eb.WithFooter(x => x.Text = ((double)size / (double)1000000000).ToString("F2") + "Gb");

                await Context.Channel.SendMessageAsync("", false, eb.Build());
            }
            else
            {
                Tools.Embedbuilder("You are not allowed to do this", Color.DarkRed, Context.Channel);
            }
        }

        [Command("restore")]
        [Alias("res")]
        [Summary("Restores a database backup")]
        public async Task Loadbackup(string filename)
        {
            if (Context.User.Id == 487265499785199616 || Context.User.Id == 300571683507404800)
            {
                try
                {
                    if (!File.Exists(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + Path.DirectorySeparatorChar + "DB_Backups" + Path.DirectorySeparatorChar + filename))
                    {
                        Tools.Embedbuilder("File does not exist", Color.DarkRed, Context.Channel);
                        return;
                    }

                    MySqlCommand cmd = new MySqlCommand();
                    MySqlBackup mb = new MySqlBackup(cmd);

                    cmd.Connection = Program._con;

                    await Context.Message.AddReactionAsync(new Emoji("⌛"));

                    EmbedBuilder eb = new EmbedBuilder();
                    eb.WithTitle("Restoring Database");
                    eb.WithDescription("Restoring " + filename);
                    await Program._client.GetUser(487265499785199616).SendMessageAsync("", false, eb.Build());
                    await Program._client.GetUser(300571683507404800).SendMessageAsync("", false, eb.Build());

                    mb.ImportFromFile(Path.Combine(Program.filepath + "DB_Backups", filename));

                    await Program.Client_Log(new LogMessage(LogSeverity.Info, "System", "Database restored"));

                    await Context.Message.RemoveReactionAsync(new Emoji("⌛"), Program._client.CurrentUser);
                    await Context.Message.AddReactionAsync(new Emoji("✅"));

                    cmd.Dispose();
                    mb.Dispose();
                }
                catch (Exception ex)
                {
                    await Program.Client_Log(new LogMessage(LogSeverity.Info, "System", "Could't restore database", ex));
                    await Context.Message.ReplyAsync(ex.ToString());
                }
            }
            else
            {
                Tools.Embedbuilder("You are not allowed to do this", Color.DarkRed, Context.Channel);
            }
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
