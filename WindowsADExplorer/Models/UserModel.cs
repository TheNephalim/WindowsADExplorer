using System.Collections.ObjectModel;

namespace WindowsADExplorer.Models
{
    public class UserModel : ObservableModel<UserModel>
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

        private string fullName;
        public string FullName 
        {
            get { return fullName; }
            set 
            {
                if (fullName == value)
                {
                    return;
                }
                OnPropertyChanging(x => x.FullName);
                fullName = value;
                OnPropertyChanged(x => x.FullName);
            }
        }

        public ThreadSafeObservableCollection<PropertyModel> Properties
        {
            get { return Get(x => x.Properties); }
            set { Set(x => x.Properties, value); }
        }

        public ThreadSafeObservableCollection<GroupModel> Groups
        {
            get { return Get(x => x.Groups); }
            set { Set(x => x.Groups, value); }
        }
    }
}
