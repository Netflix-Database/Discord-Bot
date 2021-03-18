using Discord;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using System.Threading;

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
            cmd.CommandText = "select * from moviedata where movieName = '" + moviename + "';";
            var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                id = (int)reader["id"];
                reader.Close();
                return id;
            }
            reader.Close();
            return 0;
        }

        public static void ValidateSQLValues(string[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values[i].Length; j++)
                {
                    if (values[i][j] == ';')
                    {
                        values[i] = values[i].Substring(0, j) + values[i].Substring(j + 1 < values[i].Length ? j + 1 : values[i].Length - 1);
                    }

                    if (values[i][j] == '\'')
                    {
                        values[i] = values[i].Substring(0, j) + values[i].Substring(j + 1 < values[i].Length ? j + 1 : values[i].Length - 1);
                    }
                }
            }
        }

        public static string ValidateSQLValues(ref string values)
        {
            for (int j = 0; j < values.Length; j++)
            {
                if (values[j] == ';')
                {
                    values = values.Substring(0, j) + values.Substring(j + 1 < values.Length ? j + 1 : values.Length - 1);
                }

                if (values[j] == '\'')
                {
                    values = values.Substring(0, j) + values.Substring(j + 1 < values.Length ? j + 1 : values.Length - 1);
                }
            }

            return values;
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
        public static void GetMovieData(string search, out MovieData movie)
        {
            if (Program._con.State.ToString() == "Closed")
            {
                Program._con.Open();
            }

            var cmd = Program._con.CreateCommand();
            cmd.CommandText = "select * from moviedata where movieName = '" + search + "';";
            var redar = cmd.ExecuteReader();

            if (!redar.Read())
            {
                redar.Close();
                cmd = Program._con.CreateCommand();
                cmd.CommandText = "select * from moviedata where id = '" + search + "';";
                redar = cmd.ExecuteReader();
                redar.Read();
            }

            byte[] image;

            if (redar["image"] == DBNull.Value)
            {
                image = File.ReadAllBytes("NoImage.jpg");
            }
            else
            {
                image = (byte[])redar["image"];
            }


            movie = new MovieData
            {
                Age = (int)redar["age"],
                Description = (string)redar["description"],
                Id = (int)redar["id"],
                Link = ("https://www.netflix.com/search?q=" + redar["movieName"]).Replace(" ", ""),
                Name = (string)redar["movieName"],
                Type = Convert.ToBoolean(redar["type"]),
                Releasedate = (int)redar["releaseDate"],
                Length = (int)redar["movieLength"],
                Genres = (string)redar["genres"],
                AverageReview = ((int)redar["reviews"] != 0) ? (int)redar["reviewpoints"] / (int)redar["reviews"] : 0,
                Review = (int)redar["reviews"],
                Image = image
            };

            int searchcounter = (int)redar["searchcounter"] + 1;

            redar.Close();

            RunCommand($"update moviedata set searchcounter = '{searchcounter}' where id = '{movie.Id}'; ");
        }
        /// <summary>
        /// Turns the length into hours and minutes
        /// </summary>
        /// <param name="minutes"></param>
        /// <param name="hour"></param>
        /// <param name="min"></param>
        public static void GetTime(int minutes, out int hour, out int min)
        {
            hour = 0;

            for (int i = 0; i < minutes / 60 + 1; i++)
            {
                if (minutes >= 60)
                {
                    minutes -= 60;
                    hour++;
                }
            }

            min = minutes;
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
            cmd.CommandText = "select * from moviedata where MovieName = '" + search + "';";
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
                cmd.CommandText = "select * from moviedata where id = '" + search + "';";
                var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    if ((string)reader["movieName"] == "null")
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
            cmd.CommandText = $"select * from moderation where userid = '{user.Id}';";
            var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                reader.Close();
                return true;
            }
            reader.Close();
            return false;
        }

        public static void Search(string search, out EmbedBuilder eb, out FileStream stream, out string name)
        {
            GetMovieData(search, out MovieData movie);
            name = movie.Name;
            GetTime(movie.Length, out int hour, out int min);

            stream = new FileStream("nedsofest.auni", FileMode.Create);
            eb = new EmbedBuilder();
            byte[] image = movie.Image;

            for (int i = 0; i < image.Length; i++)
            {
                stream.WriteByte(image[i]);
            }

            stream.Seek(0, SeekOrigin.Begin);

            eb.WithImageUrl("attachment://example.png");

            eb.WithColor(Color.Blue);

            string embeddate;
            if (movie.Releasedate == 0)
            {
                embeddate = "N/A";
            }
            else
            {
                embeddate = movie.Releasedate.ToString();
            }

            string embedage;
            if (movie.Age == 0)
            {
                embedage = "N/A";
            }
            else
            {
                if (movie.Age == 69)
                {
                    embedage = "All";
                }
                else
                {
                    embedage = movie.Age.ToString() + "+";
                }
            }

            if (movie.Genres == "null")
            {
                movie.Genres = "N/A";
            }

            if (movie.Type == false)
            {
                if (movie.Length == 0)
                {
                    eb.WithDescription("`" + embeddate + "` `" + embedage + "`  `N/A` \n `" + movie.Genres + "`");
                }
                else if (hour == 0)
                {
                    eb.WithDescription("`" + embeddate + "` `" + embedage + "`  `" + min + "min` \n `" + movie.Genres + "`");
                }
                else if (min == 0)
                {
                    eb.WithDescription("`" + embeddate + "` `" + embedage + "`  `" + hour + "h` \n `" + movie.Genres + "`");
                }
                else
                {
                    eb.WithDescription("`" + embeddate + "` `" + embedage + "`  `" + hour + "h " + min + "min` \n `" + movie.Genres + "`");
                }
            }
            else
            {
                if (movie.Length == 0)
                {
                    eb.WithDescription("`" + embeddate + "` `" + embedage + "`  `N/A` \n `" + movie.Genres + "`");
                }
                else
                {
                    eb.WithDescription("`" + embeddate + "` `" + embedage + "`  `" + movie.Length + " Seasons` \n `" + movie.Genres + "`");
                }
            }

            if (movie.Description == "null")
            {
                movie.Description = "N/A";
            }

            eb.AddField("About:", movie.Description);

            string text = "";

            if (movie.Review == 0)
            {
                if (movie.Type)
                {
                    text += "This series has no reviews yet.";
                }
                else
                {
                    text += "This movie has no reviews yet.";
                }
            }
            else
            {
                text += movie.AverageReview + "/10 by " + movie.Review + " user/s";
            }

            if (!movie.Type)
            {
                text += "\n watch the movie [here](" + movie.Link + ")";
            }
            else
            {
                text += "\n watch the series [here](" + movie.Link + ")";
            }

            eb.AddField("Review:", text);

            eb.WithFooter(footer => footer.Text = "#" + movie.Id.ToString("D5"));
        }
    }
}
