using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.IO;
using System.Collections.Generic;
using Discord.Rest;

namespace Netdb
{
    public delegate void ErrorOccoured(Exception ex);
    public class Commands : ModuleBase<SocketCommandContext>
    {
        public event ErrorOccoured HandleError = Program.HandleError;

        [Command("ping")]
        [Alias("p")]
        [Summary("Shows the bots ping")]
        public async Task Ping()
        {
            await ReplyAsync("Pong! 🏓 **" + Program._client.Latency + "ms**");
        }

        [Command("search")]
        [Alias("s")]
        [Summary("Search for your favourite movies/series")]
        public async Task Search([Remainder]string search)
        {
            if (Tools.ValidateSQLValues(search, Context.Channel))
            {
                return;
            }

            if (search.Contains("'"))
            {
                search = search.Replace("'", "\\'");
            }

            if (!Tools.IsAvailableId(search))
            {
                Tools.Embedbuilder("This movie/series is not available", Color.DarkRed, Context.Channel);
                return;
            }

            ulong userid = 0;
            if (Context.Channel is IDMChannel)
            {
                userid = Context.User.Id;
            }

            Tools.Search(search, out EmbedBuilder eb, out FileStream stream, userid);

            await Context.Channel.SendFileAsync(stream, "example.png", embed: eb.Build());

            stream.Close();
        }

        [Command ("prefix")]
        [Summary ("Changes the Prefix for the server")]
        public async Task Prefix(string prefix)
        {
            if (Tools.ValidateSQLValues(prefix, Context.Channel))
            {
                return;
            }

            if (Context.Channel.GetType() == typeof(SocketDMChannel))
            {
                Tools.Embedbuilder("You can't use this in a dm",Color.DarkRed, Context.Channel);
                return;
            }

            if (prefix.Length == 0)
            {
                Tools.Embedbuilder("Prefix contains wrong characters",Color.DarkRed, Context.Channel);
            }

            if (prefix.Length <= 10)
            {
                if (((SocketGuildUser)Context.User).GuildPermissions.Administrator)
                {
                    PrefixManager.ChangePrefixForGuild(Context.Guild.Id, prefix);
                    Tools.Embedbuilder($"Prefix changed to `{prefix}`.", Color.Gold, Context.Channel);
                }
                else
                {
                    Tools.Embedbuilder($"You need to have administrator.", Color.Red, Context.Channel);
                }
            }
            else
            {
                Tools.Embedbuilder($"Prefix has a maximum length of 10.", Color.Red, Context.Channel);
            }
        }

        [Command("rate")]
        [Alias("r")]
        [Summary("With this command you can rate movies/series")]
        public async Task RateMovie(int points, [Remainder] string moviename)
        {
            if (Tools.ValidateSQLValues(moviename, Context.Channel))
            {
                return;
            }

            if (points < 1 || points > 10)
            {
                Tools.Embedbuilder("Your review has to be betweeen 1 and 10",Color.DarkRed, Context.Channel);
                return;
            }

            int id;

            if (!Tools.IsAvailable(moviename))
            {
                var cmd1 = Program._con.CreateCommand();
                cmd1.CommandText = $"select * from netflixdata where netflixid = '{moviename}';";
                var reader1 = await cmd1.ExecuteReaderAsync();

                if (!reader1.Read())
                {
                    Tools.Embedbuilder("This movie is not available", Color.DarkRed, Context.Channel);
                    reader1.Close();
                    return;
                }

                reader1.Close();
                id = Convert.ToInt32(moviename);
            }
            else
            {
                id = Tools.Getid(moviename);
            }

            if (Tools.Reader($"select * from reviews where netflixid = '{id}' and userid = '{Context.User.Id}';"))
            {
                Tools.Embedbuilder("You have already rated this movie", Color.DarkRed, Context.Channel);
                return;
            }

            Tools.RunCommand($"insert into reviews (userid, netflixid, points) values ('{Context.User.Id}', '{id}', '{points}');");

            var cmd = Program._con.CreateCommand();
            cmd.CommandText = $"select * from totalreviews where id = '{id}';";
            var reader = await cmd.ExecuteReaderAsync();

            if (reader.Read())
            {
                int reviews = (int)reader["amount"] + 1;
                int reviewpoints = (int)reader["points"] + points;

                reader.Close();

                Tools.RunCommand($"update totalreviews set amount = '{reviews}' where id = '{id}';");
                Tools.RunCommand($"update totalreviews set points = '{reviewpoints}' where id = '{id}';");
            }
            else
            {
                reader.Close();
                Tools.RunCommand($"insert into totalreviews (netflixid, amount, points) values ({id}, {1}, {points});");
            }

            Tools.Embedbuilder("Thanks for your review", Color.Green, Context.Channel);
        }

