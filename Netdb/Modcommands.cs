using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using System.IO;
using System.Net;
using MySql.Data.MySqlClient;

namespace Netdb
{
    public class Modcommands : ModuleBase<SocketCommandContext>
    {
        [Command("add")]
        [Summary("Adds a movie or series to the database")]
        public async Task Add([Remainder] string input)
        {
            Tools.ValidateSQLValues(ref input);

            if (!Tools.IsModerator(Context.User))
            {
                Tools.Embedbuilder("You have to be a moderator to use this command", Color.DarkRed,Context.Channel);
                return;
            }

            string[] content = input.Split(",");

            if (content[0].ToLower() == "movie" || content[0].ToLower() == "series")
            {
                content[1] = content[1].Trim();

                if (Tools.IsAvailable(content[1]))
                {
                    Tools.Embedbuilder("This movie/series is already in the library", Color.DarkRed,Context.Channel);
                    return;
                }
                else
                {
                    content[1] = content[1].Trim();

                    var cmd = Program._con.CreateCommand();
                    cmd.CommandText = "select * from moviedata where movieName = 'null';";
                    var reader = await cmd.ExecuteReaderAsync();

                    if (reader.Read())
                    {
                        int id = (int)reader["id"];
                        reader.Close();
                        Tools.RunCommand($"update moviedata set movieName = '{content[1]}' where id = '{id}'; ");

                        if (content[0].ToLower() == "movie")
                        {
                            Tools.RunCommand($"update moviedata set type = '0' where movieName = '{content[1]}'; ");
                            await Context.Message.AddReactionAsync(new Emoji("✅"));
                            Tools.UpdateContentadded(Context.User);
                            return;
                        }
                        else if (content[0].ToLower() == "series")
                        {
                            Tools.RunCommand($"update moviedata set type = '1' where movieName = '{content[1]}'; ");
                            await Context.Message.AddReactionAsync(new Emoji("✅"));
                            Tools.UpdateContentadded(Context.User);
                            return;
                        }
                    }

                    reader.Close();

                    if (content[0].ToLower() == "movie")
                    {
                        Tools.RunCommand($"insert into moviedata (movieName, type) values ('{content[1]}', '{0}');");
                        await Context.Message.AddReactionAsync(new Emoji("✅"));
                        Tools.UpdateContentadded(Context.User);
                        return;
                    }
                    else if (content[0].ToLower() == "series")
                    {
                        Tools.RunCommand($"insert into moviedata (movieName, type) values ('{content[1]}', '{1}');");
                        await Context.Message.AddReactionAsync(new Emoji("✅"));
                        Tools.UpdateContentadded(Context.User);
                        return;
                    }
                }
            }

            content[0] = content[0].Trim();

            if (!Tools.IsAvailable(content[0]))
            {
                var cmd = Program._con.CreateCommand();
                cmd.CommandText = "select * from moviedata where id = '" + content[0] + "';";
                var reader = await cmd.ExecuteReaderAsync();

                if (reader.Read())
                {
                    content[0] = (string)reader["movieName"];
                    reader.Close();
                }
                else
                {
                    reader.Close();
                    Tools.Embedbuilder("This movie is not available.", Color.DarkRed, Context.Channel);
                    return;
                }
            }
            content[1] = content[1].Replace(" ", "");

            if (content[1] == "age")
            {
                if (!int.TryParse(content[2], out int age))
                {
                    Tools.Embedbuilder("The age must be a number", Color.DarkRed, Context.Channel);
                    return;
                }

                if (age < 0 || age > 21)
                {
                    Tools.Embedbuilder("Age must be between 0 and 21", Color.DarkRed, Context.Channel);
                    return;
                }

                if (age == 0)
                {
                    age = 69;
                }

                Tools.RunCommand($"update moviedata set age = '{age}' where movieName = '{content[0]}'; ");
                await Context.Message.AddReactionAsync(new Emoji("✅"));
                Tools.UpdateContentadded(Context.User);
            }
            else if (content[1] == "description")
            {
                if (content[2] == "")
                {
                    Tools.Embedbuilder("Please provide a description", Color.DarkRed, Context.Channel);
                    return;
                }

                if (content[2].StartsWith(" "))
                {
                    content[2] = content[2].Remove(0, 1);
                }

                string description = content[2];

                if (content.Length > 2)
                {
                    for (int i = 3; i < content.Length; i++)
                    {
                        description += ", " + content[i];
                    }
                }

                if (description.Length > 300)
                {
                    Tools.Embedbuilder("Descritpion must be less than 300 charactars", Color.DarkRed, Context.Channel);
                    return;
                }

                Tools.RunCommand($"update moviedata set description = '{description}' where movieName = '{content[0]}'; ");
                await Context.Message.AddReactionAsync(new Emoji("✅"));
                Tools.UpdateContentadded(Context.User);
            }
            else if (content[1] == "genres")
            {
                Tools.RunCommand($"update moviedata set genres = '{content[2]}' where movieName = '{content[0]}'; ");
                await Context.Message.AddReactionAsync(new Emoji("✅"));
                Tools.UpdateContentadded(Context.User);
            }
            else if (content[1] == "releasedate" || content[1] == "rd")
            {
                if (int.TryParse(content[2], out int releasedate) && releasedate < DateTime.Now.Year + 1 && releasedate > 1888)
                {
                    Tools.RunCommand($"update moviedata set releaseDate = '{Convert.ToInt32(content[2])}' where movieName = '{content[0]}'; ");
                    await Context.Message.AddReactionAsync(new Emoji("✅"));
                    Tools.UpdateContentadded(Context.User);
                    return;
                }

                Tools.Embedbuilder("The releasedate must be a number between 1888 and " + DateTime.Now.Year, Color.DarkRed, Context.Channel);
            }
            else if (content[1] == "length")
            {
                if (int.TryParse(content[2], out int length) && length > 0)
                {
                    Tools.RunCommand($"update moviedata set movieLength = '{Convert.ToInt32(content[2])   }' where movieName = '{content[0]}'; ");
                    await Context.Message.AddReactionAsync(new Emoji("✅"));
                    Tools.UpdateContentadded(Context.User);
                    return;
                }

                Tools.Embedbuilder("The length must be a number greater than 0", Color.DarkRed, Context.Channel);
            }
            else if (content[1] == "image")
            {
                var attachments = Context.Message.Attachments;

                if (attachments.Count == 0)
                {
                    Tools.Embedbuilder("You have to attach a file", Color.Blue, Context.Channel);
                    return;
                }

                if (attachments.ElementAt(0).Filename.EndsWith(".png"))
                {
                    string fileurl = attachments.ElementAt(0).Url;

                    WebClient mywebclient = new WebClient();

                    byte[] ImageData = mywebclient.DownloadData(fileurl);

                    var cmd = Program._con.CreateCommand();
                    cmd.CommandText = $"update moviedata set image = @image where id = '{Tools.Getid(content[0])}';";

                    var blob = new MySqlParameter("@image", MySqlDbType.MediumBlob, ImageData.Length)
                    {
                        Value = ImageData
                    };

                    cmd.Parameters.Add(blob);

                    cmd.ExecuteNonQuery();

                    await Context.Message.AddReactionAsync(new Emoji("✅"));

                    Tools.UpdateContentadded(Context.User);
                }
                else
                {
                    Tools.Embedbuilder("The file has to be a png!", Color.Blue, Context.Channel);
                }
            }
            else
            {
                Tools.Embedbuilder("Your are missing an operator", Color.DarkRed, Context.Channel);
            }
        }

