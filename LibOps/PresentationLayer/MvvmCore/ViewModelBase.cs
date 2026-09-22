using System.ComponentModel;
using System.Runtime.CompilerServices;
using LibOps.PresentationLayer.Services;

namespace LibOps.PresentationLayer.MvvmCore
{
    /// <summary>
    /// Lớp cơ sở cho tất cả ViewModel, triển khai INotifyPropertyChanged để tự động cập nhật dữ liệu lên giao diện XAML
    /// </summary>
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected IDialogService DialogService { get; }

        protected ViewModelBase(IDialogService dialogService = null)
        {
            DialogService = dialogService ?? Services.DialogService.Current;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value))
            {
                return false;
            }

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
