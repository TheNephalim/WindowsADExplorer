using System;

namespace WindowsADExplorer.Models
{
    public class ConfirmationModel : ObservableModel<ConfirmationModel>
    {
        public string Message
        {
            get { return Get(x => x.Message); }
            set { Set(x => x.Message, value); }
        }
    }
}
