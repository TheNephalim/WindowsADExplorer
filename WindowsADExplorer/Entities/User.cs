using System;

namespace WindowsADExplorer.Entities
{
    public class User
    {
        public string DistinguishedName { get; set; }

        public string Name { get; set; }

        public string FullName { get; set; }

        public string[] Groups { get; set; }
    }
}
