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

        IEnumerable<Group> GetGroups(string searchTerm);

        IEnumerable<Group> GetUserGroups(string userName);

        IEnumerable<User> GetUsers(string searchTerm);

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
            string userDN = getUserDN(userName);
            if (userDN == null)
            {
                return Enumerable.Empty<Group>();
            }
            string filter = "(&(objectCategory=group)(member=" + userDN + "))";
            string[] properties = getGroupProperties();
            DirectorySearcher searcher = new DirectorySearcher(rootEntry, filter, properties, SearchScope.Subtree);
            searcher.PageSize = 1000;
            SearchResultCollection results = searcher.FindAll();
            var groups = results.Cast<SearchResult>().Select(r => getGroup(r));
            return groups;
        }

        private string getUserDN(string sAMAccountName)
        {
            string filter = "(&(objectCategory=person)(sAMAccountName=" + sAMAccountName + "))";
            string[] properties = new string[] { "distinguishedname" };
            DirectorySearcher searcher = new DirectorySearcher(rootEntry, filter, properties, SearchScope.Subtree);
            SearchResult result = searcher.FindOne();
            if (result == null)
            {
                return null;
            }
            return result.Properties["distinguishedname"].Cast<string>().FirstOrDefault();
        }

        private static string[] getGroupProperties()
        {
            return new string[] { "sAMAccountName" };
        }

        private static Group getGroup(SearchResult result)
        {
            Group group = new Group();
            group.Name = result.Properties["sAMAccountName"].OfType<string>().FirstOrDefault();
            return group;
        }

        public IEnumerable<User> GetUsers(string searchTerm)
        {
            string filter = applySearchTerm("(objectCategory=person)", searchTerm);
            DirectorySearcher searcher = new DirectorySearcher(rootEntry, filter, null, SearchScope.Subtree);
            searcher.PageSize = 1000;
            SearchResultCollection results = searcher.FindAll();
            var users = results.Cast<SearchResult>().Select(r => getUser(r));
            return users;
        }

        public IEnumerable<User> GetGroupMembers(string groupName)
        {
            string groupDN = getGroupDN(groupName);
            if (groupDN == null)
            {
                return Enumerable.Empty<User>();
            }
            string filter = "(&(objectCategory=person)(memberOf=" + groupDN + "))";
            string[] properties = getUserProperties();
            DirectorySearcher searcher = new DirectorySearcher(rootEntry, filter, properties, SearchScope.Subtree);
            searcher.PageSize = 1000;
            SearchResultCollection results = searcher.FindAll();
            var users = results.Cast<SearchResult>().Select(r => getUser(r));
            return users;
        }

        private string getGroupDN(string sAMAccountName)
        {
            string filter = "(&(objectCategory=group)(sAMAccountName=" + sAMAccountName + "))";
            string[] properties = new string[] { "distinguishedname" };
            DirectorySearcher searcher = new DirectorySearcher(rootEntry, filter, properties, SearchScope.Subtree);
            SearchResult result = searcher.FindOne();
            if (result == null)
            {
                return null;
            }
            return result.Properties["distinguishedname"].Cast<string>().FirstOrDefault();
        }

        private User getUser(SearchResult result)
        {
            User user = new User();
            user.Name = result.Properties["sAMAccountName"].OfType<string>().FirstOrDefault();
            user.FullName = result.Properties["displayName"].OfType<string>().FirstOrDefault();
            return user;
        }

        private static string[] getUserProperties()
        {
            return new string[] { "sAMAccountName", "displayName" };
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
            string groupDN = getGroupDN(groupName);
            if (groupDN == null)
            {
                const string format = "Could not find a group with the given name ({0}).";
                string message = String.Format(format, groupName);
                throw new InvalidOperationException(message);
            }
            string userDN = getUserDN(userName);
            if (userDN == null)
            {
                const string format = "Could not find a group with the given name ({0}).";
                string message = String.Format(format, groupName);
                throw new InvalidOperationException(message);
            }
            using (DirectoryEntry groupEntry = new DirectoryEntry("LDAP://" + groupDN, this.userName, password)) // TODO - pass credentials
            {
                groupEntry.Properties["member"].Add(userDN);
                groupEntry.CommitChanges();
            }
        }

        public void RemoveGroupMember(string groupName, string userName)
        {
            string groupDN = getGroupDN(groupName);
            if (groupDN == null)
            {
                const string format = "Could not find a group with the given name ({0}).";
                string message = String.Format(format, groupName);
                throw new InvalidOperationException(message);
            }
            string userDN = getUserDN(userName);
            if (userDN == null)
            {
                const string format = "Could not find a group with the given name ({0}).";
                string message = String.Format(format, groupName);
                throw new InvalidOperationException(message);
            }
            using (DirectoryEntry groupEntry = new DirectoryEntry("LDAP://" + groupDN, this.userName, password)) // TODO - pass credentials
            {
                groupEntry.Properties["member"].Remove(userDN);
                groupEntry.CommitChanges();
            }
        }
    }
}
