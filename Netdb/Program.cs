using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;

namespace Netdb
{
    class Program
    {
        public static string prefix = "#"; //prefix
        const string connectionstring = "Server=127.0.0.1;Database=sys;Uid=root;Pwd=Geheimnis123!;";

        public static MySqlConnection _con = new MySqlConnection(connectionstring);

        //Botstats
        public static DateTime startedAt = DateTime.Now;
        public static int commandsexecuted = 0;

        static void Main() => new Program().RunBotAsync().GetAwaiter().GetResult();

        public static DiscordSocketClient _client;
        public static CommandService _commands;
        public static IServiceProvider _services;

        public async Task RunBotAsync()
        {
            
            _client = new DiscordSocketClient();
            _commands = new CommandService();

            _services = new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(_commands)
                .BuildServiceProvider();

            _client.Log += Client_Log;     

            await RegisterCommandsAsync();

            string token = "ODAyMjM3NTYyNjI1MTk2MDg0.YAsT8w.CPTBECeOYiax-MgyUrvj27qaSfM";

            await _client.SetActivityAsync(new Game("Netflix", ActivityType.Watching));

            await _client.LoginAsync(TokenType.Bot, token);

            try
            {
                _con.Open();

                await Client_Log(new LogMessage(LogSeverity.Info, "System", "Database Connection Open"));

                BackupDB();

                GetMostsearched();

                GetBestReviewed();

                /*try
                {
                    GetMostPopular();
                }
                catch (Exception ex)
                {
                    throw ex;
                }*/
            }
            catch (Exception)
            {
                await Client_Log(new LogMessage(LogSeverity.Info,"System", "Database Connection Closed"));
            }

            await _client.StartAsync();

            DateTime time = new DateTime(2004,09,29,12,0,0,DateTimeKind.Local);

            await SendMessages(time);

            await Task.Delay(-1);
        }

        public static void GetMostsearched()
        {
            var cmd = _con.CreateCommand();
            cmd.CommandText = "SELECT * FROM moviedata ORDER BY searchcounter DESC";
            var reader = cmd.ExecuteReader();

            MovieData[] movies = new MovieData[50];
            MovieData[] series = new MovieData[50];

           for (int i = 0; i < 50; i++)
            {
                movies[i] = new MovieData();
                series[i] = new MovieData();
            }

            int moviecount = 0;
            int seriescount = 0;

            while (reader.Read() && (seriescount < 50 && moviecount < 50))
            {
                if ((int)reader["searchcounter"] != 0)
                {
                    if ((sbyte)reader["type"] == 0 && moviecount < 50)
                    {

                        movies[moviecount].Name = (string)reader["movieName"];
                        moviecount++;
                    }
                    else if ((sbyte)reader["type"] == 1 && seriescount < 50)
                    {
                        series[seriescount].Name = (string)reader["movieName"];
                        seriescount++;
                    }
                    else
                    {
                        Console.WriteLine("Type Unknown");
                    }
                }
            }

            reader.Close();

            for (int i = 0; i < movies.Length; i++)
            {
                cmd = Program._con.CreateCommand();
                cmd.CommandText = $"update mostsearched set name = '{movies[i].Name}' where id = '{i + 1}';";
                cmd.ExecuteNonQuery();

                cmd = Program._con.CreateCommand();
                cmd.CommandText = $"update mostsearched set name = '{series[i].Name}' where id = '{i + 51}';";
                cmd.ExecuteNonQuery();
            }
        }

        public static void GetMostPopular()
        {
            var cmd = _con.CreateCommand();
            cmd.CommandText = "SELECT * FROM bestreviewed";
            var reader = cmd.ExecuteReader();

            MovieData[] bestreviewedmovies = new MovieData[50];
            MovieData[] bestreviewedseries = new MovieData[50];

            int count = 0;
            while (reader.Read())
            {
                bestreviewedmovies[count].Name = (string)reader["name"];
                bestreviewedseries[count + 50].Name = (string)reader["name"];

                count++;
            }

            reader.Close();

            cmd = _con.CreateCommand();
            cmd.CommandText = "SELECT * FROM mostsearched";
            reader = cmd.ExecuteReader();

            MovieData[] mostsearchedmovies = new MovieData[50];
            MovieData[] mostsearchedseries = new MovieData[50];

            count = 0;
            while (reader.Read())
            {
                mostsearchedmovies[count].Name = (string)reader["name"];
                mostsearchedseries[count].Name = (string)reader["name"];

                count++;
            }

            reader.Close();

            MovieData[] popularmovies = new MovieData[50];
            MovieData[] popularseries = new MovieData[50];




        }

