using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Kontur.GameStats.Server
{
    internal static class API
    {
        //internal static Task PutUrlDefinition(string rawUrl, string data)
        //{
        //    var splitUrl = rawUrl.Split('/');
        //    switch (splitUrl[1])
        //    {
        //        case "servers":
        //            {
        //                if (splitUrl.Length < 3)
        //                    throw new Exception("Неизвестный PUT метод в /servers");

        //                bool isIPv4 = Regex.IsMatch(splitUrl[2], @"\b(?:[0-9]{1,3}\.){3}[0-9]{1,3}-[0-9]{1,5}\b");
        //                bool isHostname = Regex.IsMatch(splitUrl[2], @"\b([a-z0-9]+(-[a-z0-9]+)*\.)+[a-z]{2,}-[0,9]{1,5}\b");
        //                if (isIPv4 || isHostname)
        //                {
        //                    switch (splitUrl[3])
        //                    {
        //                        case "info":
        //                            return Task.Run(() => PutServerInfo(data, splitUrl[2]));
        //                        case "matches":
        //                            {
        //                                if (splitUrl.Length < 5)
        //                                    throw new Exception("Не указан <timestamp>");
        //                                if (splitUrl.Length >= 6)
        //                                    throw new Exception("Неизвестный PUT метод в /servers/<endpoint>/matches/");

        //                                bool isTimestamp = Regex.IsMatch(splitUrl[4], @"^\d\d\d\d-(0?[1-9]|1[0-2])-(0?[1-9]|[12][0-9]|3[01])(T|t)(00|[0-9]|1[0-9]|2[0-3]):([0-9]|[0-5][0-9]):([0-9]|[0-5][0-9])(Z|z)$");
        //                                if (isTimestamp)
        //                                    return Task.Run(() => PutMatchInfo(data, splitUrl[2], splitUrl[4]));
        //                                else
        //                                    throw new Exception("<timestamp> не соответствует шаблону");
        //                            }
        //                        default:
        //                            throw new Exception("Неизвестный PUT метод в /servers/<endpoint>/");
        //                    }
        //                }
        //                else
        //                    throw new Exception("<endpoint> не соответствует шаблону");
        //            }
        //        default:
        //            throw new Exception("Неизвестный PUT метод");
        //    }
        //}

        internal static Task<string> UrlDefinition(string rawUrl, string data, string method)
        {
            var splitUrl = rawUrl.Split('/');
            if (splitUrl.Length == 2)
                throw new Exception("Похоже этот метод в другом замке");

            switch (splitUrl[1])
            {
                case "servers":
                    return ServersBranch(splitUrl.Skip(2), data, method);
                case "reports":
                    return ReportBranch(splitUrl.Skip(2));
                case "players":
                    return PlayersBranch(splitUrl.Skip(2));
                default:
                    throw new Exception("Похоже этот метод в другом замке");
            }
        }

        private static Task<string> PlayersBranch(IEnumerable<string> splitUrl)
        {
            if (splitUrl.Count() != 2 && splitUrl.Last() != "stats")
                throw new Exception("Похоже этот метод в другом замке");

            return Task.Run(() => GetPlayerStats(splitUrl.First()));
        }

        private static Task<string> ReportBranch(IEnumerable<string> splitUrl)
        {
            var splitUrlCount = splitUrl.Count();
            if (splitUrlCount > 2)
                throw new Exception("Похоже этот метод в другом замке");

            int recordCount = 5;
            switch (splitUrl.First())
            {
                case "recent-matches":
                    {
                        if (splitUrlCount == 1)
                            return Task.Run(() => GetRecentMatches(recordCount));
                        else
                        {
                            if (!int.TryParse(splitUrl.Last(), out recordCount))
                                throw new Exception("Похоже этот метод в другом замке");
                            else
                                return Task.Run(() => GetRecentMatches(recordCount));
                        }
                    }
                case "best-players":
                    {
                        if (splitUrlCount == 1)
                            return Task.Run(() => GetRecentMatches(recordCount));
                        else
                        {
                            if (!int.TryParse(splitUrl.Last(), out recordCount))
                                throw new Exception("Похоже этот метод в другом замке");
                            else
                                return Task.Run(() => GetBestPlayers(recordCount));
                        }
                    }
                case "popular-servers":
                    {
                        if (splitUrlCount == 1)
                            return Task.Run(() => GetRecentMatches(recordCount));
                        else
                        {
                            if (!int.TryParse(splitUrl.Last(), out recordCount))
                                throw new Exception("Похоже этот метод в другом замке");
                            else
                                return Task.Run(() => GetPopularServers(recordCount));
                        }
                    }
                default:
                    throw new Exception("Похоже этот метод в другом замке");
            }
        }

        private static Task<string> ServersBranchDefaultCase(IEnumerable<string> splitUrl, string data, string method)
        {
            switch (splitUrl.Skip(1).First())
            {
                case "info":
                    {
                        if (splitUrl.Count() > 2)
                            throw new Exception("Похоже этот метод в другом замке");
                        else
                            return method == "GET" ? Task.Run(() => GetServerInfo(splitUrl.First())) : Task.Run(() => PutServerInfo(data, splitUrl.First()));
                    }
                case "stats":
                    {
                        if (splitUrl.Count() > 2)
                            throw new Exception("Похоже этот метод в другом замке");
                        else
                            return Task.Run(() => GetServerStats(splitUrl.First()));
                    }
                case "matches":
                    {
                        if (splitUrl.Count() > 3)
                            throw new Exception("Похоже этот метод в другом замке");
                        else
                        {
                            bool isTimestamp = Regex.IsMatch(splitUrl.Last(), @"^\d\d\d\d-(0?[1-9]|1[0-2])-(0?[1-9]|[12][0-9]|3[01])(T|t)(00|[0-9]|1[0-9]|2[0-3]):([0-9]|[0-5][0-9]):([0-9]|[0-5][0-9])(Z|z)$");
                            if (isTimestamp)
                                return method == "GET" ? Task.Run(() => GetMatchInfo(splitUrl.First(), splitUrl.Last())) : Task.Run(() => PutMatchInfo(data, splitUrl.First(), splitUrl.Last()));
                            else
                                throw new Exception("<timestamp> не соответствует шаблону");
                        }
                    }
                default:
                    throw new Exception("Похоже этот метод в другом замке");
            }
        }

        private static Task<string> ServersBranch(IEnumerable<string> splitUrl, string data, string method)
        {
            switch (splitUrl.First())
            {
                case "info":
                    {
                        if (splitUrl.Count() > 1)
                            throw new Exception("Похоже этот метод в другом замке");

                        return Task.Run(() => GetAllServersInfo());
                    }
                default:
                    {
                        bool isIPv4 = Regex.IsMatch(splitUrl.First(), @"\b(?:[0-9]{1,3}\.){3}[0-9]{1,3}-[0-9]{1,5}\b");
                        bool isHostname = Regex.IsMatch(splitUrl.First(), @"\b([a-z0-9]+(-[a-z0-9]+)*\.)+[a-z]{2,}-[0,9]{1,5}\b");
                        if (isIPv4 || isHostname)
                            return ServersBranchDefaultCase(splitUrl, data, method);
                        else
                            throw new Exception("Похоже этот метод в другом замке");
                    }
            }
        }

        private static string GetAllServersInfo()
        {
            using (var db = new DbModel("stats.db"))
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

                return JsonConvert.SerializeObject(servers);
            }
        }

        private static string GetMatchInfo(string endpoint, string timestamp)
        {
            using (var db = new DbModel("stats.db"))
            {
                var match = db.Matches
                    .Where(matchCheck => matchCheck.Server.Endpoint == endpoint && matchCheck.Timestamp == DateTime.Parse(timestamp))
                    .FirstOrDefault();
                if (match == default(Matches))
                    throw new Exception("Запрашиваемый матч не найден");
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

                    return JsonConvert.SerializeObject(mi);
                }
            }
        }

        private static string GetServerInfo(string endpoint)
        {
            using (var db = new DbModel("stats.db"))
            {
                var server = db.Servers
                    .Where(serverCheck => serverCheck.Endpoint == endpoint)
                    .FirstOrDefault();
                if (server == default(Servers))
                    throw new Exception("Такого сервера нет в базе данных");
                else
                {
                    ServerInfo si = new ServerInfo()
                    {
                        name = server.Name,
                        gameModes = server.Modes.Select(mode => mode.Mode).ToList()
                    };

                    return JsonConvert.SerializeObject(si);
                }
            }
        }

        private static string PutServerInfo(string data, string endpoint)
        {
            ServerInfo serverInfo = JsonConvert.DeserializeObject<ServerInfo>(data);
            using (var db = new DbModel("stats.db"))
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
                db.SaveChangesAsync();
            }

            return "";
        }

        private static string PutMatchInfo(string data, string endpoint, string timestamp)
        {
            MatchInfo matchInfo = JsonConvert.DeserializeObject<MatchInfo>(data);
            using (var db = new DbModel("stats.db"))
            {
                var server = db.Servers
                    .Where(serverCheck => serverCheck.Endpoint == endpoint)
                    .FirstOrDefault();
                if (server == default(Servers))
                    throw new Exception("Сервера с таким <endpoint> нет в базе данных");

                var gameMode = db.Servers
                    .SelectMany(serverCheck => serverCheck.Modes)
                    .Where(modeCheck => modeCheck.Mode == matchInfo.gameMode)
                    .FirstOrDefault();
                if (gameMode == default(GameModes))
                    throw new Exception("На этом сервере нет такого игрового мода");

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
                        Timestamp = DateTime.Parse(timestamp)
                    };
                else
                    throw new Exception("Результаты этого матча уже присутствуют в базе");

                foreach (ScoreboardInfo si in matchInfo.scoreboard)
                {
                    var player = db.Players
                        .Where(playerCheck => playerCheck.Name.ToLower() == si.name.ToLower())
                        .FirstOrDefault();
                    if (player == default(Players))
                        player = new Players() { Name = si.name };

                    Scoreboards score = new Scoreboards()
                    {
                        Match = match,
                        Player = player,
                        Frags = si.frags,
                        Kills = si.kills,
                        Death = si.deaths
                    };
                    player.Scores.Add(score);
                    match.Scoreboard.Add(score);
                    db.Scoreboards.Add(score);
                }

                server.Matches.Add(match);
                gameMode.Matches.Add(match);
                db.Matches.Add(match);
                db.SaveChangesAsync();
            }

            return "";
        }

        private static string GetServerStats(string endpoint)
        {
            using (var db = new DbModel("stats.db"))
            {
                var server = db.Servers.Where(serverCheck => serverCheck.Endpoint == endpoint).FirstOrDefault();
                if (server == default(Servers))
                    throw new Exception("Данного сервера нет в базе данных");
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
                        maximumMatchesPerDay = matchesCountGroupByDate.Max(),
                        averageMatchesPerDay = matchesCountGroupByDate.Average(),
                        maximumPopulation = playersCount.Max(),
                        averagePopulation = playersCount.Average(),
                        top5GameModes = server.Matches
                            .Select(match => match.GameMode.Mode)
                            .GroupBy(mode => mode)
                            .OrderByDescending(group => group.Count())
                            .Select(group => group.Key)
                            .Take(5)
                            .ToList(),
                        top5Maps = server.Matches
                            .Select(match => match.Map)
                            .GroupBy(map => map)
                            .OrderByDescending(group => group.Count())
                            .Select(group => group.Key)
                            .Take(5)
                            .ToList(),
                    };

                    return JsonConvert.SerializeObject(ss);
                }
            }
        }

        private static string GetPlayerStats(string playerName)
        {
            using (var db = new DbModel("stats.db"))
            {
                var player = db.Players.Where(playerCheck => playerCheck.Name.ToLower() == playerName.ToLower()).FirstOrDefault();
                if (player == default(Players))
                    throw new Exception("Данного игрока нет в базе данных");
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
                        totalMatchesWon = db.Scoreboards
                            .GroupBy(score => score.Match)
                            .Select(group => group.FirstOrDefault())
                            .Where(score => score.Player.Name.ToLower() == playerName.ToLower())
                            .Count(),
                        favouriteServer = uniqServers
                            .OrderByDescending(group => group.Count())
                            .Select(group => group.Key)
                            .FirstOrDefault(),
                        uniqueServers = uniqServers.Count(),
                        favouriteMode = player.Scores
                            .Select(score => score.Match.GameMode.Mode)
                            .GroupBy(mode => mode)
                            .OrderByDescending(group => group.Count())
                            .Select(group => group.Key)
                            .FirstOrDefault(),
                        averageScoreboardPercent = player.Scores
                            .Select(score =>
                            {
                                List<Scoreboards> scores = score.Match.Scoreboard.ToList();
                                int playerBelow = 0;
                                while (scores[scores.Count - playerBelow - 1].Player.Name != player.Name)
                                    playerBelow++;
                                return playerBelow / (scores.Count - 1) * 100;
                            })
                            .Average(),
                        maximumMatchesPerDay = matchesPerDayGroup.Max(group => group.Count()),
                        averageMatchesPerDay = matchesPerDayGroup.Average(group => group.Count()),
                        lastMatchPlayed = player.Scores
                            .OrderByDescending(score => score.Match.Timestamp)
                            .Select(score => score.Match.Timestamp)
                            .FirstOrDefault(),
                        killToDeathRatio = player.Scores
                            .Select(score => score.Kills)
                            .Sum() / player.Scores
                            .Select(score => score.Death)
                            .Sum()
                    };

                    return JsonConvert.SerializeObject(ps);
                }
            }
        }

        private static string GetRecentMatches(int count)
        {
            using (var db = new DbModel("stats.db"))
            {
                if (count <= 0)
                    return "[]";
                //int count2 = count;
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

                return JsonConvert.SerializeObject(recentMatches);
            }
        }

        private static string GetBestPlayers(int count)
        {
            using (var db = new DbModel("stats.db"))
            {
                if (count <= 0)
                    return "[]";
                if (count > 50)
                    count = 50;

                var validPlayers = db.Players.Where(player => player.Scores.Count > 9 && player.Scores.Select(score => score.Death).Sum() != 0);
                var bestPlayers = validPlayers
                    .Select(player => new BestPlayer()
                    {
                        name = player.Name,
                        killToDeathRatio = player.Scores
                            .Select(score => score.Kills)
                            .Sum() / player.Scores
                            .Select(score => score.Death)
                            .Sum()
                    })
                    .OrderByDescending(bp => bp.killToDeathRatio)
                    .Take(count);

                return JsonConvert.SerializeObject(bestPlayers);
            }
        }

        private static string GetPopularServers(int count)
        {
            using (var db = new DbModel("stats.db"))
            {
                if (count <= 0)
                    return "[]";
                if (count > 50)
                    count = 50;

                var servers = db.Servers.ToList();
                var popularServers = servers
                    .Select(server => new PopularServer()
                    {
                        endpoint = server.Endpoint,
                        name = server.Name,
                        averageMatchesPerDay = server.Matches
                            .Select(match => match.Timestamp)
                            .GroupBy(time => new DateTime(time.Year, time.Month, time.Day))
                            .Average(group => group.Count())
                    })
                    .OrderByDescending(server => server.averageMatchesPerDay)
                    .Take(count);
                return JsonConvert.SerializeObject(popularServers);
            }
        }
    }

    internal class PopularServer
    {
        public string endpoint { get; set; }
        public string name { get; set; }
        public double averageMatchesPerDay { get; set; }
    }

    internal class BestPlayer
    {
        public string name { get; set; }
        public double killToDeathRatio { get; set; }
    }

    internal class PlayerStats
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

    internal class ServerStats
    {
        public int totalMatchesPlayed { get; set; }
        public int maximumMatchesPerDay { get; set; }
        public double averageMatchesPerDay { get; set; }
        public int maximumPopulation { get; set; }
        public double averagePopulation { get; set; }
        public List<string> top5GameModes { get; set; }
        public List<string> top5Maps { get; set; }
    }

    internal class ServerInfo
    {
        public string name { get; set; }
        public List<string> gameModes { get; set; }
    }

    internal class ServerFullInfo
    {
        public string endpoint { get; set; }
        public ServerInfo info { get; set; }
    }

    internal class MatchInfo
    {
        public string map { get; set; }
        public string gameMode { get; set; }
        public int fragLimit { get; set; }
        public double timeLimit { get; set; }
        public double timeElapsed { get; set; }
        public List<ScoreboardInfo> scoreboard { get; set; }
    }

    internal class MatchFullInfo
    {
        public string server { get; set; }
        public DateTime timestamp { get; set; }
        public MatchInfo results { get; set; }
    }

    internal class ScoreboardInfo
    {
        public string name { get; set; }
        public int frags { get; set; }
        public int kills { get; set; }
        public int deaths { get; set; }
    }
}