        [Command("recommend")]
        [Alias("rec")]
        [Summary("Recommend a movie to a certain user")]
        public async Task Recommend(string input, [Remainder]string search)
        {
            IUser user = null;

            if (ulong.TryParse(input, out ulong userid))
            {
                user = await Program._restClient.GetUserAsync(userid);
            }
            else if (input[input.Length - 5] == '#')
            {
                user = Program._client.GetUser(input.Split('#')[0], input.Split('#')[1]);
            }
            else if(input.StartsWith("<@!") && input.EndsWith('>'))
            {
                if (ulong.TryParse(input.Substring(3, 18), out userid))
                {
                    user = await Program._restClient.GetUserAsync(userid);
                }
            }

            if (user == null)
            {
                Tools.Embedbuilder("Can't find this user", Color.DarkRed, Context.Channel);
                return;
            }

            if (Tools.ValidateSQLValues(search, Context.Channel))
            {
                return;
            }

            if (user.IsBot)
            {
                Tools.Embedbuilder("You can't send recommendation to other bots", Color.DarkRed, Context.Channel);
                return;
            }

            if (!Tools.IsAvailableId(search))
            {
                Tools.Embedbuilder("This movie/series is not available", Color.DarkRed, Context.Channel);
                return;
            }

            Tools.Search(search,out EmbedBuilder eb, out FileStream stream, user.Id);

            eb.WithAuthor(Context.User);
            eb.WithTitle("Recommended:\n" + eb.Title);

            try
            {
                await user.SendFileAsync(stream, "example.png", embed: eb.Build());

                Tools.Embedbuilder("Recommendation sent successful",Color.Green, Context.Channel);
            }
            catch (Exception)
            {
                Tools.Embedbuilder("Can't send message to this user", Color.DarkRed, Context.Channel);
            }

            stream.Close();
            stream.Dispose();
        }

