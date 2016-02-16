using System;
using System.Collections.Generic;
using System.Linq;
using System.DirectoryServices;
using WindowsADExplorer.Entities;
using System.Security.Principal;
using System.Text;

namespace WindowsADExplorer.DataModeling
{
    public interface IADRepository
    {
        void TestPath();

        string GetServerName();

        User GetUser(string userName);

        Group GetGroup(string groupName);

        IEnumerable<Group> GetGroups(string searchTerm);

        IEnumerable<Group> GetUserGroups(string userName);

        IEnumerable<User> GetUsers(string searchTerm, bool includeGroups = false);

        IEnumerable<User> GetGroupMembers(string groupName);

        IEnumerable<Property> GetGroupProperties(string groupName);

        IEnumerable<Property> GetUserProperties(string userName);

        void AddGroupMember(string groupName, string userName);

        void RemoveGroupMember(string groupName, string userName);
    }

    public class ADRepository : IADRepository
    {
        private readonly string serverName;
        private readonly DirectoryEntry rootEntry;
        private readonly string userName;
        private readonly string password;

        public ADRepository(
            string serverName, 
            DirectoryEntry rootEntry,
            string userName,
            string password)
        {
            if (rootEntry == null)
            {
                throw new ArgumentNullException("rootEntry");
            }
            this.serverName = serverName;
            this.rootEntry = rootEntry;
            this.userName = userName;
            this.password = password;
        }

        public void TestPath()
        {
            if (!DirectoryEntry.Exists(rootEntry.Path))
            {
                throw new Exception("Could not find the given location in AD.");
            }
        }

        public string GetServerName()
        {
            return serverName;
        }

        public User GetUser(string userName)
        {
            return getUser(userName);
        }

        private User getUser(string userName)
        {
            string filter = "(&(objectCategory=person)(sAMAccountName=" + userName + "))";
            string[] properties = getUserProperties();
            DirectorySearcher searcher = new DirectorySearcher(rootEntry, filter, properties, SearchScope.Subtree);
            searcher.PageSize = 1000;
            SearchResult result = searcher.FindOne();
            if (result == null)
            {
                return null;
            }
            return getUser(result, false);
        }

        public Group GetGroup(string groupName)
        {
            return getGroup(groupName);
        }

        private Group getGroup(string groupName)
        {
            string filter = "(&(objectCategory=group)(sAMAccountName=" + groupName + "))";
            string[] properties = getGroupProperties();
            DirectorySearcher searcher = new DirectorySearcher(rootEntry, filter, properties, SearchScope.Subtree);
            searcher.PageSize = 1000;
            SearchResult result = searcher.FindOne();
            if (result == null)
            {
                return null;
            }
            return getGroup(result);
        }

        public IEnumerable<Group> GetGroups(string searchTerm)
        {
            string filter = applySearchTerm("(objectCategory=group)", searchTerm);
            string[] properties = getGroupProperties();
            DirectorySearcher searcher = new DirectorySearcher(rootEntry, filter, properties, SearchScope.Subtree);
            searcher.PageSize = 1000;
            SearchResultCollection results = searcher.FindAll();
            var groups = results.Cast<SearchResult>().Select(r => getGroup(r));
            return groups;
        }

        public IEnumerable<Group> GetUserGroups(string userName)
        {
            User user = getUser(userName);
            if (user == null)
            {
                return Enumerable.Empty<Group>();
            }
            string filter = "(&(objectCategory=group)(member=" + user.DistinguishedName + "))";
            string[] properties = getGroupProperties();
            DirectorySearcher searcher = new DirectorySearcher(rootEntry, filter, properties, SearchScope.Subtree);
            searcher.PageSize = 1000;
            SearchResultCollection results = searcher.FindAll();
            var groups = results.Cast<SearchResult>().Select(r => getGroup(r));
            return groups;
        }

        private static string[] getGroupProperties()
        {
            return new string[] { "sAMAccountName", "distinguishedname" };
        }

        private static Group getGroup(SearchResult result)
        {
            Group group = new Group();
            group.DistinguishedName = result.Properties["distinguishedname"].OfType<string>().FirstOrDefault();
            group.Name = result.Properties["sAMAccountName"].OfType<string>().FirstOrDefault();
            return group;
        }

        public IEnumerable<User> GetUsers(string searchTerm, bool includeGroups = false)
        {
            string filter = applySearchTerm("(objectCategory=person)", searchTerm);
            List<string> properties = new List<string>(getUserProperties());
            if (includeGroups)
            {
                properties.Add("memberOf");
            }
            DirectorySearcher searcher = new DirectorySearcher(rootEntry, filter, properties.ToArray(), SearchScope.Subtree);
            searcher.PageSize = 1000;
            SearchResultCollection results = searcher.FindAll();
            var users = results.Cast<SearchResult>().Select(r => getUser(r, includeGroups));
            return users;
        }

