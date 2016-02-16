using System;

namespace WindowsADExplorer.Models
{
    public class GroupMemberModel : ObservableModel<GroupMemberModel>
    {
        public string FullName
        {
            get { return Get(x => x.FullName); }
            set { Set(x => x.FullName, value); }
        }

        public string Name
        {
            get { return Get(x => x.Name); }
            set { Set(x => x.Name, value); }
        }

        public bool CanAdd
        {
            get { return Get(x => x.CanAdd); }
            set { Set(x => x.CanAdd, value); }
        }

        public bool CanRemove
        {
            get { return Get(x => x.CanRemove); }
            set { Set(x => x.CanRemove, value); }
        }
    }
}
