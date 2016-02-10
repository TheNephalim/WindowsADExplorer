using Ninject;
using Ninject.Activation;
using Ninject.Modules;
using WindowsADExplorer.DataModeling;
using WindowsADExplorer.Mappers;
using WindowsADExplorer.Models;
using WindowsADExplorer.Settings;

namespace WindowsADExplorer.Injection
{
    public class ExplorerNinjectModule : NinjectModule
    {
        public override void Load()
        {
            Bind<IADSettings>().To<ADSettings>();
            Bind<IADRepositoryFactory>().ToMethod(c => getADRepositoryFactory(c));
            Bind<IGroupMapper>().To<GroupMapper>();
            Bind<IUserMapper>().To<UserMapper>();
            Bind<IPropertyMapper>().To<PropertyMapper>();

            Bind<ExplorerModel>().ToSelf().InSingletonScope();
            Bind<ErrorModel>().ToSelf().InSingletonScope();
        }

        private IADRepositoryFactory getADRepositoryFactory(IContext context)
        {
            IADSettings settings = context.Kernel.Get<IADSettings>();
            if (settings.IsWindowsADAvailable)
            {
                return context.Kernel.Get<ADRepositoryFactory>();
            }
            else
            {
                return context.Kernel.Get<FakeADRepositoryFactory>();
            }
        }
    }
}
