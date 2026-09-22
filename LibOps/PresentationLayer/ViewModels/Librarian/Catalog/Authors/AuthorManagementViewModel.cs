using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using LibOps.BusinessLogicLayer.BusinessServices;
using LibOps.DataModels.Entities;
using LibOps.PresentationLayer.MvvmCore;

namespace LibOps.PresentationLayer.ViewModels
{
    public class AuthorManagementViewModel : ViewModelBase
    {
        private readonly BookMetadataService _metadataService;
        private AuthorEntity _selectedAuthor;
        private string _authorName;
        private string _authorNotes;
        private string _statusMessage;
        private bool _isStatusError;

        public AuthorEntity SelectedAuthor
        {
            get => _selectedAuthor;
            set
            {
                if (SetProperty(ref _selectedAuthor, value) && value != null)
                {
                    AuthorName = value.AuthorName;
                    AuthorNotes = value.Notes;
                }
            }
        }

        public string AuthorName { get => _authorName; set => SetProperty(ref _authorName, value); }
        public string AuthorNotes { get => _authorNotes; set => SetProperty(ref _authorNotes, value); }
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }
        public bool IsStatusError { get => _isStatusError; set => SetProperty(ref _isStatusError, value); }

        public PaginationController<AuthorEntity> Paging { get; } = new PaginationController<AuthorEntity>(10);
        public ObservableCollection<AuthorEntity> Authors => Paging.CurrentPageItems;

        public ICommand SaveCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand NewCommand { get; }
        public ICommand RefreshCommand { get; }

        public AuthorManagementViewModel()
        {
            _metadataService = new BookMetadataService();

            SaveCommand = new RelayCommand(SaveAuthor);
            DeleteCommand = new RelayCommand(DeleteAuthor);
            NewCommand = new RelayCommand(NewAuthor);
            RefreshCommand = new RelayCommand(LoadAuthors);

            LoadAuthors();
        }

        public void LoadAuthors()
        {
            try
            {
                var list = _metadataService.GetAllAuthors();
                Paging.SetSource(list);
                SetStatus($"Đã tải {Paging.TotalItems} tác giả.", false);
            }
            catch (Exception ex)
            {
                SetStatus("Lỗi tải danh mục tác giả: " + ex.Message, true);
            }
        }

        private void NewAuthor()
        {
            SelectedAuthor = null;
            AuthorName = string.Empty;
            AuthorNotes = string.Empty;
            SetStatus("Sẵn sàng thêm mới tác giả.", false);
        }

        private void SaveAuthor()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(AuthorName))
                {
                    SetStatus("Vui lòng nhập họ và tên tác giả!", true);
                    return;
                }

                if (AuthorName.Trim().Length > 100)
                {
                    SetStatus("Tên tác giả không được vượt quá 100 ký tự!", true);
                    return;
                }

                var aut = new AuthorEntity
                {
                    AuthorId = SelectedAuthor?.AuthorId ?? 0,
                    AuthorName = AuthorName.Trim(),
                    Notes = AuthorNotes?.Trim()
                };

                bool ok = _metadataService.SaveAuthor(aut, out string error);
                if (ok)
                {
                    SetStatus("Lưu thông tin tác giả thành công!", false);
                    LoadAuthors();
                    NewAuthor();
                }
                else
                {
                    SetStatus(error, true);
                }
            }
            catch (Exception ex)
            {
                SetStatus("Lỗi khi lưu tác giả: " + ex.Message, true);
            }
        }

        private void DeleteAuthor()
        {
            try
            {
                if (SelectedAuthor == null)
                {
                    SetStatus("Vui lòng chọn một tác giả cần xóa!", true);
                    return;
                }

                if (!DialogService.ShowConfirmation($"Xác nhận xóa tác giả '{SelectedAuthor.AuthorName}'?", "Xác Nhận Xóa"))
                {
                    return;
                }

                bool ok = _metadataService.DeleteAuthor(SelectedAuthor.AuthorId, out string error);
                if (ok)
                {
                    SetStatus("Đã xóa tác giả thành công!", false);
                    LoadAuthors();
                    NewAuthor();
                }
                else
                {
                    SetStatus(error, true);
                }
            }
            catch (Exception ex)
            {
                SetStatus("Lỗi khi xóa tác giả: " + ex.Message, true);
            }
        }

        private void SetStatus(string msg, bool isError)
        {
            StatusMessage = msg;
            IsStatusError = isError;
        }
    }
}
