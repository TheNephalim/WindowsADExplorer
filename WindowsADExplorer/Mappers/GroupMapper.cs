using System.Collections.ObjectModel;
using WindowsADExplorer.Entities;
using WindowsADExplorer.Models;

namespace WindowsADExplorer.Mappers
{
    public interface IGroupMapper
    {
        GroupModel GetModel(Group group, bool includeDummy);

        bool AreUsersLoaded(GroupModel group);
    }

    public class GroupMapper : IGroupMapper
    {
        private static readonly UserModel dummyUser = new UserModel();

        public GroupModel GetModel(Group group, bool includeDummy)
        {
            GroupModel model = new GroupModel();
            model.Name = group.Name;
            model.Properties = new ObservableCollection<PropertyModel>();
            model.Users = new ObservableCollection<UserModel>();
            if (includeDummy)
            {
                model.Users.Add(dummyUser);
            }
            return model;
        }

        public bool AreUsersLoaded(GroupModel group)
        {
            if (group.Users.Count == 1 && group.Users[0] == dummyUser)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
