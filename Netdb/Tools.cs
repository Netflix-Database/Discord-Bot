using Discord;
using System;
using System.IO;
using Discord.WebSocket;
using System.Reflection;

namespace Netdb
{
    class Tools
    {
        /// <summary>
        /// Gets the id of a certain movie/series
        /// </summary>
        /// <param name="moviename"></param>
        /// <returns></returns>
        public static int Getid(string moviename)
        {
            int id;

            if (Program._con.State.ToString() == "Closed")
            {
                Program._con.Open();
            }

            var cmd = Program._con.CreateCommand();
            cmd.CommandText = "select netflixid from netflixdata where name = '" + moviename + "';";
            var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                id = (int)reader["netflixid"];
                reader.Close();
                return id;
            }
            reader.Close();
            return 0;
        }
        /// <summary>
        /// Checks if someone tried to do an sql injection
        /// </summary>
        /// <param name="values"></param>
        /// <param name="c"></param>
        /// <returns>bool</returns>
        public static bool ValidateSQLValues(string values, ISocketMessageChannel c)
        {
            int sus = 0;

            values = values.ToLower();

            if (values.Contains(';'))
            {
                sus++;
            }

            if (values.Contains("drop"))
            {
                sus++;
            }

            if (values.Contains("table"))
            {
                sus++;
            }

            if (values.Contains('\''))
            {
                sus++;
            }

            if (values.Contains('='))
            {
                sus++;
            }

            if (values.Contains("update"))
            {
                sus++;
            }

            if (sus >= 3)
            {
                Tools.Embedbuilder("This command might be harmful to our database. Click [here](https://bfy.tw/QZLb) to see what has gone wrong.", Color.Red, c);

                return true;
            }
            else
            {
                if (sus > 0)
                {
                    Console.WriteLine($"Kinda sus value detected on sus lvl {sus}: " + values);
                }

                return false;
            }
        }

