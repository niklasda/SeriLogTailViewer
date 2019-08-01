using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Dapper;

namespace SeriLogTail.ViewModel
{
    public class StreamEventArgs<T> : EventArgs where T : SeriLogEntryModel, new()
    {
        public StreamEventArgs(T obj)
        {
            Obj = obj;
        }

        public T Obj { get; }
    }

    public delegate void NewValueEventHandler<T>(object sender, StreamEventArgs<T> e) where T : SeriLogEntryModel, new();

    public class ObservableTable<T> where T : SeriLogEntryModel, new()
    {
        public ObservableTable(string connString, string tableName)
        {
            var obs = GetObservable(connString, tableName);
            obs.Subscribe(x => OnChanged(new StreamEventArgs<T>(x)));
        }

        public event NewValueEventHandler<T> NewValue;

        private void OnChanged(StreamEventArgs<T> e)
        {
            NewValue?.Invoke(this, e);
        }

        private IObservable<T> GetObservable(string connString, string tableName)
        {
            return ReadLines(connString, tableName).ToObservable(Scheduler.Default);
        }

        private IEnumerable<T> ReadLines(string connString, string tableName)
        {
            using (var cn = new SqlConnection(connString))
            {
                bool keepGoing = true;
                int lastId = 0;

                do
                {
                    IEnumerable<T> logEntries;

                    try
                    {
                        if (cn.State != ConnectionState.Open)
                        {
                            cn.Open();
                        }

                        if (lastId == 0)
                        {
                            string q = $"SELECT TOP 50 * FROM {tableName} ORDER BY Id DESC";
                            logEntries = cn.Query<T>(q, new { lastId }, commandTimeout:60).OrderBy(x => x.Id);
                        }
                        else
                        {
                            string q = $"SELECT TOP 50 * FROM {tableName} WHERE Id > @lastId ORDER BY Id ASC";
                            logEntries = cn.Query<T>(q, new { lastId }, commandTimeout:60);
                        }
                    }
                    catch (Exception ex)
                    {
                        logEntries = new List<T> {new T() {Message = ex.Message} };
                        keepGoing = false;
                    }

                    foreach (T l in logEntries)
                    {
                        lastId = Math.Max(lastId, l.Id);

                        yield return l;
                    }

                    Task.Delay(1000).Wait();

                } while (keepGoing);
            }
        }
    }
}