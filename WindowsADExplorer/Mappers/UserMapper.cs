using WindowsADExplorer.Entities;
using WindowsADExplorer.Models;

namespace WindowsADExplorer.Mappers
{
    public interface IUserMapper
    {
        UserModel GetModel(User user, bool includeDummy);

        bool AreGroupsLoaded(UserModel user);
    }

    public class UserMapper : IUserMapper
    {
        private static readonly GroupModel dummyGroup = new GroupModel();

        public UserModel GetModel(User user, bool includeDummy)
        {
            UserModel model = new UserModel();
            model.Name = user.Name;
            model.FullName = user.FullName;
            model.Properties = new ThreadSafeObservableCollection<PropertyModel>();
            model.Groups = new ThreadSafeObservableCollection<GroupModel>();
            if (includeDummy)
            {
                model.Groups.Add(dummyGroup);
            }
            return model;
        }

        public bool AreGroupsLoaded(UserModel user)
        {
            if (user.Groups.Count == 1 && user.Groups[0] == dummyGroup)
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
