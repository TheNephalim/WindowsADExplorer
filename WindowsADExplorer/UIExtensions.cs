using System.Windows;

namespace WindowsADExplorer
{
    public static class UIExtensions
    {
        public static T FindResource<T>(this FrameworkElement element, object resourceKey)
        {
            return (T)element.FindResource(resourceKey);
        }

        public static T TryFindResource<T>(this FrameworkElement element, object resourceKey)
            where T : class
        {
            return element.TryFindResource(resourceKey) as T;
        }
    }
}
