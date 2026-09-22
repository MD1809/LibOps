using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using LibOps.BusinessLogicLayer.BusinessServices;
using LibOps.DataModels.Entities;
using LibOps.PresentationLayer.MvvmCore;

namespace LibOps.PresentationLayer.ViewModels
{
    public class PublisherManagementViewModel : ViewModelBase
    {
        private readonly BookMetadataService _metadataService;
        private PublisherEntity _selectedPublisher;
        private string _publisherName;
        private string _publisherAddress;
        private string _publisherPhone;
        private string _statusMessage;
        private bool _isStatusError;

        public PublisherEntity SelectedPublisher
        {
            get => _selectedPublisher;
            set
            {
                if (SetProperty(ref _selectedPublisher, value) && value != null)
                {
                    PublisherName = value.PublisherName;
                    PublisherAddress = value.Address;
                    PublisherPhone = value.PhoneNumber;
                }
            }
        }

        public string PublisherName { get => _publisherName; set => SetProperty(ref _publisherName, value); }
        public string PublisherAddress { get => _publisherAddress; set => SetProperty(ref _publisherAddress, value); }
        public string PublisherPhone { get => _publisherPhone; set => SetProperty(ref _publisherPhone, value); }
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }
        public bool IsStatusError { get => _isStatusError; set => SetProperty(ref _isStatusError, value); }

        public PaginationController<PublisherEntity> Paging { get; } = new PaginationController<PublisherEntity>(10);
        public ObservableCollection<PublisherEntity> Publishers => Paging.CurrentPageItems;

        public ICommand SaveCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand NewCommand { get; }
        public ICommand RefreshCommand { get; }

        public PublisherManagementViewModel()
        {
            _metadataService = new BookMetadataService();

            SaveCommand = new RelayCommand(SavePublisher);
            DeleteCommand = new RelayCommand(DeletePublisher);
            NewCommand = new RelayCommand(NewPublisher);
            RefreshCommand = new RelayCommand(LoadPublishers);

            LoadPublishers();
        }

        public void LoadPublishers()
        {
            try
            {
                var list = _metadataService.GetAllPublishers();
                Paging.SetSource(list);
                SetStatus($"Đã tải {Paging.TotalItems} nhà xuất bản.", false);
            }
            catch (Exception ex)
            {
                SetStatus("Lỗi tải danh mục nhà xuất bản: " + ex.Message, true);
            }
        }

        private void NewPublisher()
        {
            SelectedPublisher = null;
            PublisherName = string.Empty;
            PublisherAddress = string.Empty;
            PublisherPhone = string.Empty;
            SetStatus("Sẵn sàng thêm mới nhà xuất bản / nhà cung cấp.", false);
        }

        private void SavePublisher()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(PublisherName))
                {
                    SetStatus("Vui lòng nhập tên nhà xuất bản / nhà cung cấp!", true);
                    return;
                }

                if (PublisherName.Trim().Length > 150)
                {
                    SetStatus("Tên nhà xuất bản không được vượt quá 150 ký tự!", true);
                    return;
                }

                if (!string.IsNullOrWhiteSpace(PublisherPhone) && !LibOps.CommonUtilities.Validation.InputValidationUtility.IsValidPhoneNumber(PublisherPhone))
                {
                    SetStatus("Số điện thoại không đúng định dạng (10 chữ số)!", true);
                    return;
                }

                var pub = new PublisherEntity
                {
                    PublisherId = SelectedPublisher?.PublisherId ?? 0,
                    PublisherName = PublisherName.Trim(),
                    Address = PublisherAddress?.Trim(),
                    PhoneNumber = LibOps.CommonUtilities.Validation.InputValidationUtility.NormalizePhoneNumber(PublisherPhone)
                };

                bool ok = _metadataService.SavePublisher(pub, out string error);
                if (ok)
                {
                    SetStatus("Lưu thông tin nhà xuất bản thành công!", false);
                    LoadPublishers();
                    NewPublisher();
                }
                else
                {
                    SetStatus(error, true);
                }
            }
            catch (Exception ex)
            {
                SetStatus("Lỗi khi lưu nhà xuất bản: " + ex.Message, true);
            }
        }

        private void DeletePublisher()
        {
            if (SelectedPublisher == null)
            {
                SetStatus("Vui lòng chọn một nhà xuất bản cần xóa!", true);
                return;
            }

            if (!DialogService.ShowConfirmation($"Xác nhận xóa nhà xuất bản '{SelectedPublisher.PublisherName}'?", "Xác Nhận Xóa"))
            {
                return;
            }

            bool ok = _metadataService.DeletePublisher(SelectedPublisher.PublisherId, out string error);
            if (ok)
            {
                SetStatus("Đã xóa nhà xuất bản thành công!", false);
                LoadPublishers();
                NewPublisher();
            }
            else
            {
                SetStatus(error, true);
            }
        }

        private void SetStatus(string msg, bool isError)
        {
            StatusMessage = msg;
            IsStatusError = isError;
        }
    }
}
