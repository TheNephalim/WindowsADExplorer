using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using ComparerExtensions;
using NDex;
using WindowsADExplorer.DataModeling;
using WindowsADExplorer.Mappers;

namespace WindowsADExplorer.Models
{
    public class ExplorerModel : ObservableModel<ExplorerModel>
    {
        private readonly IADRepositoryFactory factory;
        private readonly IGroupMapper groupMapper;
        private readonly IUserMapper userMapper;
        private readonly IPropertyMapper propertyMapper;
        private IADRepository repository;
        private ICollection currentCollection;
        private CancellationTokenSource tokenSource;

        public ExplorerModel(
            IADRepositoryFactory factory,
            IGroupMapper groupMapper,
            IUserMapper userMapper,
            IPropertyMapper propertyMapper)
        {
            if (factory == null)
            {
                throw new ArgumentNullException("factory");
            }
            if (groupMapper == null)
            {
                throw new ArgumentNullException("groupMapper");
            }
            if (userMapper == null)
            {
                throw new ArgumentNullException("userMapper");
            }
            if (propertyMapper == null)
            {
                throw new ArgumentNullException("propertyMapper");
            }
            this.factory = factory;
            this.groupMapper = groupMapper;
            this.userMapper = userMapper;
            this.propertyMapper = propertyMapper;

            Groups = new ObservableCollection<GroupModel>();
            BindingOperations.EnableCollectionSynchronization(Groups, new object());
            Groups.CollectionChanged += (o, e) => { OnPropertyChanged(x => x.RecordCount); };
            currentCollection = Groups;

            Users = new ObservableCollection<UserModel>();
            BindingOperations.EnableCollectionSynchronization(Users, new object());
            Users.CollectionChanged += (o, e) => { OnPropertyChanged(x => x.RecordCount); };
        }

        public int RecordCount 
        {
            get { return currentCollection.Count; }
        }

        public string ServerName
        {
            get { return Get(x => x.ServerName); }
            set { Set(x => x.ServerName, value); }
        }

        public bool IsSearching
        {
            get { return Get(x => x.IsSearching); }
            private set { Set(x => x.IsSearching, value); }
        }

        public ObservableCollection<GroupModel> Groups 
        { 
            get { return Get(x => x.Groups); }
            private set { Set(x => x.Groups, value); }
        }

        public ObservableCollection<UserModel> Users 
        {
            get { return Get(x => x.Users); }
            private set { Set(x => x.Users, value); }
        }

        public bool IsConnected
        {
            get { return repository != null; }
        }

        public void OpenConnection(ConnectionModel connectionModel)
        {
            IADRepository repository = factory.GetRepository(
                connectionModel.DomainName, 
                connectionModel.UserName, 
                connectionModel.Password);
            repository.TestPath();
            this.repository = repository;
            ServerName = this.repository.GetServerName();
        }

        public void RetrieveGroups(string searchTerm)
        {
            if (repository == null)
            {
                throw new InvalidOperationException("You must connect to AD before querying the groups.");
            }
            currentCollection = Groups;
            OnPropertyChanged(x => x.RecordCount);

            if (tokenSource != null)
            {
                tokenSource.Cancel();
            }
            tokenSource = new CancellationTokenSource();
            var token = tokenSource.Token;

            Task.Factory.StartNew(() =>
            {
                IsSearching = true;
                Groups.Clear();
                var groups = repository.GetGroups(searchTerm);
                var models = groups.Select(g => groupMapper.GetModel(g, includeDummy: true));
                var modelComparer = KeyComparer<GroupModel>.OrderBy(m => m.Name);
                foreach (var model in models)
                {
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }
                    int index = Groups.ToSublist().UpperBound(model, modelComparer);
                    Groups.Insert(index, model);
                }
            }, token).ContinueWith(t => 
            { 
                IsSearching = false; 
            }, token, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.FromCurrentSynchronizationContext());
        }

