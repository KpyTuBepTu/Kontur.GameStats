using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace Kontur.GameStats.Server
{
    internal class StatServer : IDisposable
    {
        public StatServer()
        {
            listener = new HttpListener();
        }

        public void Start(string prefix)
        {
            //Console.WriteLine("Fist slow connect to db");
            //using (var db = new DbModel("stats.db"))
            //{
            //    var initConnect = db.GameModes.Count();
            //}
            //Console.WriteLine("Ready");
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
                    API.CreateErrorLogRecord(error.Message, "external error", "ext");
                    Console.WriteLine("[" + DateTime.Now + "] " + error.Message);
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
                    // Key - true = success, false = error
                    // Value - if true = response, if false = error message
                    var json = await API.UrlDefinition(listenerContext.Request.RawUrl, GetRequestData(listenerContext.Request), listenerContext.Request.HttpMethod);
                    if (json.Key)
                    {
                        listenerContext.Response.StatusCode = (int)HttpStatusCode.OK;
                        using (var writer = new StreamWriter(listenerContext.Response.OutputStream))
                            writer.WriteLine(json.Value);
                    }
                    else
                        LoggingError(listenerContext, json.Value);
                    break;

                default:
                    LoggingError(listenerContext, "Unsupported method");
                    break;
            }
        }

        private static void LoggingError(HttpListenerContext listenerContext, string errorDescription)
        {
            API.CreateErrorLogRecord(errorDescription, listenerContext.Request.RawUrl, listenerContext.Request.HttpMethod);
            listenerContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
            using (var writer = new StreamWriter(listenerContext.Response.OutputStream))
                writer.WriteLine();
            Console.WriteLine("[" + DateTime.Now + "] " + errorDescription);
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