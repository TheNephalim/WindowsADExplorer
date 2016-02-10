using System;

namespace WindowsADExplorer.Models
{
    public class ConnectionModel : ObservableModel<ConnectionModel>
    {
        public string DomainName 
        {
            get { return Get(x => x.DomainName); }
            set { Set(x => x.DomainName, value); }
        }

        public string UserName 
        {
            get { return Get(x => x.UserName); }
            set { Set(x => x.UserName, value); }
        }

        public string Password 
        {
            get { return Get(x => x.Password); }
            set { Set(x => x.Password, value); }
        }
    }
}
