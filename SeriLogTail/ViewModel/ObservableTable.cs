using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Dapper;

namespace SeriLogTail.ViewModel
{
    public class StreamEventArgs<T> : EventArgs where T : SeriLogEntryModel
    {
        public StreamEventArgs(T obj)
        {
            Obj = obj;
        }

        public T Obj { get; private set; }
    }

    public delegate void NewValueEventHandler<T>(object sender, StreamEventArgs<T> e) where T : SeriLogEntryModel;

    public class ObservableTable<T> where T : SeriLogEntryModel
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
                cn.Open();

                int lastId = 0;

                do
                {
                    IEnumerable<T> logEntries;
                    if (lastId==0)
                    {
                        string q = string.Format("SELECT TOP 50 * FROM {0} ORDER BY Id DESC", tableName);
                        logEntries = cn.Query<T>(q, new { lastId }).OrderBy(x=>x.Id);
                    }
                    else
                    {
                        string q = string.Format("SELECT TOP 50 * FROM {0} WHERE Id > @lastId ORDER BY Id ASC", tableName);
                        logEntries = cn.Query<T>(q, new { lastId });
                    }

                    foreach (T l in logEntries)
                    {
                        lastId = Math.Max(lastId, l.Id);

                        yield return l;
                    }

                    Task.Delay(1000).Wait();

                } while (true);
            }
        }
    }
}