        public static void GetBestReviewed()
        {
            var cmd = _con.CreateCommand();
            cmd.CommandText = "SELECT * FROM moviedata ORDER BY reviews DESC";
            var reader = cmd.ExecuteReader();

            MovieData[] movies = new MovieData[50];
            MovieData[] series = new MovieData[50];

            for (int i = 0; i < 50; i++)
            {
                movies[i] = new MovieData();
                series[i] = new MovieData();
            }

            int moviecount = 0;
            int seriescount = 0;

            while (reader.Read() && (seriescount < 50 && moviecount < 50))
            {

                if ((int)reader["reviews"] != 0)
                {
                    if ((sbyte)reader["type"] == 0 && moviecount < 50)
                    {
                        movies[moviecount].Name = (string)reader["movieName"];
                        movies[moviecount].Review = (int)reader["reviews"];
                        movies[moviecount].AverageReview = (int)reader["reviewpoints"] / (int)reader["reviews"];
                        moviecount++;
                    }
                    else if ((sbyte)reader["type"] == 1 && seriescount < 50)
                    {
                        series[seriescount].Name = (string)reader["movieName"];
                        series[seriescount].Review = (int)reader["reviews"];
                        series[seriescount].AverageReview = (int)reader["reviewpoints"] / (int)reader["reviews"];
                        seriescount++;
                    }
                    else
                    {
                        Console.WriteLine("Type Unknown");
                    }
                }
            }

            reader.Close();

            int mostmoviereviews = movies[0].Review;
            int mostseriesreviews = series[0].Review;

            for (int i = 0; i < moviecount; i++)
            {
                movies[i].Totalreview = (movies[i].AverageReview / 10.0) * 100 * 0.7;
                movies[i].Totalreview += ((double)movies[i].Review / (double)mostmoviereviews * 100) * 0.3;

                series[i].Totalreview = (series[i].AverageReview / 10.0) * 100 * 0.7;
                series[i].Totalreview += ((double)series[i].Review / (double)mostseriesreviews * 100) * 0.3;
            }

            for (int write = 0; write < movies.Length; write++)
            {
                for (int sort = 0; sort < movies.Length - 1; sort++)
                {
                    if (movies[sort].Totalreview < movies[sort + 1].Totalreview)
                    {
                        MovieData temp = movies[sort + 1];
                        movies[sort + 1] = movies[sort];
                        movies[sort] = temp;
                    }
                }
            }

            for (int write = 0; write < series.Length; write++)
            {
                for (int sort = 0; sort < series.Length - 1; sort++)
                {
                    if (series[sort].Totalreview < series[sort + 1].Totalreview)
                    {
                        MovieData temp = series[sort + 1];
                        series[sort + 1] = series[sort];
                        series[sort] = temp;
                    }
                }
            }

            for (int i = 0; i < movies.Length; i++)
            {
                cmd = Program._con.CreateCommand();
                cmd.CommandText = $"update bestreviewed set name = '{movies[i].Name}' where id = '{i + 1}';";
                cmd.ExecuteNonQuery();

                cmd = Program._con.CreateCommand();
                cmd.CommandText = $"update bestreviewed set name = '{series[i].Name}' where id = '{i + 51}';";
                cmd.ExecuteNonQuery();
            }
        }

        public static void BackupDB()
        {
            string file = "DB_Backups/" + DateTime.Now.ToString().Replace(" ", "_").Replace(".", "_").Replace(":","_") + "_backup.sql";
            MySqlCommand cmd = new MySqlCommand();
            MySqlBackup mb = new MySqlBackup(cmd);

            cmd.Connection = _con;

            mb.ExportToFile(file);

            Client_Log(new LogMessage(LogSeverity.Info, "System", "Database Backup Created")).GetAwaiter().GetResult();
        }
         
