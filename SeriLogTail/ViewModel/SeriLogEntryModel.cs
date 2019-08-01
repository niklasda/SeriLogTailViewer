using System;

namespace SeriLogTail.ViewModel
{
    public class SeriLogEntryModel
    {
        public int Id { get; set; }
        public string Message { get; set; }
        public string MessageTemplate { get; set; }
        public string Level { get; set; }
        public DateTimeOffset TimeStamp { get; set; }
        public string TimeStampString
        {
            get { return TimeStamp.LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss.fffff zzz"); }
        }
        public string Exception { get; set; }
        public string Properties { get; set; }
        public string LogEvent { get; set; }
    }
}