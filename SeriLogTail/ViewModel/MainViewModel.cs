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

            var obsStream = new ObservableTable<SeriLogEntryModel>(connString);
            obsStream.NewValue += Strm_NewTransaction;
        }

        public ObservableCollection<SeriLogEntryModel> TheLog { get; private set; }

        private void Strm_NewTransaction(object sender, StreamEventArgs<SeriLogEntryModel> e)
        {
            App.Current.Dispatcher.Invoke(() => TheLog.Insert(0, e.Obj));
            Debug.WriteLine(@"logId: {0}", e.Obj.Id);
        }
    }
}