        [Command("remove")]
        [Alias("re")]
        [Summary("Removes a movie from the library")]
        public async Task Remove([Remainder] string moviename)
        {
            Tools.ValidateSQLValues(ref moviename);

            if (!Tools.IsModerator(Context.User))
            {
                Tools.Embedbuilder("You have to be a moderator to use this command", Color.DarkRed, Context.Channel);
                return;
            }

            if (Tools.IsAvailable(moviename))
            {
                int id = Tools.Getid(moviename);

                Tools.RunCommand($"update moviedata set movieName = 'null',description = 'null',age = '0',genres = 'null',movieLength = '0',searchcounter = '0',releaseDate = '0',reviews = '0',reviewpoints = '0',image = 'null' where movieName = '{moviename}';");

                Tools.RunCommand($"delete from userdata where movieid = '{id}';");

                Tools.RunCommand($"delete from reviewsdata where movieid = '{id}';");

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
            Tools.ValidateSQLValues(ref date);
            Tools.ValidateSQLValues(ref moviename);

            if (!Tools.IsModerator(Context.User))
            {
                Tools.Embedbuilder("You have to be a moderator to use this command", Color.DarkRed, Context.Channel);
                return;
            }

            if (!date.EndsWith("."))
            {
                date += ".";
            }

            DateTime realeasedate = Convert.ToDateTime(date + DateTime.Now.Year.ToString());

            if (Tools.IsAvailable(moviename))
            {
                Tools.Embedbuilder("This movie is already available", Color.DarkRed, Context.Channel);
                return;
            }

            var cmd = Program._con.CreateCommand();
            cmd.CommandText = $"select * from comingsoon where moviename = '{moviename}';";
            var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                Tools.Embedbuilder("This movie is already in the What's next list", Color.DarkRed, Context.Channel);
                reader.Close();
                return;
            }

            reader.Close();

            Tools.UpdateContentadded(Context.User);
            Tools.RunCommand($"insert into comingsoon (moviename,releasedate) values ('{moviename}','{realeasedate:yyyy-MM-dd}');");
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        [Command("missing")]
        [Alias("m")]
        [Summary("Shows what's missing in the Database")]
        public async Task ShowMissing(int i = 0)
        {
            if (Tools.IsModerator(Context.User))
            {
                var cmd = Program._con.CreateCommand();
                cmd.CommandText = $"select * from moviedata where description = 'null' or age = '0' or movieLength = '0' or releasedate = '0' or image is null;";
                var reader = cmd.ExecuteReader();

                if (!reader.Read())
                {
                    Tools.Embedbuilder("There is currently nothing missing", Color.Green, Context.Channel);
                    reader.Close();
                    return;
                }

                while ((string)reader["movieName"] == "null")
                {
                    reader.Read();
                }

                EmbedBuilder eb = new EmbedBuilder();
                eb.WithTitle((string)reader["movieName"]);
                eb.WithDescription("#add " + (string)reader["movieName"] + ",");

                string missing = "";

                if ((string)reader["description"] == "null")
                {
                    missing += "description, ";
                }

                if ((int)reader["age"] == 0)
                {
                    missing += "age, ";
                }

                if ((string)reader["genres"] == "null")
                {
                    missing += "genres, ";
                }

                if ((int)reader["movieLength"] == 0)
                {
                    missing += "length, ";
                }

                if ((int)reader["releaseDate"] == 0)
                {
                    missing += "releasedate, ";
                }

                if (reader["image"] == DBNull.Value)
                {
                    missing += "image";
                }

                reader.Close();

                eb.AddField("Missing", missing);

                await Context.Channel.SendMessageAsync("", false, eb.Build());
            }
            else
            {
                Tools.Embedbuilder("You have to be a moderator to do this", Color.DarkRed, Context.Channel);
            }
        }

        [Command("update")]
        [Alias("u")]
        [Summary("Updates something in the Db")]
        public async Task Update()
        {
            if (Tools.IsModerator(Context.User))
            {
                Program.BackupDB();
                Program.GetMostsearched();
                Program.GetBestReviewed();
                //Program.GetMostPopular();

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

                Tools.RunCommand($"insert into moderation (userid, since,contentadded) values ('{user.Id}', '{DateTime.Now.Date:yyyy-MM-dd}','0');");
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

                Tools.RunCommand($"delete from moderation where userid = '{user.Id}';");
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
                cmd.CommandText = $"select * from reviewsdata where userid = '{id}';";
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
        }

        [Command("backup")]
        [Alias("b")]
        [Summary("Creates a database backup")]
        public async Task Backup()
        {
            if (Context.User.Id == 487265499785199616)
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
            if (Context.User.Id == 487265499785199616)
            {
                string[] files = Directory.GetFiles("DB_Backups");
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

                    backups += "`" + fi.CreationTime + "` Backup \n";
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
        public async Task Loadbackup([Remainder] DateTime backuptime)
        {
            if (Context.User.Id == 487265499785199616)
            {
                string file = "DB_Backups/" + backuptime.ToString().Replace(" ", "_").Replace(".", "_").Replace(":", "_") + "_backup.sql";
                MySqlCommand cmd = new MySqlCommand();
                MySqlBackup mb = new MySqlBackup(cmd);

                cmd.Connection = Program._con;

                mb.ImportFromFile(file);

                await Program.Client_Log(new LogMessage(LogSeverity.Info, "System", "Database restored"));

                await Context.Message.AddReactionAsync(new Emoji("✅"));
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
            if (Context.User.Id == 487265499785199616)
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
