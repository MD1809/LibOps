using System;
using System.Windows.Input;
using LibOps.BusinessLogicLayer.BusinessServices;
using LibOps.DataModels.Entities;
using LibOps.PresentationLayer.MvvmCore;

namespace LibOps.PresentationLayer.ViewModels
{
    public enum QuickAddMetadataMode
    {
        Category,
        Author,
        Publisher
    }

    /// <summary>
    /// ViewModel điều khiển popup thêm nhanh Thể loại, Tác giả, Nhà xuất bản
    /// </summary>
    public class QuickAddMetadataViewModel : ViewModelBase
    {
        private readonly BookMetadataService _metadataService;
        private readonly QuickAddMetadataMode _mode;

        private string _dialogTitle;
        private string _fieldLabel;
        private string _primaryValue;
        private string _secondaryLabel;
        private string _secondaryValue;
        private bool _hasSecondaryField;
        private string _errorMessage;
        private bool _isSavedSuccessfully;

        public string DialogTitle
        {
            get => _dialogTitle;
            set => SetProperty(ref _dialogTitle, value);
        }

        public string FieldLabel
        {
            get => _fieldLabel;
            set => SetProperty(ref _fieldLabel, value);
        }

        public string PrimaryValue
        {
            get => _primaryValue;
            set => SetProperty(ref _primaryValue, value);
        }

        public string SecondaryLabel
        {
            get => _secondaryLabel;
            set => SetProperty(ref _secondaryLabel, value);
        }

        public string SecondaryValue
        {
            get => _secondaryValue;
            set => SetProperty(ref _secondaryValue, value);
        }

        public bool HasSecondaryField
        {
            get => _hasSecondaryField;
            set => SetProperty(ref _hasSecondaryField, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public bool IsSavedSuccessfully
        {
            get => _isSavedSuccessfully;
            set => SetProperty(ref _isSavedSuccessfully, value);
        }

        public int CreatedId { get; private set; }
        public string CreatedName { get; private set; }

        public ICommand SaveCommand { get; }
        public event Action RequestClose;

        public QuickAddMetadataViewModel(QuickAddMetadataMode mode)
        {
            _metadataService = new BookMetadataService();
            _mode = mode;

            SaveCommand = new RelayCommand(ExecuteSave);

            SetupFieldsByMode();
        }

        private void SetupFieldsByMode()
        {
            switch (_mode)
            {
                case QuickAddMetadataMode.Category:
                    DialogTitle = "THÊM NHANH THỂ LOẠI MỚI";
                    FieldLabel = "Tên Thể Loại (*)";
                    SecondaryLabel = "Mô tả thể loại";
                    HasSecondaryField = true;
                    break;

                case QuickAddMetadataMode.Author:
                    DialogTitle = "THÊM NHANH TÁC GIẢ MỚI";
                    FieldLabel = "Họ và Tên Tác Giả (*)";
                    SecondaryLabel = "Ghi chú / Tiểu sử ngắn";
                    HasSecondaryField = true;
                    break;

                case QuickAddMetadataMode.Publisher:
                    DialogTitle = "THÊM NHANH NHÀ XUẤT BẢN";
                    FieldLabel = "Tên Nhà Xuất Bản (*)";
                    SecondaryLabel = "Địa chỉ & Liên hệ";
                    HasSecondaryField = true;
                    break;
            }
        }

        private void ExecuteSave()
        {
            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(PrimaryValue))
            {
                ErrorMessage = $"Vui lòng nhập {FieldLabel.Replace("(*)", "").Trim().ToLower()}.";
                return;
            }

            string trimmedPrimary = PrimaryValue.Trim();
            string trimmedSecondary = SecondaryValue?.Trim();

            bool success = false;
            string error = string.Empty;

            switch (_mode)
            {
                case QuickAddMetadataMode.Category:
                    var category = new CategoryEntity
                    {
                        CategoryName = trimmedPrimary,
                        Description = trimmedSecondary
                    };
                    success = _metadataService.SaveCategory(category, out error);
                    if (success)
                    {
                        CreatedId = category.CategoryId;
                        CreatedName = category.CategoryName;
                    }
                    break;

                case QuickAddMetadataMode.Author:
                    var author = new AuthorEntity
                    {
                        AuthorName = trimmedPrimary,
                        Notes = trimmedSecondary
                    };
                    success = _metadataService.SaveAuthor(author, out error);
                    if (success)
                    {
                        CreatedId = author.AuthorId;
                        CreatedName = author.AuthorName;
                    }
                    break;

                case QuickAddMetadataMode.Publisher:
                    var publisher = new PublisherEntity
                    {
                        PublisherName = trimmedPrimary,
                        Address = trimmedSecondary,
                        PhoneNumber = string.Empty
                    };
                    success = _metadataService.SavePublisher(publisher, out error);
                    if (success)
                    {
                        CreatedId = publisher.PublisherId;
                        CreatedName = publisher.PublisherName;
                    }
                    break;
            }

            if (success)
            {
                IsSavedSuccessfully = true;
                RequestClose?.Invoke();
            }
            else
            {
                ErrorMessage = error;
            }
        }
    }
}
