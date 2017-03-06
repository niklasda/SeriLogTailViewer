using System;
using System.Collections.Generic;
using System.Data.SqlClient;
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
        public ObservableTable(string connString)
        {
            var obs = GetObservable(connString);
            obs.Subscribe(x => OnChanged(new StreamEventArgs<T>(x)));
        }

        public event NewValueEventHandler<T> NewValue;

        private void OnChanged(StreamEventArgs<T> e)
        {
            NewValue?.Invoke(this, e);
        }

        private IObservable<T> GetObservable(string connString)
        {
            return ReadLines(connString).ToObservable(Scheduler.Default);
        }

        private IEnumerable<T> ReadLines(string connString)
        {
            using (var cn = new SqlConnection(connString))
            {
                cn.Open();

                int lastId = 0;

                do
                {
                    IEnumerable<T> logEntries = cn.Query<T>("SELECT TOP 50 * FROM Logs WHERE Id > @lastId ORDER BY Id ASC", new { lastId});

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