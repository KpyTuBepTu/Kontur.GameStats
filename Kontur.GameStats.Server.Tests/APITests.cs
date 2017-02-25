using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Kontur.GameStats.Server;

namespace Kontur.GameStats.Server.Tests
{
    [TestClass()]
    public class APITests
    {
        private List<string> servers = new List<string>()
        {
            "192.168.1.1-1337",
            "127.0.0.1-1488",
            "localhost.com-322",
            "serverhub.ru-228",
            "nefti.net-42"
        };

        private List<string> maps = new List<string>()
        {
            "Lancre",
            "Klatch",
            "Uberwald",
            "XXXX",
            "Agatean Empire",
            "Tsort",
            "Urawebe",
            "Omnia",
            "Laotan",
            "Djellybaby",
            "Borogravia"
        };

        private List<string> gameModes =  new List<string>()
        {
            "DM",
            "TDM",
            "CTF",
            "Conquest",
            "Rush",
            "Domination"
        };

        private List<string> playerNames = new List<string>()
        {
            "PapaMama",
            "Ubica228",
            "Alex666",
            "Tpycuku",
            "xXxNogebatorrRxXx",
            "qwe123da",
            "Pa3DBAPa3",
            "DeaDPeRDeaD",
            "Kakaval",
            "Olol'ewa",
            "-=PlahinSuperHero=-",
            "MsFartBoner",
            "MisterTwister",
            "SweetyBybalex",
            "PonySlaystation",
            "1TonyMontana1"
        };

        private void DeleteTestDb()
        {
            if (File.Exists("APITestDb.db"))
                File.Delete("APITestDb.db");
        }

        private ServerInfo CreateTestServerInfo()
        {
            Random rnd = new Random();
            ServerInfo si = new ServerInfo()
            {
                name = "My server #" + rnd.Next(1, 100),
                gameModes = new List<string>()
            };
            int gameModeCount = rnd.Next(1, 7);
            List<string> remainingGM = new List<string>(gameModes);
            for (int i = 0; i < gameModeCount; i++)
            {
                int mode = rnd.Next(0, remainingGM.Count);
                si.gameModes.Add(remainingGM[mode]);
                remainingGM.RemoveAt(mode);
            }

            return si;
        }

        private MatchInfo CreateTestMatchInfo()
        {
            Random rnd = new Random();
            MatchInfo mi = new MatchInfo()
            {
                map = maps[rnd.Next(0, 11)],
                fragLimit = 30,
                gameMode = gameModes[rnd.Next(0, 6)],
                timeLimit = 20,
                timeElapsed = 10.0,
                scoreboard = new List<ScoreboardInfo>()
            };
            int playersCount = rnd.Next(2, playerNames.Count);
            List<string> remainingPlayers = new List<string>(playerNames);
            for (var i = 0; i < playersCount; i++)
            {
                var playerIndex = rnd.Next(0, remainingPlayers.Count);
                mi.scoreboard.Add(new ScoreboardInfo()
                {
                    name = remainingPlayers[playerIndex],
                    frags = mi.fragLimit - i,
                    kills = mi.fragLimit - i + rnd.Next(0, 11),
                    deaths = rnd.Next(0, 61)
                });
                remainingPlayers.RemoveAt(playerIndex);
            }

            return mi;
        }

        private void CreateTestDb()
        {
            Random rnd = new Random();
            List<string> remainingEndpoints = new List<string>(servers);
            for (int i = 0; i < servers.Count; i++)
            {
                int serverIndex = rnd.Next(0, remainingEndpoints.Count);
                var task = API.ParseUrl("APITestDb.db", "/servers/" + remainingEndpoints[serverIndex] + "/info", JsonConvert.SerializeObject(CreateTestServerInfo()), "PUT");
                task.Wait();
                remainingEndpoints.RemoveAt(serverIndex);
            }
            int matchesCount = rnd.Next(100, 201);
            for (int i = 0; i < matchesCount; i++)
            {
                string server = servers[rnd.Next(0, servers.Count)];
                string dt = DateTime.Now.AddHours(i).ToString("s") + "Z";
                var task = API.ParseUrl("APITestDb.db", "/servers/" + server + "/matches/" + dt, JsonConvert.SerializeObject(CreateTestMatchInfo()), "PUT");
                task.Wait();
                if (task.Result.Key == false)
                    API.CreateErrorLogRecord("APITestDb.db", task.Result.Value, "url", "PUT");
                    
            }
        }

