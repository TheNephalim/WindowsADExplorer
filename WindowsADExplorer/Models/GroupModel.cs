using System;
using System.Collections.ObjectModel;

namespace WindowsADExplorer.Models
{
    public class GroupModel : ObservableModel<GroupModel>
    {
        public string Name 
        {
            get { return Get(x => x.Name); }
            set { Set(x => x.Name, value); }
        }

        public ObservableCollection<PropertyModel> Properties
        {
            get { return Get(x => x.Properties); }
            set { Set(x => x.Properties, value); }
        }

        public ObservableCollection<UserModel> Users
        {
            get { return Get(x => x.Users); }
            set { Set(x => x.Users, value); }
        }
    }
}
