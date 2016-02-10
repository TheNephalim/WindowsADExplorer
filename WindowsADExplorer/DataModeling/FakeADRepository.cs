using System;
using System.Collections.Generic;
using System.Linq;
using WindowsADExplorer.Entities;

namespace WindowsADExplorer.DataModeling
{
    public class FakeADRepository : IADRepository
    {
        public void TestPath()
        {
        }

        public string GetServerName()
        {
            return "<not connected>";
        }

        public IEnumerable<Group> GetGroups(string searchTerm)
        {
            var groups = Enumerable.Range(1, 100)
                .Select(i => "Group " + i)
                .Select(s => new Group() { Name = s });
            if (!String.IsNullOrWhiteSpace(searchTerm))
            {
                groups = groups.Where(g => g.Name.Contains(searchTerm));
            }
            return groups;
        }

        public IEnumerable<Group> GetUserGroups(string userName)
        {
            return new Group[]
            {
                new Group() { Name = "Group 1" },
                new Group() { Name = "Group 2" },
                new Group() { Name = "Group 3" }
            };
        }

        public IEnumerable<User> GetUsers(string searchTerm)
        {
            var users = Enumerable.Range(1, 10)
                .Select(i => "User " + i)
                .Select(s => new User() { Name = s, FullName = s });
            if (!String.IsNullOrWhiteSpace(searchTerm))
            {
                users = users.Where(g => g.Name.Contains(searchTerm));
            }
            return users;
        }

        public IEnumerable<User> GetGroupMembers(string userName)
        {
            return new User[]
            {
                new User() { Name = "User 1", FullName = "Ron Burgandy" },
                new User() { Name = "User 2", FullName = "Seth Johnson" }
            };
        }

        public IEnumerable<Property> GetGroupProperties(string groupName)
        {
            var properties = Enumerable.Range(1, 50)
                .Select(i => new Property() { Name = "Property " + i, Value = "Value " + i });
            return properties;
        }

        public IEnumerable<Property> GetUserProperties(string userName)
        {
            var properties = Enumerable.Range(1, 50)
                .Select(i => new Property() { Name = "Property " + i, Value = "Value " + i });
            return properties;
        }
    }
}
