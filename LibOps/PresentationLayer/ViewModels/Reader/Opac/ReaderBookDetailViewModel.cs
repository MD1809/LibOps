using System;
using System.Windows.Input;
using LibOps.DataModels.DataTransferObjects;
using LibOps.PresentationLayer.MvvmCore;

namespace LibOps.PresentationLayer.ViewModels
{
    /// <summary>
    /// ViewModel hiển thị thông tin chi tiết của một đầu sách dành cho độc giả
    /// </summary>
    public class ReaderBookDetailViewModel : ViewModelBase
    {
        private readonly Action _onBackAction;
        private ReaderBookSearchDto _book;

        public ReaderBookSearchDto Book
        {
            get => _book;
            set => SetProperty(ref _book, value);
        }

        public ICommand BackCommand { get; }

        public ReaderBookDetailViewModel(ReaderBookSearchDto book, Action onBackAction = null)
        {
            _book = book;
            _onBackAction = onBackAction;

            BackCommand = new RelayCommand(() => _onBackAction?.Invoke());
        }
    }
}
