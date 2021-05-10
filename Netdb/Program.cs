using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Diagnostics;

namespace Netdb
{
    class Program
    {
        static List<EmbedBuilder> errors = new List<EmbedBuilder>();
        static int error;
        static Timer errorTimer;
        static Timer updateTimer;
        string connectionstring;
        public static string token;
        public static MySqlConnection _con;
        public static string mainPrefix = "#";

        //BotData
        public static int memberCount = 0;
        public static int movies = 0;
        public static int series = 0;
        public static int subscribers = 0;
        public static int reviews = 0;
        public static DateTime dailymessagetime = new DateTime(2004, 09, 29, 12, 0, 0, DateTimeKind.Local);

        public static string filepath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + Path.DirectorySeparatorChar;

        public static DateTime startedAt = DateTime.Now;
        public static int commandsexecuted = 0;

        static void Main() => new Program().RunBotAsync().GetAwaiter().GetResult();

        public static DiscordSocketClient _client;
        public static CommandService _commands;
        public static IServiceProvider _services;

        public static void HandleError(Exception ex)
        {
            try
            {
                StackTrace trace = new StackTrace(ex, true);
                var frame = trace.GetFrame(trace.FrameCount - 1);
                EmbedBuilder b = new EmbedBuilder().WithColor(Color.Red);
                string insight = "";
                b.WithTitle("Error");
                b.WithCurrentTimestamp();
                b.AddField("Exception type", ex.GetType().FullName);
                b.AddField("Message", ex.Message);
                b.AddField("Method", frame.GetMethod().Name);
                if (frame.GetFileName() == "" || frame.GetFileName() == null || frame.GetFileName().Remove(' ') == "")
                {
                    b.AddField("File", "-");
                }
                else
                {
                    b.AddField("File", frame.GetFileName());
                    string filePath = frame.GetFileName();
                    if (File.Exists(filePath))
                    {
                        string[] lines = new string[5];
                        var Read = File.ReadAllLines(filePath);
                        int longest = -1;
                        int longestIndex = 0;
                        for (int i = Math.Max(frame.GetFileLineNumber() - 3, 0); i < Math.Min(frame.GetFileLineNumber() + 2, Read.Length); i++)
                        {
                            lines[i - Math.Max(frame.GetFileLineNumber() - 3, 0)] = (i+1) + " " + Read[i];
                            if (lines[i - Math.Max(frame.GetFileLineNumber() - 3, 0)].Length > 60)
                            {
                                lines[i - Math.Max(frame.GetFileLineNumber() - 3, 0)] = lines[i - Math.Max(frame.GetFileLineNumber() - 3, 0)].Substring(0, 57) + "...";
                            }
                            if (lines[i - Math.Max(frame.GetFileLineNumber() - 3, 0)].Length > longest)
                            {
                                longestIndex = i - Math.Max(frame.GetFileLineNumber() - 3, 0);
                                longest = lines[i - Math.Max(frame.GetFileLineNumber() - 3, 0)].Length;
                            }
                        }

                        if (longest > 60)
                        {
                            longest = 60;
                        }

                        for (int i = 0; i < lines.Length; i++)
                        {
                            int cnt = longest - lines[i].Length;
                            for (int j = 0; j < cnt; j++)
                            {
                                lines[i] += " ";
                            }
                        }
                        insight = $"`{frame.GetFileName().Split('/')[frame.GetFileName().Split('/').Length - 1]}:\n{lines[0]}\n{lines[1]}\n{lines[2]}` <--- `\n{lines[3]}\n{lines[4]}`";
                    }
                }
                b.AddField("Line", frame.GetFileLineNumber().ToString());
                if (insight != "")
                {
                    b.AddField("Insight", insight);
                }
                else
                {
                    b.AddField("Insight", "NA");
                }
                errors.Add(b);
                Console.WriteLine("Error queued. Errors in queue: " + errors.Count);
            }
            catch (Exception)
            {
                Console.WriteLine("Error while reading stack trace");
            }
        }

