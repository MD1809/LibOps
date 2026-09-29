using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using LibOps.BusinessLogicLayer.BusinessServices;
using LibOps.DataModels.DataTransferObjects;
using LibOps.DataModels.Entities;
using LibOps.PresentationLayer.MvvmCore;

namespace LibOps.PresentationLayer.ViewModels
{
    public class BookCopyBarcodeViewModel : ViewModelBase
    {
        private readonly BookService _bookService;

        private int _bookId;
        private BookEntity _bookInfo;
        private int _addCopyCount = 1;
        private string _initialCondition = "Mới 100%, nguyên vẹn";
        private BookCopyGridDisplayDto _selectedCopy;
        private string _selectedCopyStatus = "AVAILABLE";
        private string _selectedCopyConditionNote = string.Empty;
        private string _statusMessage;
        private bool _isStatusError;

        private int _totalCopiesCount;
        private int _availableCopiesCount;
        private int _borrowedCopiesCount;
        private int _damagedOrLostCount;

        public int BookId
        {
            get => _bookId;
            set
            {
                if (SetProperty(ref _bookId, value))
                {
                    LoadData();
                }
            }
        }

        public BookEntity BookInfo
        {
            get => _bookInfo;
            set => SetProperty(ref _bookInfo, value);
        }

        public int AddCopyCount
        {
            get => _addCopyCount;
            set => SetProperty(ref _addCopyCount, value);
        }

        public string InitialCondition
        {
            get => _initialCondition;
            set => SetProperty(ref _initialCondition, value);
        }

