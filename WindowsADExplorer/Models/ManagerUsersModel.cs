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
        private Task searchTask;
        private CancellationTokenSource memberTokenSource;
        private CancellationTokenSource searchTokenSource;

        public ManagerUsersModel(IUserMapper userMapper)
        {
            if (userMapper == null)
            {
                throw new ArgumentNullException("userMapper");
            }
            this.userMapper = userMapper;

            this.Members = new ThreadSafeObservableCollection<UserModel>();
            this.Members.EnableSync();

            this.searchTask = new Task(() => { });
            this.SearchResults = new ThreadSafeObservableCollection<UserModel>();
            this.SearchResults.EnableSync();
        }

        public bool IsSearching
        {
            get { return Get(x => x.IsSearching); }
            set { Set(x => x.IsSearching, value); }
        }

        public GroupModel Group
        {
            get { return Get(x => x.Group); }
            private set { Set(x => x.Group, value); }
        }

        public ThreadSafeObservableCollection<UserModel> SearchResults
        {
            get { return Get(x => x.SearchResults); }
            private set { Set(x => x.SearchResults, value); }
        }

        public ThreadSafeObservableCollection<UserModel> Members
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

            if (memberTokenSource != null)
            {
                memberTokenSource.Cancel();
            }
            memberTokenSource = new CancellationTokenSource();

            var membersTask = Task.Factory.StartNew(() =>
            {
                var members = repository.GetGroupMembers(group.Name);
                return members;
            }, memberTokenSource.Token).ContinueWith((t, o) => 
            {
                CancellationToken token = memberTokenSource.Token;
                Members.Clear();

                var members = t.Result;
                var models = members.Select(m => userMapper.GetModel(m, includeDummy: false));
                var modelComparer = KeyComparer<UserModel>.OrderBy(m => m.FullName).ThenBy(m => m.Name);
                foreach (var model in models)
                {
                    token.ThrowIfCancellationRequested();
                    int index = Members.ToSublist().UpperBound(model, modelComparer);
                    Members.Insert(index, model);
                }
            }, null, memberTokenSource.Token, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
        }

        public void Search(string searchTerm)
        {
            if (String.IsNullOrWhiteSpace(searchTerm))
            {
                return;
            }
            if (repository == null)
            {
                throw new InvalidOperationException("You must share the connection with the model before searching.");
            }

            if (searchTokenSource != null)
            {
                searchTokenSource.Cancel();
            }
            searchTokenSource = new CancellationTokenSource();

            searchTask = searchTask.ContinueWith(
                t => IsSearching = true,
                searchTokenSource.Token,
                TaskContinuationOptions.None,
                TaskScheduler.FromCurrentSynchronizationContext()
            ).ContinueWith(t =>
            {
                CancellationToken token = searchTokenSource.Token;
                SearchResults.Clear();

                var searchResults = repository.GetUsers(searchTerm);
                var models = searchResults.Select(m => userMapper.GetModel(m, includeDummy: false));
                var modelComparer = KeyComparer<UserModel>.OrderBy(m => m.FullName).ThenBy(m => m.Name);
                foreach (var model in models)
                {
                    token.ThrowIfCancellationRequested();
                    int index = SearchResults.ToSublist().UpperBound(model, modelComparer);
                    SearchResults.Insert(index, model);
                }
            }, searchTokenSource.Token).ContinueWith(
                t => IsSearching = false,
                searchTokenSource.Token, 
                TaskContinuationOptions.None, 
                TaskScheduler.FromCurrentSynchronizationContext()
            );
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

        public void Cancel()
        {
            if (memberTokenSource != null)
            {
                memberTokenSource.Cancel();
            }
            if (searchTokenSource != null)
            {
                searchTokenSource.Cancel();
            }
        }
    }
}