        public IEnumerable<User> GetGroupMembers(string groupName)
        {
            Group group = getGroup(groupName);
            if (group == null)
            {
                return Enumerable.Empty<User>();
            }
            string filter = "(&(objectCategory=person)(memberOf=" + group.DistinguishedName + "))";
            string[] properties = getUserProperties();
            DirectorySearcher searcher = new DirectorySearcher(rootEntry, filter, properties, SearchScope.Subtree);
            searcher.PageSize = 1000;
            SearchResultCollection results = searcher.FindAll();
            var users = results.Cast<SearchResult>().Select(r => getUser(r, includeGroups: false));
            return users;
        }

        private User getUser(SearchResult result, bool includeGroups)
        {
            User user = new User();
            user.DistinguishedName = result.Properties["distinguishedname"].OfType<string>().FirstOrDefault();
            user.Name = result.Properties["sAMAccountName"].OfType<string>().FirstOrDefault();
            user.FullName = result.Properties["displayName"].OfType<string>().FirstOrDefault();
            if (includeGroups)
            {
                user.Groups = result.Properties["memberOf"].OfType<string>().ToArray();
            }
            return user;
        }

        private static string[] getUserProperties()
        {
            return new string[] { "sAMAccountName", "displayName", "distinguishedname" };
        }

        public IEnumerable<Property> GetGroupProperties(string groupName)
        {
            return getProperties("group", groupName);
        }

        public IEnumerable<Property> GetUserProperties(string userName)
        {
            return getProperties("person", userName);
        }

        private IEnumerable<Property> getProperties(string itemCategory, string itemName)
        {
            string filter = "(&(objectCategory=" + itemCategory + ")(sAMAccountName=" + itemName + "))";
            string[] properties = null;  // Get all properties
            DirectorySearcher searcher = new DirectorySearcher(rootEntry, filter, properties, SearchScope.Subtree);
            SearchResult result = searcher.FindOne();
            if (result == null)
            {
                return Enumerable.Empty<Property>();
            }
            var results = result.Properties.PropertyNames
                .Cast<string>()
                .Select(n => new Property()
                {
                    Name = n,
                    Value = String.Join("; ", result.Properties[n].OfType<string>())
                });
            return results;
        }

        private static string applySearchTerm(string baseFilter, string searchTerm)
        {
            if (String.IsNullOrWhiteSpace(searchTerm))
            {
                return baseFilter;
            }
            searchTerm = escape(searchTerm);
            string filter = String.Format("(&{0}(sAMAccountName={1}*))", baseFilter, searchTerm);
            return filter;
        }

        private static string escape(string value)
        {
            if (value == null)
            {
                return null;
            }
            value = value.Trim();
            StringBuilder builder = new StringBuilder();
            foreach (char character in value)
            {
                switch (character)
                {
                    case ',':
                    case '\\':
                    case '#':
                    case '+':
                    case '<':
                    case '>':
                    case ';':
                    case '"':
                    case '=':
                        builder.Append('\\');
                        builder.Append(character);
                        break;
                    default: 
                        builder.Append(character);
                        break;
                }
            }
            return builder.ToString();
        }

        public void AddGroupMember(string groupName, string userName)
        {
            Group group = getGroup(groupName);
            if (group == null)
            {
                const string format = "Could not find a group with the given name ({0}).";
                string message = String.Format(format, groupName);
                throw new InvalidOperationException(message);
            }
            User user = getUser(userName);
            if (user == null)
            {
                const string format = "Could not find a group with the given name ({0}).";
                string message = String.Format(format, groupName);
                throw new InvalidOperationException(message);
            }
            using (DirectoryEntry groupEntry = new DirectoryEntry("LDAP://" + group.DistinguishedName, this.userName, password))
            {
                groupEntry.Properties["member"].Add(user.DistinguishedName);
                groupEntry.CommitChanges();
            }
        }

        public void RemoveGroupMember(string groupName, string userName)
        {
            Group group = getGroup(groupName);
            if (group == null)
            {
                const string format = "Could not find a group with the given name ({0}).";
                string message = String.Format(format, groupName);
                throw new InvalidOperationException(message);
            }
            User user = getUser(userName);
            if (user == null)
            {
                const string format = "Could not find a group with the given name ({0}).";
                string message = String.Format(format, groupName);
                throw new InvalidOperationException(message);
            }
            using (DirectoryEntry groupEntry = new DirectoryEntry("LDAP://" + group.DistinguishedName, this.userName, password)) // TODO - pass credentials
            {
                groupEntry.Properties["member"].Remove(user.DistinguishedName);
                groupEntry.CommitChanges();
            }
        }
    }
}
