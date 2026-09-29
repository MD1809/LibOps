using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Microsoft.Win32;
using LibOps.BusinessLogicLayer.BusinessServices;
using LibOps.DataModels.Entities;
using LibOps.PresentationLayer.MvvmCore;
using LibOps.PresentationLayer.Services;

namespace LibOps.PresentationLayer.ViewModels
{
    public class BookAddEditViewModel : ViewModelBase
    {
        private readonly BookService _bookService;
        private readonly BookMetadataService _metadataService;

        private int _bookId;
        private string _isbn;
        private string _title;
        private int _selectedCategoryId;
        private int _selectedAuthorId;
        private AuthorEntity _selectedAuthorToAdd;
        private string _authorInputText = string.Empty;
        private int _selectedPublisherId;
        private string _publisherInputText = string.Empty;
        private int _publishYear = DateTime.Now.Year;
        private decimal _price;
        private string _priceInput = "0";
        private string _shelfLocation;
        private string _coverImagePath;
        private string _summary;
        private string _errorMessage;
        private bool _isSavedSuccessfully;

        public string DialogTitle => _bookId == 0 ? "THÊM MỚI ĐẦU SÁCH & TÀI LIỆU" : "CẬP NHẬT THÔNG TIN ĐẦU SÁCH";

        public int BookId
        {
            get => _bookId;
            set
            {
                if (SetProperty(ref _bookId, value))
                {
                    OnPropertyChanged(nameof(DialogTitle));
                    OnPropertyChanged(nameof(IsNewBook));
                }
            }
        }

        public bool IsNewBook => _bookId == 0;

        public string ISBN
        {
            get => _isbn;
            set => SetProperty(ref _isbn, value);
        }

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public int SelectedCategoryId
        {
            get => _selectedCategoryId;
            set => SetProperty(ref _selectedCategoryId, value);
        }

        public int SelectedAuthorId
        {
            get => _selectedAuthorId;
            set => SetProperty(ref _selectedAuthorId, value);
        }

        public AuthorEntity SelectedAuthorToAdd
        {
            get => _selectedAuthorToAdd;
            set
            {
                if (SetProperty(ref _selectedAuthorToAdd, value) && value != null)
                {
                    AuthorInputText = value.AuthorName;
                }
            }
        }

        public string AuthorInputText
        {
            get => _authorInputText;
            set => SetProperty(ref _authorInputText, value);
        }

        public int SelectedPublisherId
        {
            get => _selectedPublisherId;
            set
            {
                if (SetProperty(ref _selectedPublisherId, value))
                {
                    var pub = Publishers.FirstOrDefault(p => p.PublisherId == value);
                    if (pub != null && _publisherInputText != pub.PublisherName)
                    {
                        _publisherInputText = pub.PublisherName;
                        OnPropertyChanged(nameof(PublisherInputText));
                    }
                }
            }
        }

        public string PublisherInputText
        {
            get => _publisherInputText;
            set => SetProperty(ref _publisherInputText, value);
        }

        public int PublishYear
        {
            get => _publishYear;
            set => SetProperty(ref _publishYear, value);
        }

        public decimal Price
        {
            get => _price;
            set
            {
                if (SetProperty(ref _price, value))
                {
                    _priceInput = _price > 0 ? _price.ToString("N0") : "0";
                    OnPropertyChanged(nameof(PriceInput));
                }
            }
        }

        public string PriceInput
        {
            get => _priceInput;
            set
            {
                if (SetProperty(ref _priceInput, value))
                {
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        _price = 0;
                    }
                    else
                    {
                        string clean = value.Replace(".", "").Replace(",", "").Replace(" ", "").Replace("đ", "").Replace("VNĐ", "").Trim();
                        if (decimal.TryParse(clean, out decimal val))
                        {
                            _price = val >= 0 ? val : 0;
                        }
                    }
                    OnPropertyChanged(nameof(Price));
                }
            }
        }

        public string ShelfLocation
        {
            get => _shelfLocation;
            set => SetProperty(ref _shelfLocation, value);
        }

        public string CoverImagePath
        {
            get => _coverImagePath;
            set
            {
                if (SetProperty(ref _coverImagePath, value))
                {
                    OnPropertyChanged(nameof(HasCoverImage));
                }
            }
        }

        public bool HasCoverImage => !string.IsNullOrWhiteSpace(_coverImagePath);