        public void RetrieveGroupProperties(GroupModel group)
        {
            if (group == null)
            {
                throw new ArgumentNullException("group");
            }
            var propertyCollection = group.Properties;
            BindingOperations.EnableCollectionSynchronization(propertyCollection, propertyCollection);

            Task.Factory.StartNew(() =>
            {
                propertyCollection.Clear();
                var properties = repository.GetGroupProperties(group.Name);
                var models = properties.Select(p => propertyMapper.GetModel(p));
                var modelComparer = KeyComparer<PropertyModel>.OrderBy(p => p.Name);
                foreach (PropertyModel model in models)
                {
                    int index = propertyCollection.ToSublist().UpperBound(model, modelComparer);
                    propertyCollection.Insert(index, model);
                }
            });
        }

        public void RetrieveGroupMembers(GroupModel group)
        {
            if (group == null)
            {
                throw new ArgumentNullException("group");
            }
            if (groupMapper.AreUsersLoaded(group))
            {
                return;
            }
            var userCollection = group.Users;
            BindingOperations.EnableCollectionSynchronization(userCollection, userCollection);

            Task.Factory.StartNew(() =>
            {
                userCollection.Clear();
                var users = repository.GetGroupMembers(group.Name);
                var userModels = users.Select(u => userMapper.GetModel(u, includeDummy: false));
                var modelComparer = KeyComparer<UserModel>.OrderBy(m => m.FullName);
                foreach (UserModel model in userModels)
                {
                    int index = userCollection.ToSublist().UpperBound(model, modelComparer);
                    userCollection.Insert(index, model);
                }
            });
        }

        public void RetrieveUsers(string searchTerm)
        {
            if (repository == null)
            {
                throw new InvalidOperationException("You must connect to AD before querying the users.");
            }
            currentCollection = Users;
            OnPropertyChanged(x => x.RecordCount);

            if (tokenSource != null)
            {
                tokenSource.Cancel();
            }
            tokenSource = new CancellationTokenSource();
            var token = tokenSource.Token;

            Task.Factory.StartNew(() =>
            {
                IsSearching = true;
                Users.Clear();
                var users = repository.GetUsers(searchTerm);
                var models = users.Select(u => userMapper.GetModel(u, includeDummy: true));
                var modelComparer = KeyComparer<UserModel>.OrderBy(u => u.FullName);
                foreach (var model in models)
                {
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }
                    int index = Users.ToSublist().UpperBound(model, modelComparer);
                    Users.Insert(index, model);
                }
            }, token).ContinueWith(t => 
            { 
                IsSearching = false; 
            }, token, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.FromCurrentSynchronizationContext());
        }

        public void RetrieveUserProperties(UserModel user)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            var propertyCollection = user.Properties;
            BindingOperations.EnableCollectionSynchronization(propertyCollection, propertyCollection);

            Task.Factory.StartNew(() =>
            {
                propertyCollection.Clear();
                var properties = repository.GetUserProperties(user.Name);
                var models = properties.Select(p => propertyMapper.GetModel(p));
                var modelComparer = KeyComparer<PropertyModel>.OrderBy(p => p.Name);
                foreach (PropertyModel model in models)
                {
                    int index = propertyCollection.ToSublist().UpperBound(model, modelComparer);
                    propertyCollection.Insert(index, model);
                }
            });
        }

        public void RetrieveUserGroups(UserModel user)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            if (userMapper.AreGroupsLoaded(user))
            {
                return;
            }
            var groupCollection = user.Groups;
            BindingOperations.EnableCollectionSynchronization(groupCollection, groupCollection);

            Task.Factory.StartNew(() =>
            {
                groupCollection.Clear();
                var groups = repository.GetUserGroups(user.Name);
                var groupModels = groups.Select(g => groupMapper.GetModel(g, includeDummy: false));
                var modelComparer = KeyComparer<GroupModel>.OrderBy(m => m.Name);
                foreach (GroupModel model in groupModels)
                {
                    int index = groupCollection.ToSublist().UpperBound(model, modelComparer);
                    groupCollection.Insert(index, model);
                }
            });
        }

        public void Cancel()
        {
            if (tokenSource != null)
            {
                tokenSource.Cancel();
            }
        }

        public void ApplyFilter(string filterText)
        {
            if (currentCollection == Groups)
            {
                RetrieveGroups(filterText);
            }
            else if (currentCollection == Users)
            {
                RetrieveUsers(filterText);
            }
        }
    }
}
