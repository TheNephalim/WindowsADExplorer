using System.Collections.ObjectModel;

namespace WindowsADExplorer.Models
{
    public class UserModel : ObservableModel<UserModel>
    {
        public string Name 
        {
            get { return Get(x => x.Name); }
            set { Set(x => x.Name, value); }
        }

        public string FullName 
        {
            get { return Get(x => x.FullName); }
            set { Set(x => x.FullName, value); }
        }

        public ObservableCollection<PropertyModel> Properties
        {
            get { return Get(x => x.Properties); }
            set { Set(x => x.Properties, value); }
        }

        public ObservableCollection<GroupModel> Groups
        {
            get { return Get(x => x.Groups); }
            set { Set(x => x.Groups, value); }
        }
    }
}
