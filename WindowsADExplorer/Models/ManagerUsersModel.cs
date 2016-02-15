using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ComparerExtensions;
using NDex;
using WindowsADExplorer.DataModeling;
using WindowsADExplorer.Entities;
using WindowsADExplorer.Mappers;

namespace WindowsADExplorer.Models
{
    public class ManagerUsersModel : ObservableModel<ManagerUsersModel>
    {
        private readonly IUserMapper userMapper;
        private IADRepository repository;
        private CancellationTokenSource tokenSource;

        public ManagerUsersModel(IUserMapper userMapper)
        {
            if (userMapper == null)
            {
                throw new ArgumentNullException("userMapper");
            }
            this.userMapper = userMapper;
            this.Members = new ThreadSafeObservableCollection<UserModel>();
            this.SearchResults = new ThreadSafeObservableCollection<UserModel>();
        }

        public GroupModel Group
        {
            get { return Get(x => x.Group); }
            private set { Set(x => x.Group, value); }
        }

        public ObservableCollection<UserModel> SearchResults
        {
            get { return Get(x => x.SearchResults); }
            private set { Set(x => x.SearchResults, value); }
        }

        public ObservableCollection<UserModel> Members
        {
            get { return Get(x => x.Members); }
            private set { Set(x => x.Members, value); }
        }

        public void SetRepository(IADRepository repository)
        {
            if (repository == null)
            {
                throw new ArgumentNullException("repository");
            }
            this.repository = repository;
        }

        public void SetGroup(GroupModel group)
        {
            if (group == null)
            {
                throw new ArgumentNullException("group");
            }
            if (repository == null)
            {
                throw new InvalidOperationException("You must share the connection with the model before setting the group.");
            }
            Group = group;

            if (tokenSource != null)
            {
                tokenSource.Cancel();
            }
            tokenSource = new CancellationTokenSource();

            var membersTask = Task.Factory.StartNew(() =>
            {
                var members = repository.GetGroupMembers(group.Name);
                return members;
            }, tokenSource.Token);

            var allUsersTask = Task.Factory.StartNew(() =>
            {
                var allUsers = repository.GetUsers(String.Empty);
                return allUsers;
            }, tokenSource.Token);

            membersTask.ContinueWith((t, o) => 
            {
                Members.Clear();

                var members = t.Result;
                var models = members.OrderBy(m => m.FullName).ThenBy(m => m.Name).Select(m => userMapper.GetModel(m, includeDummy: false));
                foreach (var model in models)
                {
                    tokenSource.Token.ThrowIfCancellationRequested();
                    Members.Add(model);
                }
            }, null, tokenSource.Token, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);

            Task.WhenAll(membersTask, allUsersTask).ContinueWith((t, o) => 
            {
                SearchResults.Clear();

                var members = t.Result[0];
                var allUsers = t.Result[1];

                var userComparer = KeyEqualityComparer<User>.Using(u => u.Name);
                var nonMembers = allUsers.Except(members, userComparer);
                var models = nonMembers.OrderBy(m => m.FullName).ThenBy(m => m.Name).Select(m => userMapper.GetModel(m, includeDummy: false));
                foreach (var model in models)
                {
                    tokenSource.Token.ThrowIfCancellationRequested();
                    SearchResults.Add(model);
                }
            }, null, tokenSource.Token, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
        }

        public void AddMember(UserModel user)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            Task.Factory.StartNew(() =>
            {
                repository.AddGroupMember(Group.Name, user.Name);
            }).ContinueWith(t =>
            {
                SearchResults.Remove(user);
                var modelComparer = KeyComparer<UserModel>.OrderBy(m => m.FullName);
                int index = Members.ToSublist().UpperBound(user, modelComparer);
                Members.Insert(index, user);
            }, TaskContinuationOptions.OnlyOnRanToCompletion);
        }

        public void RemoveMember(UserModel user)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            Task.Factory.StartNew(() =>
            {
                repository.RemoveGroupMember(Group.Name, user.Name);
            }).ContinueWith(t =>
            {
                Members.Remove(user);
                var modelComparer = KeyComparer<UserModel>.OrderBy(m => m.FullName);
                int index = SearchResults.ToSublist().UpperBound(user, modelComparer);
                SearchResults.Insert(index, user);
            }, TaskContinuationOptions.OnlyOnRanToCompletion);
        }

        internal void Cancel()
        {
            if (tokenSource != null)
            {
                tokenSource.Cancel();
            }
        }
    }
}
