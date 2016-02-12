using Ninject;
using WindowsADExplorer.Models;

namespace WindowsADExplorer.Injection
{
    public class ModelServiceLocator
    {
        private readonly IKernel kernel;

        public ModelServiceLocator()
        {
            this.kernel = new StandardKernel(new ExplorerNinjectModule());
        }

        public ExplorerModel ExplorerModel
        {
            get { return kernel.Get<ExplorerModel>(); }
        }

        public ErrorModel ErrorModel
        {
            get { return kernel.Get<ErrorModel>(); }
        }

        public ManagerUsersModel AddUserModel
        {
            get { return kernel.Get<ManagerUsersModel>(); }
        }

        public ConfirmationModel ConfirmationModel
        {
            get { return kernel.Get<ConfirmationModel>(); }
        }
    }
}