        public string Summary
        {
            get => _summary;
            set => SetProperty(ref _summary, value);
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

        public ObservableCollection<CategoryEntity> Categories { get; }
        public ObservableCollection<AuthorEntity> Authors { get; }
        public ObservableCollection<PublisherEntity> Publishers { get; }
        public ObservableCollection<AuthorEntity> SelectedAuthors { get; }

        public ICommand SaveCommand { get; }
        public ICommand ChooseCoverImageCommand { get; }
        public ICommand RemoveCoverImageCommand { get; }
        public ICommand AddAuthorToBookCommand { get; }
        public ICommand RemoveAuthorFromBookCommand { get; }
        public ICommand QuickAddCategoryCommand { get; }
        public ICommand QuickAddAuthorCommand { get; }
        public ICommand QuickAddPublisherCommand { get; }

        public event Action RequestClose;

        public BookAddEditViewModel(int bookId = 0)
        {
            _bookService = new BookService();
            _metadataService = new BookMetadataService();

            Categories = new ObservableCollection<CategoryEntity>();
            Authors = new ObservableCollection<AuthorEntity>();
            Publishers = new ObservableCollection<PublisherEntity>();
            SelectedAuthors = new ObservableCollection<AuthorEntity>();

            SaveCommand = new RelayCommand(SaveBook);
            ChooseCoverImageCommand = new RelayCommand(ExecuteChooseCoverImage);
            RemoveCoverImageCommand = new RelayCommand(() => CoverImagePath = null);
            AddAuthorToBookCommand = new RelayCommand(ExecuteAddAuthorToBook);
            RemoveAuthorFromBookCommand = new RelayCommand<AuthorEntity>(ExecuteRemoveAuthorFromBook);

            QuickAddCategoryCommand = new RelayCommand(ExecuteQuickAddCategory);
            QuickAddAuthorCommand = new RelayCommand(ExecuteQuickAddAuthor);
            QuickAddPublisherCommand = new RelayCommand(ExecuteQuickAddPublisher);

            LoadMetadata();

            if (bookId > 0)
            {
                LoadBookData(bookId);
            }
        }

        private void ExecuteAddAuthorToBook()
        {
            string nameToAdd = !string.IsNullOrWhiteSpace(AuthorInputText) ? AuthorInputText.Trim() : SelectedAuthorToAdd?.AuthorName?.Trim();
            if (string.IsNullOrWhiteSpace(nameToAdd)) return;

            // Kiểm tra xem tác giả đã tồn tại trong danh mục hệ thống chưa
            var existingAuthor = Authors.FirstOrDefault(a => string.Equals(a.AuthorName?.Trim(), nameToAdd, StringComparison.OrdinalIgnoreCase));
            if (existingAuthor != null)
            {
                if (!SelectedAuthors.Any(a => a.AuthorId == existingAuthor.AuthorId))
                {
                    SelectedAuthors.Add(existingAuthor);
                    if (SelectedAuthorId == 0)
                    {
                        SelectedAuthorId = existingAuthor.AuthorId;
                    }
                }
            }
            else
            {
                // Tự động thêm mới tác giả vào CSDL nếu chưa có
                var newAuthor = new AuthorEntity { AuthorName = nameToAdd };
                if (_metadataService.SaveAuthor(newAuthor, out string err))
                {
                    Authors.Add(newAuthor);
                    SelectedAuthors.Add(newAuthor);
                    if (SelectedAuthorId == 0)
                    {
                        SelectedAuthorId = newAuthor.AuthorId;
                    }
                }
                else
                {
                    ErrorMessage = "Không thể tự động thêm tác giả mới: " + err;
                    return;
                }
            }

            // Xóa trắng ô gõ để sẵn sàng nhập tác giả tiếp theo
            AuthorInputText = string.Empty;
            SelectedAuthorToAdd = null;
        }

        private void ExecuteRemoveAuthorFromBook(AuthorEntity author)
        {
            if (author == null) return;
            SelectedAuthors.Remove(author);
            if (SelectedAuthorId == author.AuthorId)
            {
                SelectedAuthorId = SelectedAuthors.FirstOrDefault()?.AuthorId ?? 0;
            }
        }

        private void ExecuteQuickAddCategory()
        {
            var vm = new QuickAddMetadataViewModel(QuickAddMetadataMode.Category);
            if (DialogService.ShowQuickAddMetadataDialog(vm) == true && vm.CreatedId > 0)
            {
                var newCat = new CategoryEntity
                {
                    CategoryId = vm.CreatedId,
                    CategoryName = vm.CreatedName,
                    Description = vm.SecondaryValue
                };
                Categories.Add(newCat);
                SelectedCategoryId = vm.CreatedId;
            }
        }

        private void ExecuteQuickAddAuthor()
        {
            var vm = new QuickAddMetadataViewModel(QuickAddMetadataMode.Author);
            if (DialogService.ShowQuickAddMetadataDialog(vm) == true && vm.CreatedId > 0)
            {
                var newAuthor = new AuthorEntity
                {
                    AuthorId = vm.CreatedId,
                    AuthorName = vm.CreatedName,
                    Notes = vm.SecondaryValue
                };
                Authors.Add(newAuthor);
                SelectedAuthorToAdd = newAuthor;
                AuthorInputText = newAuthor.AuthorName;
                if (!SelectedAuthors.Any(a => a.AuthorId == newAuthor.AuthorId))
                {
                    SelectedAuthors.Add(newAuthor);
                }
                SelectedAuthorId = newAuthor.AuthorId;
            }
        }

        private void ExecuteQuickAddPublisher()
        {
            var vm = new QuickAddMetadataViewModel(QuickAddMetadataMode.Publisher);
            if (DialogService.ShowQuickAddMetadataDialog(vm) == true && vm.CreatedId > 0)
            {
                var newPub = new PublisherEntity
                {
                    PublisherId = vm.CreatedId,
                    PublisherName = vm.CreatedName,
                    Address = vm.SecondaryValue
                };
                Publishers.Add(newPub);
                SelectedPublisherId = vm.CreatedId;
                PublisherInputText = newPub.PublisherName;
            }
        }

        private void ExecuteChooseCoverImage()
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    Title = "Chọn ảnh bìa sách / tài liệu",
                    Filter = "File hình ảnh (*.jpg;*.jpeg;*.png;*.webp;*.bmp)|*.jpg;*.jpeg;*.png;*.webp;*.bmp|Tất cả tệp (*.*)|*.*",
                    CheckFileExists = true
                };