        /// <summary>
        /// Test if the movie/series is already in your watchlist
        /// </summary>
        /// <param name="movieid"></param>
        /// <param name="userid"></param>
        /// <returns></returns>
        public static bool Exists(int movieid, ulong userid)
        {
            if (Program._con.State.ToString() == "Closed")
            {
                Program._con.Open();
            }

            var cmd = Program._con.CreateCommand();
            cmd.CommandText = $"select * from userdata where userid = '{userid}' and movieid = '{movieid}';";
            var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                reader.Close();
                return true;
            }
            reader.Close();
            return false;
        }

        /// <summary>
        /// Gets every available data for a specific movie/series
        /// </summary>
        /// <param name="search"></param>
        /// <param name="movie"></param>
        public static void GetMovieData(string search, out MovieData movie, ulong userid)
        {
            if (Program._con.State.ToString() == "Closed")
            {
                Program._con.Open();
            }

            var cmd = Program._con.CreateCommand();
            cmd.CommandText = "select * from netflixdata where name = '" + search + "';";
            var redar = cmd.ExecuteReader();

            if (!redar.Read())
            {
                redar.Close();
                cmd = Program._con.CreateCommand();
                cmd.CommandText = "select * from netflixdata where netflixid = '" + search + "';";
                redar = cmd.ExecuteReader();
                redar.Read();
            }

            byte[] image;

            if (redar["desktopImg"] == DBNull.Value)
            {
                image = File.ReadAllBytes(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + Path.DirectorySeparatorChar + "NoImage.jpg");
            }
            else
            {
                image = (byte[])redar["desktopImg"];
            }
  
            movie = new MovieData
            {
                Age = (int)redar["age"],
                Description = (string)redar["description"],
                Id = (int)redar["netflixid"],
                Link = "https://www.netflix.com/title/" + redar["netflixid"],
                Name = (string)redar["name"],
                Type = (string)redar["type"],
                Releasedate = (int)redar["releasedate"],
                Length = (string)redar["length"],
                Genres = (string)redar["topGenre"],
                Image = image,
            };

            int searchcounter = (int)redar["searchcounter"] + 1;

            redar.Close();

            RunCommand($"update netflixdata set searchcounter = '{searchcounter}' where id = '{movie.Id}'; ");

            cmd = Program._con.CreateCommand();
            cmd.CommandText = $"select * from totalreviews where netflixid = '{movie.Id}';";
            redar = cmd.ExecuteReader();

            if (redar.Read())
            {
                movie.AverageReview = Convert.ToInt32(redar["points"]) / Convert.ToInt32(redar["amount"]);
            }

            redar.Close();

            cmd = Program._con.CreateCommand();
            cmd.CommandText = $"select * from reviews where netflixid = '{movie.Id}' and userid = '{userid}';";
            redar = cmd.ExecuteReader();

            if (redar.Read())
            {
                movie.Hasreviewed = true;
                movie.Review = Convert.ToInt32(redar["points"]);
            }

            redar.Close();

            redar.Dispose();
            cmd.Dispose();
        }

        public static bool IsAvailable(string search)
        {
            if (search == "null")
            {
                return false;
            }

            if (Program._con.State.ToString() == "Closed")
            {
                Program._con.Open();
            }

            var cmd = Program._con.CreateCommand();
            cmd.CommandText = "select * from netflixdata where name = '" + search + "';";
            var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                reader.Close();
                return true;
            }
            reader.Close();
            return false;
        }

        public static bool IsAvailableId(string search)
        {
            if (!Tools.IsAvailable(search))
            {
                var cmd = Program._con.CreateCommand();
                cmd.CommandText = "select * from netflixdata where netflixid = '" + search + "';";
                var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    if ((string)reader["name"] == "null")
                    {
                        reader.Close();
                        return false;
                    }
                    reader.Close();
                }
                else
                {
                    reader.Close();
                    return false;
                }
            }
            return true;
        }

        public static void RunCommand(string command)
        {
            var cmd = Program._con.CreateCommand();
            cmd.CommandText = command;
            cmd.ExecuteNonQuery();

            cmd.Dispose();
        }

        public static bool Reader(string input)
        {
            var cmd = Program._con.CreateCommand();
            cmd.CommandText = input;
            var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                reader.Close();
                return true;
            }
            reader.Close();
            return false;
        }

        public static void Embedbuilder(string description, Color color, ISocketMessageChannel channel)
        {
            var eb = new EmbedBuilder();
            eb.WithColor(color);
            eb.WithDescription(description);
            channel.SendMessageAsync("", false, eb.Build());
        }

        public static void UpdateContentadded(IUser user)
        {
            var cmd = Program._con.CreateCommand();
            cmd.CommandText = $"select * from moderation where userid = '{user.Id}';";
            var reader = cmd.ExecuteReader();

            reader.Read();

            int contentadded = (int)reader["contentadded"];
            contentadded++;

            reader.Close();

            Tools.RunCommand($"update moderation set contentadded = '{contentadded}' where userid = '{user.Id}'; ");
        }

        public static bool IsModerator(IUser user)
        {
            var cmd = Program._con.CreateCommand();
            cmd.CommandText = $"select * from moderation where userid = '{user.Id}' and ismod = '{1}';";
            var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                reader.Close();
                return true;
            }
            reader.Close();
            return false;
        }

        public static void Search(string search, out EmbedBuilder eb, out FileStream stream, ulong userid)
        {
            GetMovieData(search, out MovieData movie, userid);

            stream = new FileStream("nedsofest.auni", FileMode.Create);
            eb = new EmbedBuilder();
            byte[] image = movie.Image;

            for (int i = 0; i < image.Length; i++)
            {
                stream.WriteByte(image[i]);
            }

            stream.Seek(0, SeekOrigin.Begin);

            eb.WithImageUrl("attachment://example.png");

            if (!movie.Hasreviewed)
            {
                eb.WithFooter(footer => footer.Text = "#" + movie.Id);
                eb.WithColor(Color.Blue);
            }
            else
            {
                eb.WithFooter(footer => footer.Text = "#" + movie.Id + " You rated this movie a " + movie.Review + "/10");
                eb.WithColor(Color.Green);
            }
     

            eb.WithTitle($"**{movie.Name.ToUpper()}**");

            string embedage;
            if (movie.Age == 0)
            {
                embedage = "All";
            }
            else
            {
                embedage = movie.Age.ToString() + "+";
            }

            if (movie.Genres == "null")
            {
                movie.Genres = "N/A";
            }

            if (movie.Type == "Movie")
            {
                eb.WithDescription("`" + movie.Releasedate.ToString() + "` `" + embedage + "`   `" + movie.Length + "`   \n `" + movie.Genres + "`");
            }
            else
            {
                eb.WithDescription("`" + movie.Releasedate.ToString() + "` `" + embedage + "`  `" + movie.Length + "` \n `" + movie.Genres + "`");
            }

            if (movie.Description == "null")
            {
                movie.Description = "N/A";
            }

            eb.AddField("About:", movie.Description);

            string text = "";

            if (movie.AverageReview == 0)
            {
                if (movie.Type == "Movie")
                {
                    text += "This movie has no reviews yet.";
                }
                else
                {
                    text += "This series has no reviews yet.";
                }
            }
            else
            {
                text += movie.AverageReview + "/10";
            }

            if (movie.Type == "Movie")
            {
                text += "\n watch the movie [here](" + movie.Link + ")";
            }
            else
            {
                text += "\n watch the series [here](" + movie.Link + ")";
            }

            eb.AddField("Review:", text);
        }
    }
}
