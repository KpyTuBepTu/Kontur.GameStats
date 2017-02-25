using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Kontur.GameStats.Server
{
    public static class API
    {
        #region Parse Url
        public static Task<KeyValuePair<bool, string>> ParseUrl(string dbName, string rawUrl, string data, string method)
        {
            var splitUrl = rawUrl.Split('/');
            if (splitUrl.Length <= 2)
                return Task.Run(() =>
                {
                    return new KeyValuePair<bool, string>(false, "Very short URL: '" + rawUrl + "'");
                });

            switch (splitUrl[1])
            {
                case "servers":
                    return ServersBranch(dbName, splitUrl.Skip(2), data, method);
                case "reports":
                    return ReportBranch(dbName, splitUrl.Skip(2));
                case "players":
                    return PlayersBranch(dbName, splitUrl.Skip(2));
                default:
                    return Task.Run(() =>
                    {
                        return new KeyValuePair<bool, string>(false, "Nonexistent first part of URL: '" + splitUrl[1] + "'");
                    });
            }
        }

        private static Task<KeyValuePair<bool, string>> PlayersBranch(string dbName, IEnumerable<string> splitUrl)
        {
            if (splitUrl.Count() != 2 && splitUrl.Last() != "stats")
                return Task.Run(() => 
                {
                    return new KeyValuePair<bool, string>(false, "Something was wrong in '/players' query. Count after '/players': " + splitUrl.Count() + "  last part of URL: '" + splitUrl.Last() + "'");
                });

            return Task.Run(() => GetPlayerStats(dbName, splitUrl.First()));
        }

        private static Task<KeyValuePair<bool, string>> ReportBranch(string dbName, IEnumerable<string> splitUrl)
        {
            var splitUrlCount = splitUrl.Count();
            if (splitUrlCount > 2)
                return Task.Run(() =>
                {
                    return new KeyValuePair<bool, string>(false, "Unsupported length URL in '/reports'");
                });

            int recordCount = 5;
            switch (splitUrl.First())
            {
                case "recent-matches":
                    if (splitUrlCount == 1)
                        return Task.Run(() => GetRecentMatches(dbName, recordCount));
                    else
                        if (!int.TryParse(splitUrl.Last(), out recordCount))
                            return Task.Run(() =>
                            {
                                return new KeyValuePair<bool, string>(false, "'/count' not a number in '/reports/recent-matches'");
                            });
                        else
                            return Task.Run(() => GetRecentMatches(dbName, recordCount));

                case "best-players":
                    if (splitUrlCount == 1)
                        return Task.Run(() => GetBestPlayers(dbName, recordCount));
                    else
                        if (!int.TryParse(splitUrl.Last(), out recordCount))
                            return Task.Run(() =>
                            {
                                return new KeyValuePair<bool, string>(false, "'/count' not a number in '/reports/best-players'");
                            });
                        else
                            return Task.Run(() => GetBestPlayers(dbName, recordCount));

                case "popular-servers":
                    if (splitUrlCount == 1)
                        return Task.Run(() => GetPopularServers(dbName, recordCount));
                    else
                        if (!int.TryParse(splitUrl.Last(), out recordCount))
                            return Task.Run(() =>
                            {
                                return new KeyValuePair<bool, string>(false, "'/count' not a number in '/reports/popular-servers'");
                            });
                        else
                            return Task.Run(() => GetPopularServers(dbName, recordCount));
                default:
                    return Task.Run(() =>
                    {
                        return new KeyValuePair<bool, string>(false, "Nonexistent second part of '/reports': '" + splitUrl.First() + "'");
                    });
            }
        }

        private static Task<KeyValuePair<bool, string>> ServersBranchDefaultCase(string dbName, IEnumerable<string> splitUrl, string data, string method)
        {
            switch (splitUrl.Skip(1).First())
            {
                case "info":
                    if (splitUrl.Count() > 2)
                        return Task.Run(() =>
                        {
                            return new KeyValuePair<bool, string>(false, "Unsupported length URL in '/servers/<endpoint>/info'");
                        });
                    else
                        return method == "GET" ?
                            Task.Run(() => GetServerInfo(dbName, splitUrl.First()))
                            :
                            PutServerInfo(dbName, data, splitUrl.First());

                case "stats":
                    if (splitUrl.Count() > 2)
                        return Task.Run(() =>
                        {
                            return new KeyValuePair<bool, string>(false, "Unsupported length URL in '/servers/<endpoint>/stats'");
                        });
                    else
                        return Task.Run(() => GetServerStats(dbName, splitUrl.First()));

                case "matches":
                    if (splitUrl.Count() > 3)
                        return Task.Run(() =>
                        {
                            return new KeyValuePair<bool, string>(false, "Unsupported length URL in '/servers/<endpoint>/matches/<timestamp>'");
                        });
                    else
                    {
                        bool isTimestamp = Regex.IsMatch(splitUrl.Last(), @"^\d\d\d\d-(0?[1-9]|1[0-2])-(0?[1-9]|[12][0-9]|3[01])(T|t)(00|0[0-9]|1[0-9]|2[0-3]):([0-9]|[0-5][0-9]):([0-9]|[0-5][0-9])(Z|z)$");
                        if (isTimestamp)
                            return method == "GET" ?
                                Task.Run(() => GetMatchInfo(dbName, splitUrl.First(), splitUrl.Last()))
                                :
                                PutMatchInfo(dbName, data, splitUrl.First(), splitUrl.Last());
                        else
                            return Task.Run(() =>
                            {
                                return new KeyValuePair<bool, string>(false, "<timestamp>: '" + splitUrl.Last() + "' is not correct in '/servers/<endpoint>/matches'");
                            });
                    }

                default:
                    return Task.Run(() =>
                    {
                        return new KeyValuePair<bool, string>(false, "Nonexistent second part of '/servers': '" + splitUrl.Skip(1).First() + "'");
                    });
            }
        }

        private static Task<KeyValuePair<bool, string>> ServersBranch(string dbName, IEnumerable<string> splitUrl, string data, string method)
        {
            switch (splitUrl.First())
            {
                case "info":
                    if (splitUrl.Count() > 1)
                        return Task.Run(() =>
                        {
                            return new KeyValuePair<bool, string>(false, "Unsupported length URL in '/servers/info'");
                        });
                    else
                        return Task.Run(() => GetAllServersInfo(dbName));

                default:
                    // без примеров хостнеймов нет смысла проверять корресктность ип
                    //bool isIPv4 = Regex.IsMatch(splitUrl.First(), @"\b(?:[0-9]{1,3}\.){3}[0-9]{1,3}-[0-9]{1,5}\b");
                    //bool isHostname = Regex.IsMatch(splitUrl.First(), @"\b*{1,}-[0-9]{1,5}\b");
                    bool isRightPort = Regex.IsMatch(splitUrl.First(), @"\b*-[0-9]{1,5}\b");
                    if (isRightPort)
                        return ServersBranchDefaultCase(dbName, splitUrl, data, method);
                    else
                        return Task.Run(() =>
                        {
                            //return new KeyValuePair<bool, string>(false, "Second part of method '/servers': '" + splitUrl.First() + "' is not correct ipv4 or hostname");
                            return new KeyValuePair<bool, string>(false, "'/servers': '" + splitUrl.First() + "' port missing or not correct");
                        });
            }
        }
        #endregion

        #region PUT methods
        private static async Task<KeyValuePair<bool, string>> PutServerInfo(string dbName, string data, string endpoint)
        {
            ServerInfo serverInfo = JsonConvert.DeserializeObject<ServerInfo>(data);
            using (var db = new DbModel(dbName))
            {
                bool isServerExist = false;
                var server = db.Servers
                    .Where(serverCheck => serverCheck.Endpoint == endpoint)
                    .FirstOrDefault();

                if (server == default(Servers))
                    server = new Servers()
                    {
                        Name = serverInfo.name,
                        Endpoint = endpoint
                    };
                else
                {
                    isServerExist = true;
                    server.Name = serverInfo.name;
                    foreach (GameModes gm in db.GameModes)
                        gm.ServersSet.Remove(server);
                    server.Modes = new HashSet<GameModes>();
                }

                foreach (string mode in serverInfo.gameModes)
                {
                    var existMode = db.GameModes
                        .Where(m => m.Mode == mode)
                        .FirstOrDefault();
                    if (existMode == default(GameModes))
                    {
                        var newGameMode = new GameModes() { Mode = mode };
                        newGameMode.ServersSet.Add(server);
                        server.Modes.Add(newGameMode);
                    }
                    else
                    {
                        existMode.ServersSet.Add(server);
                        server.Modes.Add(existMode);
                    }
                }

                if (!isServerExist)
                    db.Servers.Add(server);
                await db.SaveChangesAsync();
            }

            return new KeyValuePair<bool, string>(true, "");
        }

        private static async Task<KeyValuePair<bool, string>> PutMatchInfo(string dbName, string data, string endpoint, string timestamp)
        {
            MatchInfo matchInfo = JsonConvert.DeserializeObject<MatchInfo>(data);
            using (var db = new DbModel(dbName))
            {
                var server = db.Servers
                    .Where(serverCheck => serverCheck.Endpoint == endpoint)
                    .FirstOrDefault();

                if (server == default(Servers))
                    return new KeyValuePair<bool, string>(false, "Server with enpoint = '" + endpoint + "' not found");

                var gameMode = server.Modes
                    .Where(mode => mode.Mode == matchInfo.gameMode)
                    .FirstOrDefault();

                if (gameMode == default(GameModes))
                    return new KeyValuePair<bool, string>(false, "This game mode [" + matchInfo.gameMode + "] is not support on server [" + server.Name + "]");

                var match = db.Matches
                    .Where(matchCheck => matchCheck.Server.Endpoint == endpoint && matchCheck.Timestamp.ToString() == timestamp)
                    .FirstOrDefault();

                if (match == default(Matches))
                    match = new Matches()
                    {
                        Server = server,
                        GameMode = gameMode,
                        Map = matchInfo.map,
                        FragLimit = matchInfo.fragLimit,
                        TimeLimit = matchInfo.timeLimit,
                        TimeElapsed = matchInfo.timeElapsed,
                        Timestamp = DateTime.Parse(timestamp).ToUniversalTime()
                    };
                else
                    return new KeyValuePair<bool, string>(false, "Match with equal timestamp are exist on this server");

                int playerPosition = 0;
                foreach (ScoreboardInfo si in matchInfo.scoreboard)
                {
                    var player = db.Players
                        .Where(playerCheck => playerCheck.Name.ToLower() == si.name.ToLower())
                        .FirstOrDefault();

                    if (player == default(Players))
                        player = new Players() { Name = si.name };

                    playerPosition++;
                    Scoreboards score = new Scoreboards()
                    {
                        Match = match,
                        Player = player,
                        Frags = si.frags,
                        Kills = si.kills,
                        Death = si.deaths,
                        ScoreboardPercent = ((double)(matchInfo.scoreboard.Count - playerPosition) / (double)(matchInfo.scoreboard.Count - 1)) * 100.0
                    };
                    player.Scores.Add(score);
                    match.Scoreboard.Add(score);
                    db.Scoreboards.Add(score);
                }

                server.Matches.Add(match);
                gameMode.Matches.Add(match);
                db.Matches.Add(match);
                await db.SaveChangesAsync();
            }

            return new KeyValuePair<bool, string>(true, "");
        }
        #endregion

        #region GET methods
        private static KeyValuePair<bool, string> GetAllServersInfo(string dbName)
        {
            using (var db = new DbModel(dbName))
            {
                var servers = db.Servers
                    .Select(server => new ServerFullInfo()
                    {
                        endpoint = server.Endpoint,
                        info = new ServerInfo()
                        {
                            name = server.Name,
                            gameModes = server.Modes
                                .Select(mode => mode.Mode)
                                .ToList()
                        }
                    })
                    .ToList();

                return new KeyValuePair<bool, string>(true, JsonConvert.SerializeObject(servers));
            }
        }

        private static KeyValuePair<bool, string> GetMatchInfo(string dbName, string endpoint, string timestamp)
        {
            using (var db = new DbModel(dbName))
            {
                var dt = DateTime.Parse(timestamp).ToUniversalTime();
                var match = db.Matches
                    .Where(matchCheck => matchCheck.Server.Endpoint == endpoint && matchCheck.Timestamp == dt)
                    .FirstOrDefault();

                if (match == default(Matches))
                    return new KeyValuePair<bool, string>(false, "Match with endpoint = '" + endpoint + "' and timestamp = '" + timestamp + "' not found");
                else
                {
                    MatchInfo mi = new MatchInfo()
                    {
                        map = match.Map,
                        gameMode = match.GameMode.Mode,
                        fragLimit = match.FragLimit,
                        timeLimit = match.TimeLimit,
                        timeElapsed = match.TimeElapsed,
                        scoreboard = match.Scoreboard
                            .Select(score => new ScoreboardInfo()
                            {
                                name = score.Player.Name,
                                frags = score.Frags,
                                kills = score.Kills,
                                deaths = score.Death
                            })
                            .ToList()
                    };

                    return new KeyValuePair<bool, string>(true, JsonConvert.SerializeObject(mi));
                }
            }
        }

        private static KeyValuePair<bool, string> GetServerInfo(string dbName, string endpoint)
        {
            using (var db = new DbModel(dbName))
            {
                var server = db.Servers
                    .Where(serverCheck => serverCheck.Endpoint == endpoint)
                    .FirstOrDefault();

                if (server == default(Servers))
                    return new KeyValuePair<bool, string>(false, "Server with enpoint = '" + endpoint + "' not found");
                else
                {
                    ServerInfo si = new ServerInfo()
                    {
                        name = server.Name,
                        gameModes = server.Modes.Select(mode => mode.Mode).ToList()
                    };

                    return new KeyValuePair<bool, string>(true, JsonConvert.SerializeObject(si));
                }
            }
        }

        private static KeyValuePair<bool, string> GetServerStats(string dbName, string endpoint)
        {
            using (var db = new DbModel(dbName))
            {
                var server = db.Servers
                    .Where(serverCheck => serverCheck.Endpoint == endpoint)
                    .FirstOrDefault();

                if (server == default(Servers))
                    return new KeyValuePair<bool, string>(false, "Server with enpoint = '" + endpoint + "' not found");
                else
                {
                    var matchesCountGroupByDate = server.Matches
                        .GroupBy(match => new DateTime(match.Timestamp.Year, match.Timestamp.Month, match.Timestamp.Day))
                        .Select(group => group.Count());

                    var playersCount = server.Matches
                        .Select(match => match.Scoreboard.Count);

                    ServerStats ss = new ServerStats()
                    {
                        totalMatchesPlayed = server.Matches.Count,
                        maximumMatchesPerDay = matchesCountGroupByDate.Count() == 0 ? 0 : matchesCountGroupByDate.Max(),
                        averageMatchesPerDay = matchesCountGroupByDate.Count() == 0 ? 0 : matchesCountGroupByDate.Average(),
                        maximumPopulation = playersCount.Count() == 0 ? 0 : playersCount.Max(),
                        averagePopulation = playersCount.Count() == 0 ? 0 : playersCount.Average(),
                        top5GameModes = GetTop5GameModes(server),
                        top5Maps = GetTop5Maps(server),
                    };

                    return new KeyValuePair<bool, string>(true, JsonConvert.SerializeObject(ss));
                }
            }
        }

        private static KeyValuePair<bool, string> GetPlayerStats(string dbName, string playerName)
        {
            using (var db = new DbModel(dbName))
            {
                var player = db.Players
                    .Where(playerCheck => playerCheck.Name.ToLower() == playerName.ToLower())
                    .FirstOrDefault();

                if (player == default(Players))
                    return new KeyValuePair<bool, string>(false, "Player name = '" + playerName + "' not found");
                else
                {
                    var uniqServers = player.Scores
                        .Select(score => score.Match.Server.Endpoint)
                        .GroupBy(endpoint => endpoint);

                    var matchesPerDayGroup = player.Scores
                            .Select(score => score.Match.Timestamp)
                            .GroupBy(time => new DateTime(time.Year, time.Month, time.Day));

                    PlayerStats ps = new PlayerStats()
                    {
                        totalMatchesPlayed = player.Scores.Count,
                        totalMatchesWon = player.Scores
                            .Where(score => score.ScoreboardPercent == 100.0)
                            .Count(),
                        favouriteServer = uniqServers
                            .OrderByDescending(group => group.Count())
                            .Select(group => group.Key)
                            .FirstOrDefault(),
                        uniqueServers = uniqServers.Count(),
                        favouriteMode = FindFavouriteMode(player),
                        averageScoreboardPercent = CalcAverageScoreboardPercent(player),
                        maximumMatchesPerDay = matchesPerDayGroup.Max(group => group.Count()),
                        averageMatchesPerDay = matchesPerDayGroup.Average(group => group.Count()),
                        lastMatchPlayed = FindLastMatchPlayed(player),
                        killToDeathRatio = CalcKDRatio(player)
                    };

                    return new KeyValuePair<bool, string>(true, JsonConvert.SerializeObject(ps));
                }
            }
        }

        private static KeyValuePair<bool, string> GetRecentMatches(string dbName, int count)
        {
            using (var db = new DbModel(dbName))
            {
                if (count <= 0)
                    return new KeyValuePair<bool, string>(true, "[]");
                if (count > 50)
                    count = 50;

                var recentMatches = db.Matches
                    .OrderByDescending(match => match.Timestamp)
                    .Take(count)
                    .Select(match => new MatchFullInfo()
                    {
                        server = match.Server.Endpoint,
                        timestamp = match.Timestamp,
                        results = new MatchInfo()
                        {
                            map = match.Map,
                            gameMode = match.GameMode.Mode,
                            fragLimit = match.FragLimit,
                            timeLimit = match.TimeLimit,
                            timeElapsed = match.TimeElapsed,
                            scoreboard = match.Scoreboard
                                .Select(score => new ScoreboardInfo()
                                {
                                    name = score.Player.Name,
                                    frags = score.Frags,
                                    kills = score.Kills,
                                    deaths = score.Death
                                })
                                .ToList()
                        }
                    })
                    .ToList();

                return new KeyValuePair<bool, string>(true, JsonConvert.SerializeObject(recentMatches));
            }
        }

        private static KeyValuePair<bool, string> GetBestPlayers(string dbName, int count)
        {
            using (var db = new DbModel(dbName))
            {
                if (count <= 0)
                    return new KeyValuePair<bool, string>(true, "[]");
                if (count > 50)
                    count = 50;

                var validPlayers = db.Players
                    .Where(player => player.Scores.Count > 9 && player.Scores.Select(score => score.Death).Sum() != 0)
                    .ToList();

                var bestPlayers = validPlayers
                    .Select(player => new BestPlayer()
                    {
                        name = player.Name,
                        killToDeathRatio = CalcKDRatio(player)
                    })
                    .OrderByDescending(bp => bp.killToDeathRatio)
                    .Take(count);

                return new KeyValuePair<bool, string>(true, JsonConvert.SerializeObject(bestPlayers));
            }
        }

        private static KeyValuePair<bool, string> GetPopularServers(string dbName, int count)
        {
            using (var db = new DbModel(dbName))
            {
                if (count <= 0)
                    return new KeyValuePair<bool, string>(true, "[]");
                if (count > 50)
                    count = 50;

                var servers = db.Servers.ToList();
                var popularServers = servers
                    .Select(server => new PopularServer()
                    {
                        endpoint = server.Endpoint,
                        name = server.Name,
                        averageMatchesPerDay = CalcAverageMatchesPerDay(server)
                    })
                    .OrderByDescending(server => server.averageMatchesPerDay)
                    .Take(count);
                return new KeyValuePair<bool, string>(true, JsonConvert.SerializeObject(popularServers));
            }
        }
        #endregion

        public static async void CreateErrorLogRecord(string dbName, string errorDescription, string url, string method)
        {
            using (var db = new DbModel(dbName))
            {
                Errors error = new Errors()
                {
                    Timestamp = DateTime.Now,
                    ErrorMessage = errorDescription,
                    Url = url,
                    Method = method
                };
                db.Errors.Add(error);
                await db.SaveChangesAsync();
            }
        }

        public static double CalcKDRatio(Players player)
        {
            int totalPlayerDeath = player.Scores.Sum(score => score.Death);
            return totalPlayerDeath == 0 ? 0 : (double)player.Scores.Sum(score => score.Kills) / (double)totalPlayerDeath;
        }

        public static double CalcAverageMatchesPerDay(Servers server)
        {
            return server.Matches.Count == 0 ? 0 : server.Matches
                                                            .Select(match => match.Timestamp)
                                                            .GroupBy(time => new DateTime(time.Year, time.Month, time.Day))
                                                            .Average(group => group.Count());
        }

        public static DateTime FindLastMatchPlayed(Players player)
        {
            return player.Scores
                .Select(score => score.Match.Timestamp)
                .OrderByDescending(timestamp => timestamp)
                .FirstOrDefault();
        }

        public static double CalcAverageScoreboardPercent(Players player)
        {
            return player.Scores
                .Average(score => score.ScoreboardPercent);
        }

        public static string FindFavouriteMode(Players player)
        {
            return player.Scores
                .Select(score => score.Match.GameMode.Mode)
                .GroupBy(mode => mode)
                .OrderByDescending(group => group.Count())
                .Select(group => group.Key)
                .FirstOrDefault();
        }

        public static string FindFavouriteServer(Players player)
        {
            return player.Scores
                .Select(score => score.Match.Server.Name)
                .GroupBy(name => name)
                .OrderByDescending(group => group.Count())
                .Select(group => group.Key)
                .FirstOrDefault();
        }

        public static List<string> GetTop5GameModes(Servers server)
        {
            return server.Matches
                .Select(match => match.GameMode.Mode)
                .GroupBy(mode => mode)
                .OrderByDescending(group => group.Count())
                .Select(group => group.Key)
                .Take(5)
                .ToList();
        }

        public static List<string> GetTop5Maps(Servers server)
        {
            return server.Matches
                .Select(match => match.Map)
                .GroupBy(map => map)
                .OrderByDescending(group => group.Count())
                .Select(group => group.Key)
                .Take(5)
                .ToList();
        }
    }

    #region Json classes
    public class PopularServer
    {
        public string endpoint { get; set; }
        public string name { get; set; }
        public double averageMatchesPerDay { get; set; }
    }

    public class BestPlayer
    {
        public string name { get; set; }
        public double killToDeathRatio { get; set; }
    }

    public class PlayerStats
    {
        public int totalMatchesPlayed { get; set; }
        public int totalMatchesWon { get; set; }
        public string favouriteServer { get; set; }
        public int uniqueServers { get; set; }
        public string favouriteMode { get; set; }
        public double averageScoreboardPercent { get; set; }
        public int maximumMatchesPerDay { get; set; }
        public double averageMatchesPerDay { get; set; }
        public DateTime lastMatchPlayed { get; set; }
        public double killToDeathRatio { get; set; }
    }

    public class ServerStats
    {
        public int totalMatchesPlayed { get; set; }
        public int maximumMatchesPerDay { get; set; }
        public double averageMatchesPerDay { get; set; }
        public int maximumPopulation { get; set; }
        public double averagePopulation { get; set; }
        public List<string> top5GameModes { get; set; }
        public List<string> top5Maps { get; set; }
    }

    public class ServerInfo
    {
        public string name { get; set; }
        public List<string> gameModes { get; set; }
    }

    public class ServerFullInfo
    {
        public string endpoint { get; set; }
        public ServerInfo info { get; set; }
    }

    public class MatchInfo
    {
        public string map { get; set; }
        public string gameMode { get; set; }
        public int fragLimit { get; set; }
        public int timeLimit { get; set; }
        public double timeElapsed { get; set; }
        public List<ScoreboardInfo> scoreboard { get; set; }
    }

    public class MatchFullInfo
    {
        public string server { get; set; }
        public DateTime timestamp { get; set; }
        public MatchInfo results { get; set; }
    }

    public class ScoreboardInfo
    {
        public string name { get; set; }
        public int frags { get; set; }
        public int kills { get; set; }
        public int deaths { get; set; }
    }
    #endregion
}