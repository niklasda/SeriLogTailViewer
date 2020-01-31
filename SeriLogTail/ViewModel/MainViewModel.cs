using System;
using System.Collections.ObjectModel;
using GalaSoft.MvvmLight;
using System.Configuration;
using System.Diagnostics;

namespace SeriLogTail.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        public MainViewModel()
        {
            TheLog = new ObservableCollection<SeriLogEntryModel>();

            string connString = ConfigurationManager.ConnectionStrings["TheLogDatabase"].ConnectionString;
            string table = ConfigurationManager.AppSettings["TheLogTable"];
            string defaultTableName = "Logs";
            string tableName = table ?? defaultTableName;

            var len = connString.IndexOf(";P", StringComparison.CurrentCultureIgnoreCase);
            len = len < 0 ? connString.Length : len;
            string safePart = connString.Substring(0, len);

            WindowTitle = $"SeriLogTableTailViewer - {tableName} - {safePart}";

            var obsStream = new ObservableTable<SeriLogEntryModel>(connString, tableName);
            obsStream.NewValue += Stream_NewTransaction;
        }


        private string _windowTitle;
        public string WindowTitle
        {
            get => _windowTitle;
            set
            {
                if (_windowTitle != value)
                {
                    _windowTitle = value;
                    RaisePropertyChanged();
                }
            }
        }

        public ObservableCollection<SeriLogEntryModel> TheLog { get; private set; }

        private void Stream_NewTransaction(object sender, StreamEventArgs<SeriLogEntryModel> e)
        {
            App.Current.Dispatcher.Invoke(() => TheLog.Insert(0, e.Obj));
            Debug.WriteLine(@"logId: {0}", e.Obj.Id);
        }
    }
}