        [TestMethod()]
        public void CreateDb_PUT_Servers_and_Matches()
        {
            DeleteTestDb();
            CreateTestDb();
            Assert.IsTrue(File.Exists("APITestDb.db"));
        }

        [TestMethod()]
        public void ParseUrl_ShortUrlError()
        {
            string rawUrl = "/servers";

            var getResult = API.ParseUrl("APITestDb.db", rawUrl, "", "GET");
            getResult.Wait();

            Assert.AreEqual(false, getResult.Result.Key);
        }

        [TestMethod()]
        public void ParseUrl_UnknowFirstPartUrlError()
        {
            string rawUrl = "/blablabla/192.168.1.1-1337/info";

            var getResult = API.ParseUrl("APITestDb.db", rawUrl, "", "GET");
            getResult.Wait();

            Assert.AreEqual(false, getResult.Result.Key);
        }

        [TestMethod()]
        public void ParseUrl_Servers_GetAllServersInfo_UrlLengthError()
        {
            string rawUrl = "/servers/info/";

            var getResult = API.ParseUrl("APITestDb.db", rawUrl, "", "GET");
            getResult.Wait();

            Assert.AreEqual(false, getResult.Result.Key);
        }

        [TestMethod()]
        public void GetAllServersInfo_OK()
        {
            string rawUrl = "/servers/info";

            var getResult = API.ParseUrl("APITestDb.db", rawUrl, "", "GET");
            getResult.Wait();

            Assert.AreEqual(true, getResult.Result.Key);
        }

        [TestMethod()]
        public void ParseUrl_Servers_NotCorrectEndpoint_WrongPortNumber()
        {
            string rawUrl = "/servers/192.168.1.1-133737/info";

            var getResult = API.ParseUrl("APITestDb.db", rawUrl, "", "GET");
            getResult.Wait();

            Assert.AreEqual(false, getResult.Result.Key);
        }

        [TestMethod()]
        public void ParseUrl_Servers_NotCorrectEndpoint_PortNotFound()
        {
            string rawUrl = "/servers/192.168.1.1/info";

            var getResult = API.ParseUrl("APITestDb.db", rawUrl, "", "GET");
            getResult.Wait();

            Assert.AreEqual(false, getResult.Result.Key);
        }

        [TestMethod()]
        public void GetServerInfo_OK()
        {
            Random rnd = new Random();
            string rawUrl = "/servers/" + servers[rnd.Next(0, servers.Count)] + "/info";

            var getResult = API.ParseUrl("APITestDb.db", rawUrl, "", "GET");
            getResult.Wait();

            Assert.AreEqual(true, getResult.Result.Key);
        }

        [TestMethod()]
        public void ParseUrl_Servers_GetServerInfo_UrlLengthError()
        {
            Random rnd = new Random();
            string rawUrl = "/servers/" + servers[rnd.Next(0, servers.Count)] + "/info/";

            var getResult = API.ParseUrl("APITestDb.db", rawUrl, "", "GET");
            getResult.Wait();

            Assert.AreEqual(false, getResult.Result.Key);
        }

        [TestMethod()]
        public void ParseUrl_Servers_GetServerInfo_EndpointNotFound()
        {
            string rawUrl = "/servers/123.123.123.123-123/info";

            var getResult = API.ParseUrl("APITestDb.db", rawUrl, "", "GET");
            getResult.Wait();

            Assert.AreEqual(false, getResult.Result.Key);
        }

        [TestMethod()]
        public void GetServerStats_OK()
        {
            Random rnd = new Random();
            string rawUrl = "/servers/" + servers[rnd.Next(0, servers.Count)] + "/stats";

            var getResult = API.ParseUrl("APITestDb.db", rawUrl, "", "GET");
            getResult.Wait();

            Assert.AreEqual(true, getResult.Result.Key);
        }

