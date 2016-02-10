using System;
using System.DirectoryServices;
using System.DirectoryServices.ActiveDirectory;

namespace WindowsADExplorer.DataModeling
{
    public interface IADRepositoryFactory
    {
        IADRepository GetRepository(string domainName, string userName, string password);
    }

    public class ADRepositoryFactory : IADRepositoryFactory
    {
        public IADRepository GetRepository(string domainName, string userName, string password)
        {
            Domain domain = getDomain(domainName, userName, password);
            DirectoryEntry rootEntry = domain.GetDirectoryEntry();
            return new ADRepository(domain.Name, rootEntry);
        }

        private static Domain getDomain(string domainName, string userName, string password)
        {
            if (domainName == null)
            {
                return Domain.GetCurrentDomain();
            }
            DirectoryContext context = new DirectoryContext(
                DirectoryContextType.Domain,
                domainName,
                userName,
                password);
            Domain domain = Domain.GetDomain(context);
            return domain;
        }
    }
}
