using System;

namespace WindowsADExplorer.DataModeling
{
    public class FakeADRepositoryFactory : IADRepositoryFactory
    {
        public IADRepository GetRepository(string domainName, string userName, string password)
        {
            return new FakeADRepository();
        }
    }
}
