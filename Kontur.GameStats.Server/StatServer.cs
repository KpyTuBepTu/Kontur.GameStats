using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Data.Entity;
using SQLite.CodeFirst;
using System.Data.SQLite;
using System.Data.SQLite.Linq;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Collections.Generic;

namespace Kontur.GameStats.Server
{
    internal class StatServer : IDisposable
    {
        public StatServer()
        {
            listener = new HttpListener();
        }
        
        private void CreateTestDb()
        {
            Console.WriteLine("Creating Db");

            using (var db = new DbModel("stats.db"))
            {
                GameModes gm = new GameModes();
                gm.Mode = "DM";

                GameModes gm1 = new GameModes();
                gm1.Mode = "TDM";

                Servers server = new Servers();
                server.Endpoint = "localhost";
                server.Name = "ZigaZaga";
                server.Modes.Add(gm);
                server.Modes.Add(gm1);

                gm.ServersSet.Add(server);
                gm1.ServersSet.Add(server);

                db.GameModes.Add(gm);
                db.GameModes.Add(gm1);
                db.Servers.Add(server);
                db.SaveChanges();

                Matches match = new Matches();
                var serv = db.Servers.Where(s => s.Endpoint == "localhost").First();
                match.Server = serv;
                match.Map = "lalala";
                match.GameMode = db.GameModes.Where(gmd => gmd.Mode == "TDM").First();
                match.FragLimit = 20;
                match.TimeLimit = 30.5;
                match.TimeElapsed = 21.21;
                match.Timestamp = DateTime.Now;

                Scoreboards sb = new Scoreboards();
                sb.Match = match;
                sb.Frags = 10;
                sb.Kills = 10;
                sb.Death = 0;

                Scoreboards sb1 = new Scoreboards();
                sb1.Match = match;
                sb1.Frags = 0;
                sb1.Kills = 0;
                sb1.Death = 10;

                Players p1 = new Players();
                p1.Name = "P1";
                p1.Scores.Add(sb);

                Players p2 = new Players();
                p2.Name = "P2";
                p2.Scores.Add(sb1);

                sb.Player = p1;
                sb1.Player = p2;
                match.Scoreboard.Add(sb);
                match.Scoreboard.Add(sb1);
                serv.Matches.Add(match);

                db.Matches.Add(match);
                db.Scoreboards.Add(sb);
                db.Scoreboards.Add(sb1);
                db.SaveChanges();
            }

            Console.WriteLine("Test db complete!");
        }

        public void Start(string prefix)
        {
            lock (listener)
            {
                if (!isRunning)
                {
                    listener.Prefixes.Clear();
                    listener.Prefixes.Add(prefix);
                    listener.Start();

                    listenerThread = new Thread(Listen)
                    {
                        IsBackground = true,
                        Priority = ThreadPriority.Highest
                    };
                    listenerThread.Start();
                    
                    isRunning = true;
                }
            }
        }

        public void Stop()
        {
            lock (listener)
            {
                if (!isRunning)
                    return;

                listener.Stop();

                listenerThread.Abort();
                listenerThread.Join();
                
                isRunning = false;
            }
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;

            Stop();

            listener.Close();
        }
        
        private void Listen()
        {
            while (true)
            {
                try
                {
                    if (listener.IsListening)
                    {
                        var context = listener.GetContext();
                        Task.Run(() => HandleContextAsync(context));
                    }
                    else Thread.Sleep(0);
                }
                catch (ThreadAbortException)
                {
                    return;
                }
                catch (Exception error)
                {
                    // TODO: log errors
                    Console.WriteLine(error.Message);
                }
            }
        }

        private async Task HandleContextAsync(HttpListenerContext listenerContext)
        {
            // TODO: implement request handling
            switch (listenerContext.Request.HttpMethod)
            {
                case "GET":
                case "PUT":
                    {
                        try
                        {
                            var json = await API.UrlDefinition(listenerContext.Request.RawUrl, GetRequestData(listenerContext.Request), listenerContext.Request.HttpMethod);
                            listenerContext.Response.StatusCode = (int)HttpStatusCode.OK;
                            using (var writer = new StreamWriter(listenerContext.Response.OutputStream))
                                writer.WriteLine(json);
                            break;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                            listenerContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
                            using (var writer = new StreamWriter(listenerContext.Response.OutputStream))
                                writer.WriteLine();
                            break;
                        }
                    }
                default:
                    {
                        Console.WriteLine("Неподдерживаемый метод");
                        listenerContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
                        using (var writer = new StreamWriter(listenerContext.Response.OutputStream))
                            writer.WriteLine();
                        break;
                    }
            }
        }

        private string GetRequestData(HttpListenerRequest request)
        {
            if (!request.HasEntityBody)
                return string.Empty;

            using (Stream stream = request.InputStream)
            {
                using (StreamReader reader = new StreamReader(stream, request.ContentEncoding))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        private readonly HttpListener listener;

        private Thread listenerThread;
        private bool disposed;
        private volatile bool isRunning;
    }
}