        [Command("watchlist")]
        [Alias("w")]
        [Summary("Add or remove movies to your watchlist")]
        public async Task Watchlist(string input, [Remainder] string moviename = null)
        {
            if (Tools.ValidateSQLValues(input + moviename, Context.Channel))
            {
                return;
            }

            if (input == "clear" || input == "c")
            {
                Tools.RunCommand($"delete from watchlistdata where userid = '{Context.User.Id}';");

                await Context.Message.AddReactionAsync(new Emoji("✅"));
                return;
            }
            else if (input == "list" || input == "l")
            {
                List<int> movieids = new List<int>();

                var cmd = Program._con.CreateCommand();
                cmd.CommandText = $"select * from watchlistdata where userid = '{Context.User.Id}';";
                var reader = cmd.ExecuteReader();

                EmbedBuilder eb = new EmbedBuilder();
                eb.WithColor(Color.DarkBlue);
                eb.WithAuthor(Context.User);
                eb.WithTitle("Watchlist");

                while (reader.Read())
                {
                    movieids.Add(Convert.ToInt32(reader["netflixid"]));
                }

                reader.Close();

                int pages = 1;

                if (movieids.Count > 9)
                {
                    pages = (int)Math.Ceiling((decimal)((double)movieids.Count / 9));
                }

                int page;
                if (moviename == null)
                {
                    page = 1;
                }
                else
                {
                    if (int.TryParse(moviename, out int result))
                    {
                        if (result > pages)
                        {
                            Tools.Embedbuilder("This page doesn't exist", Color.DarkRed, Context.Channel);
                            return;
                        }

                        page = result;
                    }
                    else
                    {
                        Tools.Embedbuilder("That's not a valid number", Color.DarkRed, Context.Channel);
                        return;
                    }
                }

                int tmp = 0;

                for (int i = 0; i < page - 1; i++)
                {
                    tmp += 9;
                }

                for (int i = tmp; i < tmp + 9; i++)
                {
                    if (i == movieids.Count)
                    {
                        break;
                    }

                    cmd = Program._con.CreateCommand();
                    cmd.CommandText = $"select * from netflixdata where netflixid = '{movieids[i]} ';";
                    reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        string link = "https://www.netflix.com/title/" + reader["netflixid"];

                        if ((string)reader["type"] == "TVSeries")
                        {
                            eb.AddField((string)reader["name_en"], " watch the series [here](" + link + ")");
                        }
                        else
                        {
                            eb.AddField((string)reader["name_en"], " watch the movie [here](" + link + ")");
                        }
                    }

                    reader.Close();
                }

                if (movieids.Count == 0)
                {
                    eb.WithDescription("There are currently no movies or series in your watchlist");
                }
                else
                {
                    eb.WithFooter("Page " + page + "/" + pages);
                }

                await Context.Channel.SendMessageAsync("", false, eb.Build());
                return;
            }

            if (Tools.IsAvailableId(moviename))
            {
                if (input == "add" || input == "a")
                {
                    if (Tools.Exists(Tools.Getid(moviename), Context.User.Id))
                    {
                        Tools.Embedbuilder("This movie is already in your watchlist", Color.DarkRed, Context.Channel);
                        return;
                    }

                    Tools.RunCommand($"insert into watchlistdata (netflixid, userid) values ('{Tools.Getid(moviename)}', '{Context.User.Id}');");

                    Tools.Embedbuilder("Added movie to your watchlist", Color.Green, Context.Channel);
                    return;
                }
                else if (input == "remove" || input == "r")
                {
                    if (Tools.Exists(Tools.Getid(moviename), Context.User.Id))
                    {
                        Tools.RunCommand($"delete from watchlistdata where userid = '{Context.User.Id}' and netflixid = '{Tools.Getid(moviename)}';");
                        await Context.Message.AddReactionAsync(new Emoji("✅"));
                        return;
                    }
                    else
                    {
                        Tools.Embedbuilder("This movie/series is not in your watchlist", Color.DarkRed, Context.Channel);
                        return;
                    }
                }
            }

            Tools.Embedbuilder("Couldn't find the movie", Color.DarkRed, Context.Channel);
        }

        [Command("top")]
        [Alias("t")]
        [Summary("Shows the searched list")]
        public async Task Top(int i)
        {
            Tools.Embedbuilder("This feature will be available soon. Keep rating your favourite movies/shows.", Color.Red, Context.Channel);
            return;

            EmbedBuilder eb = new EmbedBuilder();

            switch (i)
            {
                case 0:
                    eb.WithTitle("Top Reviewed");

                    var cmd = Program._con.CreateCommand();
                    cmd.CommandText = $"select * from bestreviewed;";
                    var reader = await cmd.ExecuteReaderAsync();

                    string movies = "";
                    string series = "";
                    int counter = 1;
                    while (reader.Read())
                    {
                        if (counter <= 25)
                        {
                            movies += counter + ". " + reader["name"] + " \n";
                        }
                        else if (counter > 50 && counter <= 75)
                        {
                            series += (counter - 50) + ". " + reader["name"] + " \n";
                        }

                        counter++;
                    }

                    reader.Close();

                    eb.AddField("Movies", movies);
                    eb.AddField("Series", series);

                    await Context.Channel.SendMessageAsync("", false, eb.Build());
                    break;
                case 1:
                    eb.WithTitle("Top Searched");

                    cmd = Program._con.CreateCommand();
                    cmd.CommandText = $"select * from mostsearched;";
                    reader = await cmd.ExecuteReaderAsync();

                    movies = "";
                    series = "";
                    counter = 1;
                    while (reader.Read())
                    {
                        if (counter <= 25)
                        {
                            movies += counter + ". " + reader["name"] + " \n";
                        }
                        else if (counter > 50 && counter <= 75)
                        {
                            series += (counter - 50) + ". " + reader["name"] + " \n";
                        }

                        counter++;
                    }

                    reader.Close();

                    eb.AddField("Movies", movies);
                    eb.AddField("Series", series);

                    await Context.Channel.SendMessageAsync("", false, eb.Build());
                    break;
                default:
                    break;
            }

            

           
        }

