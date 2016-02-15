using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        private CancellationTokenSource tokenSource;
        private Task groupTask;
        private Task userTask;

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
            this.groupTask = Task.Factory.StartNew(() => { });
            this.userTask = Task.Factory.StartNew(() => { });

            Groups = new ThreadSafeObservableCollection<GroupModel>();
            Groups.EnableSync();

            Users = new ThreadSafeObservableCollection<UserModel>();
            Users.EnableSync();
        }

        public event EventHandler<Exception> ErrorOccurred;

        public string ServerName
        {
            get { return Get(x => x.ServerName); }
            private set { Set(x => x.ServerName, value); }
        }

        public bool IsSearching
        {
            get { return Get(x => x.IsSearching); }
            private set { Set(x => x.IsSearching, value); }
        }

        public ICollection ActiveCollection
        {
            get { return Get(x => x.ActiveCollection); }
            private set { Set(x => x.ActiveCollection, value); }
        }

        public ThreadSafeObservableCollection<GroupModel> Groups 
        { 
            get { return Get(x => x.Groups); }
            private set { Set(x => x.Groups, value); }
        }

        public ThreadSafeObservableCollection<UserModel> Users 
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

        public void ShareConnection(ManagerUsersModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException("model");
            }
            if (repository == null)
            {
                throw new InvalidOperationException("You must connect to AD before sharing the connection.");
            }
            model.SetRepository(repository);
        }

        public void RetrieveGroups(string searchTerm)
        {
            if (repository == null)
            {
                throw new InvalidOperationException("You must connect to AD before querying the groups.");
            }
            ActiveCollection = Groups;

            if (tokenSource != null)
            {
                tokenSource.Cancel();
            }
            tokenSource = new CancellationTokenSource();

            groupTask = groupTask.ContinueWith(
                t => IsSearching = true, 
                tokenSource.Token, 
                TaskContinuationOptions.None, 
                TaskScheduler.FromCurrentSynchronizationContext()
            ).ContinueWith(t =>
            {
                CancellationToken token = tokenSource.Token;
                Groups.Clear();
                var groups = repository.GetGroups(searchTerm);
                var models = groups.Select(g => groupMapper.GetModel(g, includeDummy: true));
                var modelComparer = KeyComparer<GroupModel>.OrderBy(m => m.Name);
                foreach (var model in models)
                {
                    token.ThrowIfCancellationRequested();
                    int index = Groups.ToSublist().UpperBound(model, modelComparer);
                    Groups.Insert(index, model);
                }
            }, tokenSource.Token).ContinueWith(
                t => IsSearching = false, 
                tokenSource.Token, 
                TaskContinuationOptions.None, 
                TaskScheduler.FromCurrentSynchronizationContext()
            );
        }

        public void RetrieveGroupProperties(GroupModel group)
        {
            if (group == null)
            {
                throw new ArgumentNullException("group");
            }
            group.Properties.EnableSync();

            Task.Factory.StartNew(() =>
            {
                group.Properties.Clear();
                var properties = repository.GetGroupProperties(group.Name);
                var models = properties.Select(p => propertyMapper.GetModel(p));
                var modelComparer = KeyComparer<PropertyModel>.OrderBy(p => p.Name);
                foreach (PropertyModel model in models)
                {
                    int index = group.Properties.ToSublist().UpperBound(model, modelComparer);
                    group.Properties.Insert(index, model);
                }
            }).ContinueWith(
                t => onErrorOccurred(t.Exception), 
                CancellationToken.None, 
                TaskContinuationOptions.OnlyOnFaulted, 
                TaskScheduler.FromCurrentSynchronizationContext()
            );
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
            group.Users.EnableSync();

            Task.Factory.StartNew(() =>
            {
                group.Users.Clear();
                var users = repository.GetGroupMembers(group.Name);
                var userModels = users.Select(u => userMapper.GetModel(u, includeDummy: false));
                var modelComparer = KeyComparer<UserModel>.OrderBy(m => m.FullName);
                foreach (UserModel model in userModels)
                {
                    int index = group.Users.ToSublist().UpperBound(model, modelComparer);
                    group.Users.Insert(index, model);
                }
            }).ContinueWith(
                t => onErrorOccurred(t.Exception), 
                CancellationToken.None, 
                TaskContinuationOptions.OnlyOnFaulted, 
                TaskScheduler.FromCurrentSynchronizationContext()
            );
        }

        public void RetrieveUsers(string searchTerm)
        {
            if (repository == null)
            {
                throw new InvalidOperationException("You must connect to AD before querying the users.");
            }
            ActiveCollection = Users;

            if (tokenSource != null)
            {
                tokenSource.Cancel();
            }
            tokenSource = new CancellationTokenSource();

            userTask = userTask.ContinueWith(
                t => IsSearching = true, 
                tokenSource.Token, 
                TaskContinuationOptions.None, 
                TaskScheduler.FromCurrentSynchronizationContext()
            ).ContinueWith(t =>
            {
                CancellationToken token = tokenSource.Token;
                Users.Clear();
                var users = repository.GetUsers(searchTerm);
                var models = users.Select(u => userMapper.GetModel(u, includeDummy: true));
                var modelComparer = KeyComparer<UserModel>.OrderBy(u => u.FullName);
                foreach (var model in models)
                {
                    token.ThrowIfCancellationRequested();
                    int index = Users.ToSublist().UpperBound(model, modelComparer);
                    Users.Insert(index, model);
                }
            }, tokenSource.Token).ContinueWith(
                t => onErrorOccurred(t.Exception),
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted,
                TaskScheduler.FromCurrentSynchronizationContext()
            ).ContinueWith(
                t => IsSearching = false,
                tokenSource.Token, 
                TaskContinuationOptions.None, 
                TaskScheduler.FromCurrentSynchronizationContext()
            );
        }

        public void RetrieveUserProperties(UserModel user)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            user.Properties.EnableSync();

            Task.Factory.StartNew(() =>
            {
                user.Properties.Clear();
                var properties = repository.GetUserProperties(user.Name);
                var models = properties.Select(p => propertyMapper.GetModel(p));
                var modelComparer = KeyComparer<PropertyModel>.OrderBy(p => p.Name);
                foreach (PropertyModel model in models)
                {
                    int index = user.Properties.ToSublist().UpperBound(model, modelComparer);
                    user.Properties.Insert(index, model);
                }
            }).ContinueWith(
                t => onErrorOccurred(t.Exception), 
                CancellationToken.None, 
                TaskContinuationOptions.OnlyOnFaulted, 
                TaskScheduler.FromCurrentSynchronizationContext()
            );
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
            user.Groups.EnableSync();

            Task.Factory.StartNew(() =>
            {
                user.Groups.Clear();
                var groups = repository.GetUserGroups(user.Name);
                var groupModels = groups.Select(g => groupMapper.GetModel(g, includeDummy: false));
                var modelComparer = KeyComparer<GroupModel>.OrderBy(m => m.Name);
                foreach (GroupModel model in groupModels)
                {
                    int index = user.Groups.ToSublist().UpperBound(model, modelComparer);
                    user.Groups.Insert(index, model);
                }
            }).ContinueWith(
                t => onErrorOccurred(t.Exception), 
                CancellationToken.None, 
                TaskContinuationOptions.OnlyOnFaulted, 
                TaskScheduler.FromCurrentSynchronizationContext()
            );
        }

        private void onErrorOccurred(Exception exception)
        {
            if (exception != null && ErrorOccurred != null)
            {
                ErrorOccurred(this, exception);
            }
        }

        public void Cancel()
        {
            if (tokenSource != null)
            {
                tokenSource.Cancel();
            }
        }
    }
}