        public BookCopyGridDisplayDto SelectedCopy
        {
            get => _selectedCopy;
            set
            {
                if (SetProperty(ref _selectedCopy, value))
                {
                    if (value != null)
                    {
                        SelectedCopyStatus = value.Status;
                        SelectedCopyConditionNote = value.ConditionNote;
                    }
                    else
                    {
                        SelectedCopyStatus = "AVAILABLE";
                        SelectedCopyConditionNote = string.Empty;
                    }
                    OnPropertyChanged(nameof(HasSelectedCopy));
                    OnPropertyChanged(nameof(SelectedCopyBarcode));
                    ((RelayCommand)UpdateCopyCommand)?.RaiseCanExecuteChanged();
                    ((RelayCommand)DeleteCopyCommand)?.RaiseCanExecuteChanged();
                    ((RelayCommand)PrintBarcodeCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public bool HasSelectedCopy => SelectedCopy != null;
        public string SelectedCopyBarcode => SelectedCopy?.Barcode ?? "(Chưa chọn bản sao)";

        public string SelectedCopyStatus
        {
            get => _selectedCopyStatus;
            set => SetProperty(ref _selectedCopyStatus, value);
        }

        public string SelectedCopyConditionNote
        {
            get => _selectedCopyConditionNote;
            set => SetProperty(ref _selectedCopyConditionNote, value);
        }

        public int TotalCopiesCount { get => _totalCopiesCount; set => SetProperty(ref _totalCopiesCount, value); }
        public int AvailableCopiesCount { get => _availableCopiesCount; set => SetProperty(ref _availableCopiesCount, value); }
        public int BorrowedCopiesCount { get => _borrowedCopiesCount; set => SetProperty(ref _borrowedCopiesCount, value); }
        public int DamagedOrLostCount { get => _damagedOrLostCount; set => SetProperty(ref _damagedOrLostCount, value); }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public bool IsStatusError
        {
            get => _isStatusError;
            set
            {
                if (SetProperty(ref _isStatusError, value))
                {
                    OnPropertyChanged(nameof(StatusMessageBrush));
                }
            }
        }

        public System.Windows.Media.Brush StatusMessageBrush => _isStatusError 
            ? new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#EF4444"))
            : new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#10B981"));

        public PaginationController<BookCopyGridDisplayDto> Paging { get; } = new PaginationController<BookCopyGridDisplayDto>(10);
        public ObservableCollection<BookCopyGridDisplayDto> Copies => Paging.CurrentPageItems;
        public ObservableCollection<BookGridDisplayDto> AllBooks { get; }

        public ICommand GenerateCopiesCommand { get; }
        public ICommand UpdateCopyCommand { get; }
        public ICommand DeleteCopyCommand { get; }
        public ICommand PrintBarcodeCommand { get; }
        public ICommand RefreshCommand { get; }

        public BookCopyBarcodeViewModel(int bookId = 0)
        {
            _bookService = new BookService();

            AllBooks = new ObservableCollection<BookGridDisplayDto>();

            GenerateCopiesCommand = new RelayCommand(GenerateCopies);
            UpdateCopyCommand = new RelayCommand(UpdateCopy, () => HasSelectedCopy);
            DeleteCopyCommand = new RelayCommand(DeleteCopy, () => HasSelectedCopy);
            PrintBarcodeCommand = new RelayCommand(PrintBarcode, () => HasSelectedCopy);
            RefreshCommand = new RelayCommand(LoadData);

            LoadAllBooksList();

            if (bookId > 0)
            {
                BookId = bookId;
            }
            else if (AllBooks.Count > 0)
            {
                BookId = AllBooks[0].BookId;
            }
        }

        private void LoadAllBooksList()
        {
            try
            {
                AllBooks.Clear();
                var list = _bookService.GetAllBooks();
                if (list != null)
                {
                    foreach (var b in list)
                    {
                        AllBooks.Add(b);
                    }
                }
            }
            catch (Exception ex)
            {
                SetStatus("Lỗi tải danh sách đầu sách: " + ex.Message, true);
            }
        }

        public void LoadData()
        {
            try
            {
                if (BookId <= 0) return;

                BookInfo = _bookService.GetBookById(BookId);

                var list = _bookService.GetCopiesByBookId(BookId) ?? new System.Collections.Generic.List<BookCopyGridDisplayDto>();
                Paging.SetSource(list);

                TotalCopiesCount = list.Count;
                AvailableCopiesCount = list.Count(x => x.Status == "AVAILABLE");
                BorrowedCopiesCount = list.Count(x => x.Status == "BORROWED");
                DamagedOrLostCount = list.Count(x => x.Status == "DAMAGED" || x.Status == "LOST");

                SelectedCopy = Copies.FirstOrDefault();

                SetStatus($"Đã tải {list.Count} bản sao của đầu sách '{BookInfo?.Title}'.", false);
            }
            catch (Exception ex)
            {
                SetStatus("Lỗi tải danh sách bản sao: " + ex.Message, true);
            }
        }

        private void GenerateCopies()
        {
            if (BookId <= 0)
            {
                SetStatus("Vui lòng chọn một đầu sách để tạo bản sao.", true);
                return;
            }

            if (AddCopyCount <= 0 || AddCopyCount > 100)
            {
                SetStatus("Số lượng bản sao cần tạo phải từ 1 đến 100 cuốn.", true);
                return;
            }

            bool ok = _bookService.GenerateAndAddCopies(BookId, AddCopyCount, InitialCondition, out string error);
            if (ok)
            {
                SetStatus($"Đã sinh thành công {AddCopyCount} bản sao và mã vạch Barcode!", false);
                LoadData();
            }
            else
            {
                SetStatus("Không thể sinh bản sao: " + error, true);
            }
        }

        private void UpdateCopy()
        {
            if (SelectedCopy == null) return;

            bool ok = _bookService.UpdateCopyStatus(
                SelectedCopy.CopyId,
                SelectedCopyStatus,
                SelectedCopyConditionNote,
                out string error);

            if (ok)
            {
                SetStatus($"Đã cập nhật tình trạng bản sao '{SelectedCopy.Barcode}' thành công!", false);
                LoadData();
            }
            else
            {
                SetStatus("Cập nhật thất bại: " + error, true);
            }
        }

        private void DeleteCopy()
        {
            if (SelectedCopy == null) return;

            if (SelectedCopy.Status == "BORROWED")
            {
                SetStatus("Không thể xóa bản sao đang được độc giả mượn.", true);
                return;
            }

            if (!DialogService.ShowConfirmation(
                $"Bạn có chắc chắn muốn xóa bản sao '{SelectedCopy.Barcode}' không?",
                "Xác nhận xóa bản sao"))
            {
                return;
            }

            bool ok = _bookService.DeleteCopy(SelectedCopy.CopyId, out string error);
            if (ok)
            {
                SetStatus($"Đã xóa bản sao '{SelectedCopy.Barcode}' thành công!", false);
                LoadData();
            }
            else
            {
                SetStatus("Không thể xóa bản sao: " + error, true);
            }
        }

        private void PrintBarcode()
        {
            if (SelectedCopy == null) return;

            try
            {
                using (var bmp = _bookService.GenerateBarcodeBitmap(SelectedCopy.Barcode, BookInfo?.Title ?? SelectedCopy.BookTitle ?? "Sách Thư Viện"))
                {
                    string tempFile = Path.Combine(Path.GetTempPath(), $"Barcode_{SelectedCopy.Barcode}.png");
                    bmp.Save(tempFile, System.Drawing.Imaging.ImageFormat.Png);

                    var imageSource = new System.Windows.Media.Imaging.BitmapImage(new Uri(tempFile));
                    DialogService.ShowBarcodePreviewDialog(SelectedCopy.Barcode, imageSource);
                }
            }
            catch (Exception ex)
            {
                SetStatus("Lỗi in mã vạch: " + ex.Message, true);
            }
        }

        private void SetStatus(string message, bool isError)
        {
            StatusMessage = message;
            IsStatusError = isError;
        }
    }
}
