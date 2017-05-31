using Divisas2.ViewModels;

namespace Divisas2.Infrastructure
{
    public class InstanceLocator
    {
        public MainViewModel Main;
        public InstanceLocator()
        {
            Main = new MainViewModel();
        }
    }
}