        [TestMethod()]
        public void ParseUrl_Servers_GetServerStats_UrlLengthError()
        {
            Random rnd = new Random();
            string rawUrl = "/servers/" + servers[rnd.Next(0, servers.Count)] + "/stats/";

            var getResult = API.ParseUrl("APITestDb.db", rawUrl, "", "GET");
            getResult.Wait();

            Assert.AreEqual(false, getResult.Result.Key);
        }

        [TestMethod()]
        public void ParseUrl_Servers_GetServerStats_EndpointNotFound()
        {
            string rawUrl = "/servers/123.123.123.123-123/stats";

            var getResult = API.ParseUrl("APITestDb.db", rawUrl, "", "GET");
            getResult.Wait();

            Assert.AreEqual(false, getResult.Result.Key);
        }

        [TestMethod()]
        public void GetMatchInfo_OK()
        {
            ServerInfo si = new ServerInfo()
            {
                name = "HandTestServer",
                gameModes = new List<string>()
            };
            for (int i = 0; i < gameModes.Count; i++)
                si.gameModes.Add(gameModes[i]);

            MatchInfo mi = new MatchInfo()
            {
                map = maps[0],
                fragLimit = 30,
                gameMode = gameModes[0],
                timeLimit = 20,
                timeElapsed = 10.0,
                scoreboard = new List<ScoreboardInfo>()
            };
            mi.scoreboard.Add(new ScoreboardInfo()
            {
                name = playerNames[0],
                frags = 30,
                kills = 31,
                deaths = 11
            });
            mi.scoreboard.Add(new ScoreboardInfo()
            {
                name = playerNames[1],
                frags = 25,
                kills = 29,
                deaths = 24
            });
            mi.scoreboard.Add(new ScoreboardInfo()
            {
                name = playerNames[2],
                frags = 20,
                kills = 20,
                deaths = 19
            });
            mi.scoreboard.Add(new ScoreboardInfo()
            {
                name = playerNames[3],
                frags = 15,
                kills = 17,
                deaths = 19
            });
            mi.scoreboard.Add(new ScoreboardInfo()
            {
                name = playerNames[4],
                frags = 10,
                kills = 10,
                deaths = 25
            });

            string serverEndpoint = si.name + "-12345";
            string dt = "2018-01-01T00:00:00Z";
            string rawUrl = "/servers/" + serverEndpoint + "/matches/" + dt;

            var putServer = API.ParseUrl("APITestDb.db", "/servers/" + serverEndpoint + "/info", JsonConvert.SerializeObject(si), "PUT");
            putServer.Wait();
            var putMatch = API.ParseUrl("APITestDb.db", rawUrl, JsonConvert.SerializeObject(mi), "PUT");
            putMatch.Wait();
            var getResult = API.ParseUrl("APITestDb.db", rawUrl, "", "GET");
            getResult.Wait();

            Assert.AreEqual(JsonConvert.SerializeObject(mi), getResult.Result.Value);
        }

        [TestMethod()]
        public void ParseUrl_Servers_GetMatchInfo_UrlLengthError()
        {
            Random rnd = new Random();
            string rawUrl = "/servers/" + servers[rnd.Next(0, servers.Count)] + "/matches/2017-01-22T15:17:00Z/";

            var getResult = API.ParseUrl("APITestDb.db", rawUrl, "", "GET");
            getResult.Wait();

            Assert.AreEqual(false, getResult.Result.Key);
        }

        [TestMethod()]
        public void ParseUrl_Servers_GetMatchInfo_NotCorrectTimestamp()
        {
            Random rnd = new Random();
            string rawUrl = "/servers/" + servers[rnd.Next(0, servers.Count)] + "/matches/20170122Tт151700з";

            var getResult = API.ParseUrl("APITestDb.db", rawUrl, "", "GET");
            getResult.Wait();

            Assert.AreEqual(false, getResult.Result.Key);
        }