                if (dialog.ShowDialog() == true)
                {
                    CoverImagePath = dialog.FileName;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "Lỗi khi chọn ảnh bìa: " + ex.Message;
            }
        }

        private void LoadMetadata()
        {
            try
            {
                Categories.Clear();
                foreach (var c in _metadataService.GetAllCategories())
                {
                    Categories.Add(c);
                }
                if (Categories.Count > 0 && SelectedCategoryId == 0)
                {
                    SelectedCategoryId = Categories[0].CategoryId;
                }

                Authors.Clear();
                foreach (var a in _metadataService.GetAllAuthors())
                {
                    Authors.Add(a);
                }
                SelectedAuthorToAdd = null;
                AuthorInputText = string.Empty;

                Publishers.Clear();
                foreach (var p in _metadataService.GetAllPublishers())
                {
                    Publishers.Add(p);
                }
                if (Publishers.Count > 0 && SelectedPublisherId == 0)
                {
                    SelectedPublisherId = Publishers[0].PublisherId;
                    PublisherInputText = Publishers[0].PublisherName;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "Không thể tải danh mục liên quan: " + ex.Message;
            }
        }

        private void LoadBookData(int bookId)
        {
            try
            {
                var book = _bookService.GetBookById(bookId);
                if (book != null)
                {
                    BookId = book.BookId;
                    ISBN = book.ISBN;
                    Title = book.Title;
                    SelectedCategoryId = book.CategoryId;
                    SelectedAuthorId = book.AuthorId;
                    SelectedPublisherId = book.PublisherId;
                    PublishYear = book.PublishYear;
                    Price = book.Price;
                    PriceInput = book.Price > 0 ? book.Price.ToString("N0") : "0";
                    ShelfLocation = book.ShelfLocation;
                    CoverImagePath = book.CoverImagePath;
                    Summary = book.Summary;

                    var currentPub = Publishers.FirstOrDefault(p => p.PublisherId == book.PublisherId);
                    if (currentPub != null)
                    {
                        PublisherInputText = currentPub.PublisherName;
                    }

                    // Tải danh sách các tác giả của sách
                    SelectedAuthors.Clear();
                    var authors = _bookService.GetAuthorsByBookId(bookId);
                    if (authors != null && authors.Count > 0)
                    {
                        foreach (var auth in authors)
                        {
                            SelectedAuthors.Add(auth);
                        }
                    }
                    else if (book.AuthorId > 0)
                    {
                        var singleAuthor = Authors.FirstOrDefault(a => a.AuthorId == book.AuthorId);
                        if (singleAuthor != null)
                        {
                            SelectedAuthors.Add(singleAuthor);
                        }
                    }

                    AuthorInputText = string.Empty;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "Không thể tải thông tin đầu sách: " + ex.Message;
            }
        }

        private void SaveBook()
        {
            ErrorMessage = string.Empty;

            try
            {
                // 1. Validate Tên tựa đề sách (*)
                if (string.IsNullOrWhiteSpace(Title))
                {
                    ErrorMessage = "Vui lòng nhập tên tựa đề sách.";
                    return;
                }

                if (Title.Trim().Length > 250)
                {
                    ErrorMessage = "Tên tựa đề sách không được vượt quá 250 ký tự.";
                    return;
                }

                // 2. Validate Mã chuẩn ISBN (*)
                if (string.IsNullOrWhiteSpace(ISBN))
                {
                    ErrorMessage = "Vui lòng nhập mã chuẩn ISBN của sách.";
                    return;
                }

                string cleanIsbn = ISBN.Replace("-", "").Replace(" ", "").Trim();
                if (cleanIsbn.Length < 8 || cleanIsbn.Length > 20)
                {
                    ErrorMessage = "Mã chuẩn ISBN không đúng định dạng (độ dài thông thường từ 10 đến 17 ký tự).";
                    return;
                }

                // 3. Validate Giá bìa (*)
                if (Price <= 0)
                {
                    ErrorMessage = "Vui lòng nhập giá bìa sách hợp lệ (lớn hơn 0 VNĐ).";
                    return;
                }

                if (Price > 100000000)
                {
                    ErrorMessage = "Giá bìa sách vượt quá giới hạn cho phép (tối đa 100,000,000 VNĐ).";
                    return;
                }

                // 4. Validate Thể loại (*)
                if (SelectedCategoryId <= 0)
                {
                    ErrorMessage = "Vui lòng chọn thể loại cho đầu sách.";
                    return;
                }

                // 5. Validate Tác giả (*)
                // Tự động thêm tác giả nếu người dùng gõ vào ô nhập liệu mà chưa bấm 'Thêm Tác Giả'
                if (!string.IsNullOrWhiteSpace(AuthorInputText))
                {
                    ExecuteAddAuthorToBook();
                }

                if (SelectedAuthors.Count == 0 && SelectedAuthorId <= 0)
                {
                    ErrorMessage = "Vui lòng chọn hoặc nhập ít nhất một tác giả cho sách.";
                    return;
                }

                // 6. Validate Nhà xuất bản (*)
                string pubName = PublisherInputText?.Trim();
                if (!string.IsNullOrWhiteSpace(pubName))
                {
                    var existingPub = Publishers.FirstOrDefault(p => string.Equals(p.PublisherName?.Trim(), pubName, StringComparison.OrdinalIgnoreCase));
                    if (existingPub != null)
                    {
                        SelectedPublisherId = existingPub.PublisherId;
                    }
                    else
                    {
                        // Tự động tạo mới nhà xuất bản vào CSDL
                        var newPub = new PublisherEntity { PublisherName = pubName };
                        if (_metadataService.SavePublisher(newPub, out string err))
                        {
                            Publishers.Add(newPub);
                            SelectedPublisherId = newPub.PublisherId;
                        }
                        else
                        {
                            ErrorMessage = "Không thể tự động thêm nhà xuất bản mới: " + err;
                            return;
                        }
                    }
                }

                if (SelectedPublisherId <= 0)
                {
                    ErrorMessage = "Vui lòng nhập hoặc chọn nhà xuất bản.";
                    return;
                }

                // 7. Validate Năm xuất bản
                if (PublishYear < 1000 || PublishYear > DateTime.Now.Year + 1)
                {
                    ErrorMessage = $"Năm xuất bản không hợp lệ (phải từ năm 1000 đến {DateTime.Now.Year + 1}).";
                    return;
                }

                var authorIds = SelectedAuthors.Select(a => a.AuthorId).Distinct().ToList();
                if (authorIds.Count == 0 && SelectedAuthorId > 0)
                {
                    authorIds.Add(SelectedAuthorId);
                }

                int primaryAuthorId = authorIds.Count > 0 ? authorIds[0] : SelectedAuthorId;

                var book = new BookEntity
                {
                    BookId = BookId,
                    ISBN = ISBN?.Trim(),
                    Title = Title?.Trim(),
                    CategoryId = SelectedCategoryId,
                    AuthorId = primaryAuthorId,
                    PublisherId = SelectedPublisherId,
                    PublishYear = PublishYear,
                    Price = Price,
                    ShelfLocation = ShelfLocation?.Trim(),
                    CoverImagePath = CoverImagePath?.Trim(),
                    Summary = Summary?.Trim()
                };

                bool success = _bookService.SaveBook(book, authorIds, out string error);
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
            catch (Exception ex)
            {
                ErrorMessage = "Đã xảy ra lỗi khi lưu thông tin sách: " + ex.Message;
            }
        }
    }
}
