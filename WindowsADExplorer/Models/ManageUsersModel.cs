using System;
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
    public class ManageUsersModel : ObservableModel<ManageUsersModel>
    {
        private readonly IUserMapper userMapper;
        private IADRepository repository;
        private Task searchTask;
        private CancellationTokenSource searchTokenSource;

        public ManageUsersModel(IUserMapper userMapper)
        {
            if (userMapper == null)
            {
                throw new ArgumentNullException("userMapper");
            }
            this.userMapper = userMapper;

            this.searchTask = Task.Factory.StartNew(() => { });
            this.SearchResults = new ThreadSafeObservableCollection<GroupMemberModel>();
            this.SearchResults.EnableSync();
        }

        public event EventHandler<Exception> ErrorOccurred;

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

        public ThreadSafeObservableCollection<GroupMemberModel> SearchResults
        {
            get { return Get(x => x.SearchResults); }
            private set { Set(x => x.SearchResults, value); }
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
        }

        public void Search(string searchTerm)
        {
            if (repository == null)
            {
                throw new InvalidOperationException("You must share the connection with the model before searching.");
            }
            if (Group == null)
            {
                throw new InvalidOperationException("You must set the group before searching.");
            }
            if (String.IsNullOrWhiteSpace(searchTerm))
            {
                return;
            }

            if (searchTokenSource != null)
            {
                searchTokenSource.Cancel();
            }
            searchTokenSource = new CancellationTokenSource();

            searchTask = searchTask.ContinueWith(
                t => { IsSearching = true; },
                searchTokenSource.Token,
                TaskContinuationOptions.None,
                TaskScheduler.FromCurrentSynchronizationContext()
            ).ContinueWith(t =>
            {
                var searchResults = repository.GetUsers(searchTerm, includeGroups: true);
                return searchResults;
            }, searchTokenSource.Token).ContinueWith(t =>
            {
                CancellationToken token = searchTokenSource.Token;
                SearchResults.Clear();

                Group group = repository.GetGroup(Group.Name);
                var searchResults = t.Result;
                var models = searchResults.Select(m => userMapper.GetMemberModel(group.DistinguishedName, m));
                var modelComparer = KeyComparer<GroupMemberModel>.OrderBy(m => m.FullName).ThenBy(m => m.Name);
                foreach (var model in models)
                {
                    token.ThrowIfCancellationRequested();
                    int index = SearchResults.ToSublist().UpperBound(model, modelComparer);
                    SearchResults.Insert(index, model);
                }
            }, searchTokenSource.Token, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
            searchTask.ContinueWith(
                t => onErrorOccurred(t.Exception), 
                CancellationToken.None, 
                TaskContinuationOptions.OnlyOnFaulted, 
                TaskScheduler.FromCurrentSynchronizationContext()
            );
            searchTask.ContinueWith(
                 t => { IsSearching = false; },
                 searchTokenSource.Token,
                 TaskContinuationOptions.None,
                 TaskScheduler.FromCurrentSynchronizationContext()
             );
        }

        public void AddMember(GroupMemberModel user)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            var addTask = Task.Factory.StartNew(() =>
            {
                repository.AddGroupMember(Group.Name, user.Name);
            });
            addTask.ContinueWith(
                t => onErrorOccurred(t.Exception), 
                CancellationToken.None, 
                TaskContinuationOptions.OnlyOnFaulted, 
                TaskScheduler.FromCurrentSynchronizationContext()
            );
            addTask.ContinueWith(t =>
            {
                user.CanAdd = false;
                user.CanRemove = true;
            }, CancellationToken.None, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.FromCurrentSynchronizationContext());
        }

        public void RemoveMember(GroupMemberModel user)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            var removeTask = Task.Factory.StartNew(() =>
            {
                repository.RemoveGroupMember(Group.Name, user.Name);
            });
            removeTask.ContinueWith(
                t => onErrorOccurred(t.Exception),
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted,
                TaskScheduler.FromCurrentSynchronizationContext()
            );
            removeTask.ContinueWith(t =>
            {
                user.CanAdd = true;
                user.CanRemove = false;
            }, CancellationToken.None, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void onErrorOccurred(AggregateException exception)
        {
            if (exception != null && ErrorOccurred != null)
            {
                ErrorOccurred(this, exception.InnerExceptions.First());
            }
        }

        public void Cancel()
        {
            if (searchTokenSource != null)
            {
                searchTokenSource.Cancel();
            }
        }
    }
}