        [TestMethod()]
        public void ParseUrl_Servers_GetMatchInfo_MatchNotFound()
        {
            Random rnd = new Random();
            string rawUrl = "/servers/" + servers[rnd.Next(0, servers.Count)] + "/matches/1970-01-22T15:17:00Z";

            var getResult = API.ParseUrl("APITestDb.db", rawUrl, "", "GET");
            getResult.Wait();

            Assert.AreEqual(false, getResult.Result.Key);
        }

        [TestMethod()]
        public void ParseUrl_Servers_NotCorrectSecondPart()
        {
            string rawUrl = "/servers/blablabla/matches/1970-01-22T15:17:00Z";

            var getResult = API.ParseUrl("APITestDb.db", rawUrl, "", "GET");
            getResult.Wait();

            Assert.AreEqual(false, getResult.Result.Key);
        }

        [TestMethod()]
        public void ParseUrl_Reports_UrlLengthError()
        {
            string rawUrl = "/reports/popular-servers/10/";

            var getResult = API.ParseUrl("APITestDb.db", rawUrl, "", "GET");
            getResult.Wait();

            Assert.AreEqual(false, getResult.Result.Key);
        }

        [TestMethod()]
        public void ParseUrl_Reports_CountNotANumberError()
        {
            string rawUrl = "/reports/popular-servers/blablabla";

            var getResult = API.ParseUrl("APITestDb.db", rawUrl, "", "GET");
            getResult.Wait();

            Assert.AreEqual(false, getResult.Result.Key);
        }

        [TestMethod()]
        public void GetPopularServers_OK()
        {
            string rawUrl = "/reports/popular-servers";

            var getResult = API.ParseUrl("APITestDb.db", rawUrl, "", "GET");
            getResult.Wait();

            Assert.AreEqual(true, getResult.Result.Key);
        }

        [TestMethod()]
        public void GetBestPlayers_OK()
        {
            string rawUrl = "/reports/best-players";

            var getResult = API.ParseUrl("APITestDb.db", rawUrl, "", "GET");
            getResult.Wait();

            Assert.AreEqual(true, getResult.Result.Key);
        }

        [TestMethod()]
        public void GetRecentMatches_OK()
        {
            string rawUrl = "/reports/recent-matches";

            var getResult = API.ParseUrl("APITestDb.db", rawUrl, "", "GET");
            getResult.Wait();

            Assert.AreEqual(true, getResult.Result.Key);
        }

        [TestMethod()]
        public void ParseUrl_Reports_NotCorrectSecondPart()
        {
            string rawUrl = "/reports/blablabla";

            var getResult = API.ParseUrl("APITestDb.db", rawUrl, "", "GET");
            getResult.Wait();

            Assert.AreEqual(false, getResult.Result.Key);
        }

        [TestMethod()]
        public void ParseUrl_Players_UrlError()
        {
            Random rnd = new Random();
            string rawUrl = "/players/" + playerNames[rnd.Next(0, playerNames.Count)] + "/stats/";
            var getResult = API.ParseUrl("APITestDb.db", rawUrl, "", "GET");
            getResult.Wait();

            Assert.AreEqual(false, getResult.Result.Key);
        }

        [TestMethod()]
        public void GetPlayerStats_OK()
        {
            Random rnd = new Random();
            string rawUrl = "/players/" + playerNames[rnd.Next(0, playerNames.Count)] + "/stats";
            var getResult = API.ParseUrl("APITestDb.db", rawUrl, "", "GET");
            getResult.Wait();

            Assert.AreEqual(true, getResult.Result.Key);
        }

        [TestMethod()]
        public void CalcKDRatioTest_TotalDeathsAboveZero()
        {
            Players testPlayer = new Players()
            {
                Id = 0,
                Name = "TestPlayer1",
                Scores = new List<Scoreboards>(),
            };
            testPlayer.Scores.Add(new Scoreboards()
            {
                Frags = 30,
                Kills = 31,
                Death = 11,
                ScoreboardPercent = 100.0
            });

            double kdr = 31.0 / 11.0;

            Assert.AreEqual(API.CalcKDRatio(testPlayer), kdr);
        }

