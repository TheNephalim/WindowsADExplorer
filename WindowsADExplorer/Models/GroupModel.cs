using System;
using System.Collections.ObjectModel;

namespace WindowsADExplorer.Models
{
    public class GroupModel : ObservableModel<GroupModel>
    {
        private string name;

        public string Name 
        {
            get { return name; }
            set 
            {
                if (name == value)
                {
                    return;
                }
                OnPropertyChanging(x => x.Name);
                name = value;
                OnPropertyChanged(x => x.Name);
            }
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