        [Command("subscribe")]
        [Alias("sc")]
        [Summary("When you are subscribed the bot will send you the newest movies daily")]
        public async Task Subscribe(string country = null)
        {
            var countries = new List<Tuple<string, string, string, IEmote>>();
            countries.Add(new Tuple<string, string, string, IEmote>("Austria", "AT", "de_AT", new Emoji("😂")));
            countries.Add(new Tuple<string, string, string, IEmote>("Germany", "DE", "de_DE", new Emoji("😂")));
            countries.Add(new Tuple<string, string, string,  IEmote>("USA", "US", "en_US", new Emoji("😂")));
            countries.Add(new Tuple<string, string, string, IEmote>("India", "IN", "en_IN", new Emoji("😂")));

            if (country == null)
            {
                var eb = new EmbedBuilder();
                eb.WithColor(Color.Blue);
                eb.WithAuthor("#subscribe {country}");
                eb.WithTitle("Countries");

                for (int i = 0; i < countries.Count; i++)
                {
                    eb.AddField(countries[i].Item1, countries[i].Item2);
                }

                await Context.Channel.SendMessageAsync("", false, eb.Build());
                return;
            }

            Tuple<string, string, string, IEmote> tuple = null;
            for (int i = 0; i < countries.Count; i++)
            {
                if (country.ToUpper() == countries[i].Item2)
                {
                    tuple = countries[i];
                }
            }

            if (tuple == null)
            {
                Tools.Embedbuilder("This country is not available", Color.DarkRed, Context.Channel);
                return;
            }

            if (Context.Channel.GetType() == typeof(SocketDMChannel))
            {
                if (Tools.Reader($"select null from subscriberdata where guildid = '{0}' and channelid = '{Context.User.Id}';"))
                {
                    Tools.Embedbuilder("You have already subscribed",Color.DarkRed, Context.Channel);
                    return;
                }

                Tools.RunCommand($"insert into subscriberdata (channelid, abostarted, guildid, country) values ('{Context.User.Id}', '{DateTime.Now.Date:yyyy-MM-dd}', '{0}', '{tuple.Item3}');");
            }
            else
            {
                IGuildUser user = (IGuildUser)Context.User;

                if (!user.GuildPermissions.Administrator)
                {
                    Tools.Embedbuilder("Only the server administators can use this command",Color.DarkRed, Context.Channel);
                    return;
                }

                var cmd = Program._con.CreateCommand();
                cmd.CommandText = $"select guildid, channelid from subscriberdata where guildid = '{Context.Guild.Id}';";
                var reader = await cmd.ExecuteReaderAsync();

                if (reader.Read())
                {
                    IGuildChannel channel = Program._client.GetGuild((ulong)(long)reader["guildid"]).GetChannel((ulong)(long)reader["channelid"]);

                    reader.Close();

                    Tools.Embedbuilder("This server already subscribed! See whats coming to Netflix in  " + MentionUtils.MentionChannel(channel.Id), Color.DarkRed, Context.Channel);
                    return;
                }
                reader.Close();

                Tools.RunCommand($"insert into subscriberdata (channelid, abostarted, guildid, country) values ('{Context.Channel.Id}', '{DateTime.Now.Date:yyyy-MM-dd}', '{Context.Guild.Id}', '{tuple.Item3}');");
            }

            Tools.Embedbuilder("You will now recieve an update about whats coming to Netflix every day", Color.Green, Context.Channel);
        }