        [TestMethod()]
        public void CalcKDRatioTest_TotalDeathsBelowZero()
        {
            Players testPlayer = new Players()
            {
                Id = 0,
                Name = "TestPlayer1",
                Scores = new List<Scoreboards>(),
            };
            testPlayer.Scores.Add(new Scoreboards()
            {
                Frags = 30,
                Kills = 31,
                Death = 0,
                ScoreboardPercent = 100.0
            });
            testPlayer.Scores.Add(new Scoreboards()
            {
                Frags = 25,
                Kills = 29,
                Death = 0,
                ScoreboardPercent = 66.0
            });
            testPlayer.Scores.Add(new Scoreboards()
            {
                Frags = 20,
                Kills = 20,
                Death = 0,
                ScoreboardPercent = 45.3
            });
            testPlayer.Scores.Add(new Scoreboards()
            {
                Frags = 15,
                Kills = 17,
                Death = 0,
                ScoreboardPercent = 93.8
            });
            testPlayer.Scores.Add(new Scoreboards()
            {
                Frags = 10,
                Kills = 10,
                Death = 0,
                ScoreboardPercent = 12.1
            });

            double kdr = 0;

            Assert.AreEqual(API.CalcKDRatio(testPlayer), kdr);
        }

        [TestMethod()]
        public void CalcAverageScoreboardPercent()
        {
            Players testPlayer = new Players()
            {
                Id = 0,
                Name = "TestPlayer1",
                Scores = new List<Scoreboards>(),
            };
            testPlayer.Scores.Add(new Scoreboards()
            {
                Frags = 30,
                Kills = 31,
                Death = 11,
                ScoreboardPercent = 100.0
            });
            testPlayer.Scores.Add(new Scoreboards()
            {
                Frags = 10,
                Kills = 10,
                Death = 25,
                ScoreboardPercent = 12.1
            });

            double asp = (100.0 + 12.1) / 2.0;

            Assert.AreEqual(API.CalcAverageScoreboardPercent(testPlayer), asp);
        }

        [TestMethod()]
        public void CalcAverageMatchesPerDay()
        {
            Servers server = new Servers()
            {
                Matches = new List<Matches>(),
            };
            server.Matches.Add(new Matches()
            {
                Timestamp = DateTime.Now,
            });
            server.Matches.Add(new Matches()
            {
                Timestamp = DateTime.Now.AddSeconds(10),
            });
            server.Matches.Add(new Matches()
            {
                Timestamp = DateTime.Now.AddDays(1),
            });

            double ampd = 1.5;

            Assert.AreEqual(API.CalcAverageMatchesPerDay(server), ampd);
        }

        [TestMethod()]
        public void FindLastMatchPlayed()
        {
            Players testPlayer = new Players()
            {
                Id = 0,
                Name = "TestPlayer1",
                Scores = new List<Scoreboards>(),
            };
            testPlayer.Scores.Add(new Scoreboards()
            {
                Match = new Matches()
                {
                    Timestamp = DateTime.Parse("2017-01-01T00:00:00Z"),
                }
            });
            testPlayer.Scores.Add(new Scoreboards()
            {
                Match = new Matches()
                {
                    Timestamp = DateTime.Parse("2018-01-01T00:00:00Z"),
                }
            });
            testPlayer.Scores.Add(new Scoreboards()
            {
                Match = new Matches()
                {
                    Timestamp = DateTime.Parse("2017-03-01T00:00:00Z"),
                }
            });

            DateTime dt = DateTime.Parse("2018-01-01T00:00:00Z");

            Assert.AreEqual(API.FindLastMatchPlayed(testPlayer), dt);
        }