        public async Task RunBotAsync()
        {
            try
            {
                _client = new DiscordSocketClient();
                _commands = new CommandService();

                _services = new ServiceCollection()
                    .AddSingleton(_client)
                    .AddSingleton(_commands)
                    .BuildServiceProvider();

                File.WriteAllText(filepath + "log.txt", "");
                _client.Log += Client_Log;

                _client.JoinedGuild += _client_JoinedGuild;

                _client.Ready += _client_Ready;

                await RegisterCommandsAsync();

                string[] input = new string[1];

                try
                {
                    input = File.ReadAllLines(filepath + "token.txt", Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    await Client_Log(new LogMessage(LogSeverity.Info, "System", "Couldn't get token and MYSQL connection string", ex));
                    HandleError(ex);
                    error++;
                }

                try
                {
                    token = input[1];
                    connectionstring = input[0];
                    _con = new MySqlConnection(connectionstring);

                    await _client.SetActivityAsync(new Game("Netflix", ActivityType.Watching));

                    await _client.LoginAsync(TokenType.Bot, token);
                }
                catch (Exception ex)
                {
                    HandleError(ex);
                    error++;
                }

                try
                {
                    _con.Open();

                    await Client_Log(new LogMessage(LogSeverity.Info, "System", "Database Connection Open"));
                }
                catch (Exception ex)
                {
                    await Client_Log(new LogMessage(LogSeverity.Info, "System", "Database Connection Closed"));
                    error++;
                    HandleError(ex);
                }

                try
                {
                    BackupDB();
                }
                catch (Exception ex)
                {
                    await Client_Log(new LogMessage(LogSeverity.Info, "System", "Couldn't create a new backup"));
                    error++;
                    HandleError(ex);
                }

                try
                {
                    //GetMostsearched();

                    //GetBestReviewed();

                    SetupDB();
                }
                catch (Exception ex)
                {
                    await Client_Log(new LogMessage(LogSeverity.Info, "System", "Setups aren't working"));
                    error++;
                    HandleError(ex);
                }

                await _client.StartAsync();

                updateTimer = new Timer((e) =>
                {
                    Perform5MinuteUpdate();
                }, null, TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(5));

                errorTimer = new Timer(OutputErrors, null, 10000, 1000);

                await Client_Log(new LogMessage(LogSeverity.Info, "System", "Error setup finished"));
            }
            catch (Exception exx)
            {
                HandleError(exx);
                error++;
            }
            await Task.Delay(-1);
        }

        public static void OutputErrors(object p)
        {
            if (errors.Count > 0)
            {
                var b = errors.ToArray()[0];
                errors.RemoveAt(0);
                ((ISocketMessageChannel)_client.GetChannel(835295047477231616)).SendMessageAsync("", false, b.Build()).GetAwaiter().GetResult();
            }
        }

        public static void Perform5MinuteUpdate()
        {
            memberCount = 0;
            movies = 0;
            series = 0;
            reviews = 0;
            subscribers = 0;

            var guilds = _client.Guilds;
            _client.DownloadUsersAsync(guilds);
            foreach (var item in guilds)
            {
                memberCount += item.MemberCount;
            }

            if (_con.State != System.Data.ConnectionState.Open)
            {
                return;
            }

            if (_con.Ping())
            {
                using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM netflixdata WHERE type='Movie'", _con))
                {
                    movies = Convert.ToInt32(cmd.ExecuteScalar());
                }

                using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM netflixdata WHERE type='TVSeries';", _con))
                {
                    series = Convert.ToInt32(cmd.ExecuteScalar());
                }

                using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM reviews;", _con))
                {
                    reviews = Convert.ToInt32(cmd.ExecuteScalar());
                }

                using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM subscriberlist;", _con))
                {
                    subscribers = Convert.ToInt32(cmd.ExecuteScalar());
                }

                Client_Log(new LogMessage(LogSeverity.Info, "System", "Updated Botdata"));
            }
        }

        public static void SetupDB()
        {
            var cmd = _con.CreateCommand();
            cmd.CommandText = "CREATE TABLE IF NOT EXISTS sys.commands (id INT NOT NULL AUTO_INCREMENT Primary Key,command VARCHAR(45) NULL,alias VARCHAR(10) NULL,short_description VARCHAR(100) NULL,syntax VARCHAR(100) NULL,mod_required TINYINT NULL,uses INT NULL);";
            cmd.ExecuteNonQuery();

            cmd = _con.CreateCommand();
            cmd.CommandText = "CREATE TABLE IF NOT EXISTS sys.prefixes (id INT NOT NULL AUTO_INCREMENT,guildId VARCHAR(10) NULL,prefix VARCHAR(10) NULL DEFAULT '#',PRIMARY KEY(id),UNIQUE INDEX id_UNIQUE (id ASC) VISIBLE,UNIQUE INDEX guildId_UNIQUE (guildId ASC) VISIBLE);";
            cmd.ExecuteNonQuery();

            cmd = _con.CreateCommand();
            cmd.CommandText = "CREATE TABLE IF NOT EXISTS `sys`.`netflixdata` (`id` INT NOT NULL AUTO_INCREMENT,`netflixid` INT NULL,`type` VARCHAR(45) NULL,`name` VARCHAR(200) NULL,`description` VARCHAR(500) NULL,`age` INT NULL,`releasedate` INT NULL,`topGenre` VARCHAR(45) NULL,`length` VARCHAR(45) NULL,`titleImg` BLOB NULL,`desktopImg` BLOB NULL,`mobileImg` BLOB NULL,`awards` VARCHAR(500) NULL,`downloadable` TINYINT NULL,`subtitles` VARCHAR(500) NULL,`audio` VARCHAR(2000) NULL,`emotions` VARCHAR(200) NULL,`creator` VARCHAR(300) NULL,`starring` VARCHAR(700) NULL,`cast` VARCHAR(2000) NULL,`allGenres` VARCHAR(500) NULL,PRIMARY KEY(`id`));";
            cmd.ExecuteNonQuery();

            cmd = _con.CreateCommand();
            cmd.CommandText = "CREATE TABLE IF NOT EXISTS `sys`.`reviews` (`id` INT NOT NULL AUTO_INCREMENT,`userid` BIGINT UNSIGNED NULL,`netflixid` INT UNSIGNED NULL,`points` TINYINT UNSIGNED NULL,PRIMARY KEY(`id`));";
            cmd.ExecuteNonQuery();  

            cmd = _con.CreateCommand();
            cmd.CommandText = "CREATE TABLE IF NOT EXISTS `sys`.`totalreviews` (`id` INT NOT NULL AUTO_INCREMENT,`netflixid` INT UNSIGNED NULL,`points` INT UNSIGNED NULL DEFAULT 0,`amount` INT UNSIGNED NULL DEFAULT 0,PRIMARY KEY(`id`));";
            cmd.ExecuteNonQuery();

            cmd = _con.CreateCommand();
            cmd.CommandText = "CREATE TABLE IF NOT EXISTS `sys`.`watchlistdata` (`id` INT NOT NULL AUTO_INCREMENT,`userid` BIGINT UNSIGNED NULL,`netflixid` INT UNSIGNED NULL,PRIMARY KEY(`id`),UNIQUE INDEX `id_UNIQUE` (`id` ASC) VISIBLE);";
            cmd.ExecuteNonQuery();

            cmd = _con.CreateCommand();
            cmd.CommandText = "CREATE TABLE IF NOT EXISTS `sys`.`subscriberdata` (`id` INT NOT NULL AUTO_INCREMENT,`channelid` BIGINT UNSIGNED NULL,`guildid` BIGINT UNSIGNED NULL,`abostarted` DATE NULL,`lastsent` DATE NULL,PRIMARY KEY(`id`),UNIQUE INDEX `id_UNIQUE` (`id` ASC) VISIBLE);";
            cmd.ExecuteNonQuery();

            cmd = _con.CreateCommand();
            cmd.CommandText = "CREATE TABLE IF NOT EXISTS `sys`.`moderation` (`id` INT NOT NULL AUTO_INCREMENT,`userid` BIGINT UNSIGNED NULL,`ismod` TINYINT UNSIGNED NULL,`contentadded` INT UNSIGNED NULL,`since` DATE NULL,PRIMARY KEY(`id`));";
            cmd.ExecuteNonQuery();

            cmd = _con.CreateCommand();
            cmd.CommandText = "CREATE TABLE IF NOT EXISTS `sys`.`comingsoon` (`id` INT NOT NULL AUTO_INCREMENT,`name` VARCHAR(100) NULL,`releasedate` DATE NULL,PRIMARY KEY(`id`));";
            cmd.ExecuteNonQuery();

            cmd.Dispose();
        }

        private async Task _client_Ready()
        {
            EmbedBuilder eb = new EmbedBuilder();
            eb.WithTitle("Bot started");

            if (error > 0)
            {
                if (error > 1)
                {
                    eb.WithDescription($"Bot started with {error} errors");
                }
                else
                {
                    eb.WithDescription($"Bot started with one error");
                }
            }
            else
            {
                eb.WithDescription("Bot started without errors");
            }
 
            eb.AddField("Database connection", _con.State);
            await ((ISocketMessageChannel)_client.GetChannel(835295047477231616)).SendMessageAsync("", false, eb.Build());
        }

        private async Task _client_JoinedGuild(SocketGuild arg)
        {
            EmbedBuilder eb = new EmbedBuilder();
            eb.WithColor(Color.Blue);
            eb.WithTitle("Hi");
            eb.WithDescription("use `#help` to get an overview of all commands");

            await arg.DefaultChannel.SendMessageAsync("", false, eb.Build());
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

            reader.Dispose();
            cmd.Dispose();
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

            reader.Dispose();
            cmd.Dispose();
        }

        public static void BackupDB()
        {
            try
            {
                string path = Path.Combine("DB_Backups", DateTime.Now.ToString().Replace(" ", "_").Replace(".", "_").Replace(":", "_").Replace("/","_") + "_backup.sql");

                MySqlCommand cmd = new MySqlCommand();
                MySqlBackup mb = new MySqlBackup(cmd);

                cmd.Connection = _con;

                mb.ExportToFile(filepath + path);

                mb.Dispose();
                cmd.Dispose();

                Client_Log(new LogMessage(LogSeverity.Info, "System", "Database Backup Created")).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            
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

            reader.Dispose();
            cmd.Dispose();
        }

        public static Task Client_Log(LogMessage arg)
        {
            Console.WriteLine(arg);
            File.AppendAllText(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + Path.DirectorySeparatorChar + "log.txt",DateTime.Now + ":  " + arg.Message + "  " + arg.Exception + "\n");
            return Task.CompletedTask;
        }

        public async Task RegisterCommandsAsync()
        {
            _client.MessageReceived += HandleCommandAsync;
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        }

        private async Task HandleCommandAsync(SocketMessage arg)
        {
            try {
            SocketUserMessage message = arg as SocketUserMessage;
            if (message == null)
            {
                return;
            }

                var context = new SocketCommandContext(_client, message);
                if (message.Author.IsBot) return;

            int argPos = 0;

                if (_con.State.ToString() == "Closed")
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

                string prefix = PrefixManager.GetPrefixFromGuildId(arg.Channel);

            if (message.HasStringPrefix(prefix, ref argPos) || message.HasStringPrefix("#", ref argPos))
            {
               

                CommandDB.CommandUsed(message.Content.Substring(prefix.Length).Split(" ")[0]);

                    var result = await _commands.ExecuteAsync(context, argPos, _services);
                    commandsexecuted++;

                    if (!result.IsSuccess)
                    {
                        await Client_Log(new LogMessage(LogSeverity.Info, "System", "Error while executing command  " + result.ErrorReason));

                        var eb = new EmbedBuilder();
                        eb.WithColor(Color.DarkRed);

                        if (result.ErrorReason == "Unknown command.")
                        {
                            eb.WithDescription("This command doesn't exist. Use " + prefix + "`help {commandname}` to see the exact syntax.");
                        }
                        else if (result.ErrorReason == "User not found.")
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
                        else if (result.ErrorReason == "The input text has too many parameters.")
                        {
                            eb.WithDescription("There are too many parameters");
                        }
                        else
                        {
                            eb.WithDescription("An error occured");
                        }

                        await message.Channel.SendMessageAsync("", false, eb.Build());
                    }

                    if (result.Error.Equals(CommandError.UnmetPrecondition)) await message.Channel.SendMessageAsync(result.ErrorReason);
                }
            }
            catch (Exception ex)
            {
                HandleError(ex);
            }
        }
    }
}