        private async Task SendMessages(DateTime executiontime)
        {
            int waitingtime = (int)executiontime.TimeOfDay.Subtract(DateTime.Now.TimeOfDay).TotalMilliseconds;

            if (waitingtime < 0)
            {
                return;
            }

            await Task.Delay(waitingtime);

            var cmd = Program._con.CreateCommand();
            cmd.CommandText = $"select * from comingsoon where releasedate = '{DateTime.Now.Date:yyyy-MM-dd}';";
            var reader = cmd.ExecuteReader();

            EmbedBuilder eb = new EmbedBuilder();
            eb.WithColor(Color.Blue);
            eb.WithTitle("Today's releases");
            eb.WithCurrentTimestamp();

            string content = "";

            while(reader.Read())
            {
                content += reader["moviename"] + "\n";
            }

            reader.Close();

            eb.AddField(DateTime.Now.Date.ToString("MMMM dd"),content);

            cmd = Program._con.CreateCommand();
            cmd.CommandText = $"select * from subscriberlist;";
            reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                if ((ulong)(long)reader["guildid"] != 0)
                {
                    ITextChannel channel = _client.GetGuild((ulong)(long)reader["guildid"]).GetTextChannel((ulong)(long)reader["channelid"]);
                    await channel.SendMessageAsync("", false, eb.Build());
                }
                else
                {
                    IUser user = _client.GetUser((ulong)(long)reader["channelid"]);
                    await user.SendMessageAsync("", false, eb.Build());
                }
            }

            reader.Close();

            cmd = Program._con.CreateCommand();
            cmd.CommandText = $"update subscriberlist set lastupdated = '{DateTime.Now:yyyy-MM-dd}'";
            cmd.ExecuteNonQuery();
        }

        public static Task Client_Log(LogMessage arg)
        {
            Console.WriteLine(arg);
            return Task.CompletedTask;
        }

        public async Task RegisterCommandsAsync()
        {
            _client.MessageReceived += HandleCommandAsync;
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        }

        private async Task HandleCommandAsync(SocketMessage arg)
        {
            var message = arg as SocketUserMessage;
            var context = new SocketCommandContext(_client, message);
            if (message.Author.IsBot) return;

            int argPos = 0;
            if (message.HasStringPrefix(prefix, ref argPos))
            {
                if (_con.State.ToString() == "Closed" && !message.Content.Contains("botstats"))
                {
                    try
                    {
                        _con.Open();
                    }
                    catch (Exception)
                    {
                        var eb = new EmbedBuilder();
                        eb.WithColor(Color.DarkRed);
                        eb.WithDescription("The databse is currently offline. Try again later.");

                        await message.Channel.SendMessageAsync("", false, eb.Build());
                        return;
                    }
                }

                var result = await _commands.ExecuteAsync(context, argPos, _services);
                commandsexecuted++;

                if (!result.IsSuccess)
                {
                    Console.WriteLine(result.ErrorReason);

                    var eb = new EmbedBuilder();
                    eb.WithColor(Color.DarkRed);

                    if (result.ErrorReason == "Unknown command.")
                    {
                        eb.WithDescription("This command doesn't exist. Use " + prefix + "`help {commandname}` to see the exact syntax.");
                    }
                    else if(result.ErrorReason == "User not found.")
                    {
                        eb.WithDescription("Couldn't find this user");
                    }
                    else if (result.ErrorReason == "Failed to parse Int32.")
                    {
                        eb.WithDescription("wrong number");
                    }
                    else if (result.ErrorReason == "The input text has too few parameters.")
                    {
                        eb.WithDescription("Something is missing");
                    }
                    else
                    {
                        eb.WithDescription("An error occured");
                    }


                    var msg = await message.Channel.SendMessageAsync("", false, eb.Build());

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    Delete(msg, 5);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                }

                if (result.Error.Equals(CommandError.UnmetPrecondition)) await message.Channel.SendMessageAsync(result.ErrorReason);
            }
        }

        // zum löschen von nachrichten
        public async Task Delete(IMessage msg, int delay)
        {
            await Task.Delay(delay * 1000);
            await msg.DeleteAsync();

        }
    }
}