        [TestMethod()]
        public void FindFavouriteMode()
        {
            Players testPlayer = new Players()
            {
                Id = 0,
                Name = "TestPlayer1",
                Scores = new List<Scoreboards>(),
            };
            testPlayer.Scores.Add(new Scoreboards()
            {
                Match = new Matches()
                {
                    GameMode = new GameModes() { Mode = "DM" },
                }
            });
            testPlayer.Scores.Add(new Scoreboards()
            {
                Match = new Matches()
                {
                    GameMode = new GameModes() { Mode = "TDM" },
                }
            });
            testPlayer.Scores.Add(new Scoreboards()
            {
                Match = new Matches()
                {
                    GameMode = new GameModes() { Mode = "CTF" },
                }
            });
            testPlayer.Scores.Add(new Scoreboards()
            {
                Match = new Matches()
                {
                    GameMode = new GameModes() { Mode = "TDM" },
                }
            });

            string mode = "TDM";

            Assert.AreEqual(API.FindFavouriteMode(testPlayer), mode);
        }

        [TestMethod()]
        public void FindFavouriteServer()
        {
            Players testPlayer = new Players()
            {
                Id = 0,
                Name = "TestPlayer1",
                Scores = new List<Scoreboards>(),
            };
            testPlayer.Scores.Add(new Scoreboards()
            {
                Match = new Matches()
                {
                    Server = new Servers() { Name = "Server1" },
                }
            });
            testPlayer.Scores.Add(new Scoreboards()
            {
                Match = new Matches()
                {
                    Server = new Servers() { Name = "Server3" },
                }
            });
            testPlayer.Scores.Add(new Scoreboards()
            {
                Match = new Matches()
                {
                    Server = new Servers() { Name = "Server3" },
                }
            });
            testPlayer.Scores.Add(new Scoreboards()
            {
                Match = new Matches()
                {
                    Server = new Servers() { Name = "Server2" },
                }
            });

            string server = "Server3";

            Assert.AreEqual(API.FindFavouriteServer(testPlayer), server);
        }

        [TestMethod()]
        public void GetTop5GameModes()
        {
            Servers server = new Servers()
            {
                Matches = new List<Matches>(),
            };
            for (int i = 0; i < 5; i++)
                server.Matches.Add(new Matches()
                {
                    GameMode = new GameModes() { Mode = "DM" }
                });
            for (int i = 0; i < 4; i++)
                server.Matches.Add(new Matches()
                {
                    GameMode = new GameModes() { Mode = "TDM" }
                });
            for (int i = 0; i < 3; i++)
                server.Matches.Add(new Matches()
                {
                    GameMode = new GameModes() { Mode = "CTF" }
                });
            for (int i = 0; i < 2; i++)
                server.Matches.Add(new Matches()
                {
                    GameMode = new GameModes() { Mode = "Rush" }
                });
            for (int i = 0; i < 1; i++)
                server.Matches.Add(new Matches()
                {
                    GameMode = new GameModes() { Mode = "Domination" }
                });

            List<string> expected = new List<string>();
            expected.Add("DM");
            expected.Add("TDM");
            expected.Add("CTF");
            expected.Add("Rush");
            expected.Add("Domination");

            for(int i = 0; i < 5; i++) 
                Assert.AreEqual(API.GetTop5GameModes(server)[i], expected[i]);
        }

        [TestMethod()]
        public void GetTop5GameMaps()
        {
            Servers server = new Servers()
            {
                Matches = new List<Matches>(),
            };
            for (int i = 0; i < 5; i++)
                server.Matches.Add(new Matches()
                {
                    Map = maps[0],
                });
            for (int i = 0; i < 4; i++)
                server.Matches.Add(new Matches()
                {
                    Map = maps[1],
                });
            for (int i = 0; i < 3; i++)
                server.Matches.Add(new Matches()
                {
                    Map = maps[2],
                });
            for (int i = 0; i < 2; i++)
                server.Matches.Add(new Matches()
                {
                    Map = maps[3],
                });
            for (int i = 0; i < 1; i++)
                server.Matches.Add(new Matches()
                {
                    Map = maps[4],
                });

            List<string> expected = new List<string>();
            expected.Add(maps[0]);
            expected.Add(maps[1]);
            expected.Add(maps[2]);
            expected.Add(maps[3]);
            expected.Add(maps[4]);

            for (int i = 0; i < 5; i++)
                Assert.AreEqual(API.GetTop5Maps(server)[i], expected[i]);
        }
    }
}