        [Command("unsubscribe")]
        [Alias("usc")]
        [Summary("Meh")]
        public async Task Unsubscribe()
        {
            ulong id;

            if (Context.Channel.GetType() == typeof(SocketDMChannel))
            {
                id = Context.User.Id;
            }
            else
            {
                IGuildUser user = (IGuildUser)Context.User;

                if (!user.GuildPermissions.Administrator)
                {
                    Tools.Embedbuilder("Only the server administators can use this command", Color.DarkRed, Context.Channel);
                    return;
                }

                id = Context.Channel.Id;
            }
            
            Tools.RunCommand($"delete from subscriberdata where channelid = '{id}';");
            Tools.Embedbuilder("Ok :(",Color.Green, Context.Channel);
        }

        [Command("soon")]
        [Alias("so")]
        [Summary("Shows what's coming soon to Netflix")]
        public async Task Whatsnext()
        {
            Tools.Embedbuilder("This command will be available soon", Color.DarkRed, Context.Channel);
            return;

            EmbedBuilder eb = new EmbedBuilder();
            eb.WithColor(Color.LightOrange);
            eb.WithTitle("What's next?");

            bool content = false;
            int datesadded = 0;
            int counter = 0;

            do
            {
                DateTime releasedate = DateTime.Now.AddDays(counter);

                var cmd = Program._con.CreateCommand();
                cmd.CommandText = $"select * from comingsoon where releasedate = '{releasedate.Date:yyyy-MM-dd}';";
                var reader = await cmd.ExecuteReaderAsync();

                string movies = "";

                while (reader.Read())
                {
                    movies += reader["moviename"] + "\n";
                    content = true;
                }

                if (!reader.IsClosed)
                {
                    reader.Close();
                }

                if (movies != "")
                {
                    eb.AddField(releasedate.Date.ToString("MMMM dd"), movies);
                    datesadded++;
                }

                counter++;
            }
            while (datesadded < 5 && counter < 20);

            if (!content)
            {
                eb.WithDescription("There is nothing coming soon");
            }
            else
            {
                eb.WithDescription("Subscribe with `" + PrefixManager.GetPrefixFromGuildId(Context.Channel) + "sc` to get a daily notification about what's new");
            }

            await Context.Channel.SendMessageAsync("", false, eb.Build());
        }

        [Command("info")]
        [Alias("i")]
        [Summary("Gives information about the bot")]
        public async Task Info()
        {
            var eb = new EmbedBuilder();

            eb.WithColor(Color.Gold);
            eb.WithTitle("Netdb");
            eb.WithDescription("a simple Netflix bot");
            eb.AddField("Server", Context.Client.Guilds.Count);
            eb.AddField("Ping", Context.Client.Latency);
            eb.AddField("Users", Program.memberCount);
            eb.AddField("Currently in the database","movies: `" + Program.movies + "` \n series: `" + Program.series + "` \n reviews: `" + Program.reviews + "`");
            eb.AddField("Invite the Bot", "[here](https://discord.com/oauth2/authorize?client_id=802237562625196084&scope=bot&permissions=518208) \n [vote](https://top.gg/bot/802237562625196084/vote)");
            eb.WithFooter("made by Füreder Yannick and Traunbauer Elias");

            await Context.Channel.SendMessageAsync("", false, eb.Build());
        }

        [Command("report")]
        [Summary("Report bugs pls")]
        public async Task Report([Remainder] string report)
        {
            ISocketMessageChannel user = (ISocketMessageChannel)Program._client.GetChannel(835800158528733184);

            EmbedBuilder eb = new EmbedBuilder();
            eb.WithTitle("Report");
            eb.WithAuthor(Context.User);
            eb.WithColor(Color.DarkRed);
            eb.WithDescription(report);
            await user.SendMessageAsync("", false, eb.Build());

            Tools.Embedbuilder("Report sent successful",Color.Green,Context.Channel);
        }
    }
}
