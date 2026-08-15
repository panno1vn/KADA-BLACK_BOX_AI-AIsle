using CommunityToolkit.Mvvm.ComponentModel;

namespace AIsle.DesktopApp.ViewModels
{
    public abstract class ViewModelBase : ObservableObject
    {
        public virtual string Title => GetType().Name.Replace("ViewModel", "");
    }
}
