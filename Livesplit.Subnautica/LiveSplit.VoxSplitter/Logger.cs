using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace LiveSplit.VoxSplitter {
    public class Logger {
#if LOG
#if !DEBUG
        private readonly string logFile = "_" + Factory.ExAssembly.GetName().Name.Substring(10) + ".log";
        private const int LinesMax = 5000;
        private const int LinesErase = 500;

        private int lineNumber;
        private readonly Queue<string> linesQueue = new Queue<string>();
        private readonly object lockLines = new object();
        private readonly CancellationTokenSource tokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent manualEvent = new ManualResetEvent(false);
#endif
        private readonly Dictionary<string, Stopwatch> swList = new Dictionary<string, Stopwatch>();
        private readonly Dictionary<string, Tuple<int, double>> swAvg = new Dictionary<string, Tuple<int, double>>();
#endif

        public void StartLogger() {
#if LOG
#if DEBUG
            NativeMethods.AllocConsole();
            Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
#else
            new Thread(() => {
                lineNumber = 0;
                if(!File.Exists(logFile)) {
                    File.Create(logFile);
                } else {
                    using(FileStream stream = File.Open(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                        using(StreamReader reader = new StreamReader(stream)) {
                            while(!tokenSource.IsCancellationRequested && reader.ReadLine() != null) {
                                lineNumber++;
                            }
                        }
                    }
                }

                if(tokenSource.IsCancellationRequested) {
                    return;
                }

                string line = null;
                while(true) {
                    manualEvent.WaitOne();
                    if(tokenSource.IsCancellationRequested) {
                        return;
                    }
                    lock(lockLines) {
                        if(linesQueue.Count != 0) {
                            line = linesQueue.Dequeue();
                        } else {
                            manualEvent.Reset();
                            continue;
                        }
                    }
                    WriteLine(line);
                    line = null;
                }
            }).Start();
#endif
#endif
        }

        public void StopLogger() {
#if LOG
#if DEBUG
            NativeMethods.FreeConsole();
#else
            tokenSource.Cancel();
            manualEvent.Set();
#endif
#endif
        }

        public void Log(string msg) {
#if LOG
            msg = DateTime.Now.ToString("HH:mm:ss.fff") + " " + msg;
#if DEBUG
            Console.WriteLine(msg);
#else
            lock(lockLines) {
                linesQueue.Enqueue(msg);
                manualEvent.Set();
            }
#endif
#endif
        }

        public void WriteLine(string msg) {
#if LOG
#if !DEBUG
            if(lineNumber >= LinesMax) {
                string tempLog = logFile + "-temp";
                int linesSkipped = this.lineNumber - LinesMax + LinesErase;
                int lineNumber = 1;
                using(StreamReader reader = File.OpenText(logFile)) {
                    using(StreamWriter writer = File.CreateText(tempLog)) {
                        string line;
                        while((line = reader.ReadLine()) != null) {
                            if(lineNumber > linesSkipped) {
                                writer.WriteLine(line);
                            } else {
                                lineNumber++;
                            }
                        }
                    }
                }
                try {
                    File.Copy(tempLog, logFile, true);
                    this.lineNumber = LinesMax - LinesErase;
                } catch {
                    Options.Log.Error("Failed replacing log file");
                } finally {
                    try {
                        File.Delete(tempLog);
                    } catch {
                        Options.Log.Error("Failed deleting temp log file");
                    }
                }
            }

            try {
                using(StreamWriter writer = new StreamWriter(logFile, true)) {
                    writer.WriteLine(msg);
                    ++lineNumber;
                }
            } catch(Exception e) {
                Options.Log.Error(e.ToString());
            }
#endif
#endif
        }

        public void StartBenchmark(string key) {
#if LOG
            swList.Add(key, Stopwatch.StartNew());
#endif
        }

        public void StopBenchmark(string key, string prefix = "") {
#if LOG
            swList[key].Stop();
            Log(prefix + swList[key].Elapsed.ToString("mm:ss.fffffff"));
            swList.Remove(key);
#endif
        }

        public void StartAverageBenchmark(string key) {
#if LOG
            swList.Add(key, Stopwatch.StartNew());
            if(!swAvg.ContainsKey(key)) {
                swAvg.Add(key, new Tuple<int, double>(0, 0));
            }
#endif
        }

        public void StopAverageBenchmark(string key, string prefix = "") {
#if LOG
            swList[key].Stop();
            Tuple<int, double> tuple = swAvg[key];
            swAvg[key] = new Tuple<int, double>(tuple.Item1 + 1, tuple.Item2 + swList[key].Elapsed.TotalMilliseconds);
            Log(prefix + swList[key].Elapsed.ToString("mm:ss.fffffff") + " Average " + (tuple.Item2 / tuple.Item1));
            swList.Remove(key);
#endif
        }
    }
}