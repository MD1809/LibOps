using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using LibOps.BusinessLogicLayer.BusinessServices;
using LibOps.DataModels.DataTransferObjects;
using LibOps.DataModels.Entities;
using LibOps.PresentationLayer.MvvmCore;
using LibOps.PresentationLayer.Services;

namespace LibOps.PresentationLayer.ViewModels
{
    public class BookDetailDialogViewModel : ViewModelBase
    {
        private readonly BookService _bookService;
        private readonly BookMetadataService _metaService;

        private int _bookId;
        private BookGridDisplayDto _book;
        private ObservableCollection<BookCopyGridDisplayDto> _copies;
        private bool _isModified;

        public event Action RequestClose;

        public int BookId
        {
            get => _bookId;
            set => SetProperty(ref _bookId, value);
        }

        public BookGridDisplayDto Book
        {
            get => _book;
            set
            {
                if (SetProperty(ref _book, value))
                {
                    OnPropertyChanged(nameof(HasCoverImage));
                    OnPropertyChanged(nameof(FormattedPrice));
                    OnPropertyChanged(nameof(AvailabilityDisplay));
                }
            }
        }

        public ObservableCollection<BookCopyGridDisplayDto> Copies
        {
            get => _copies;
            set => SetProperty(ref _copies, value);
        }

        public bool IsModified
        {
            get => _isModified;
            set => SetProperty(ref _isModified, value);
        }

        public bool HasCoverImage => Book != null && !string.IsNullOrWhiteSpace(Book.CoverImagePath) && 
            (File.Exists(Book.CoverImagePath) || File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Book.CoverImagePath)) || Book.CoverImagePath.StartsWith("http", StringComparison.OrdinalIgnoreCase));
        public string FormattedPrice => Book != null ? $"{Book.Price:N0} đ" : "0 đ";
        public string AvailabilityDisplay => Book != null && Book.AvailableQuantity > 0
            ? $"Sẵn sàng ({Book.AvailableQuantity}/{Book.TotalQuantity} cuốn)"
            : "Hết sách khả dụng";

        public ICommand CloseCommand { get; }
        public ICommand EditBookCommand { get; }
        public ICommand ManageCopiesCommand { get; }

        public BookDetailDialogViewModel(int bookId)
        {
            _bookService = new BookService();
            _metaService = new BookMetadataService();
            _bookId = bookId;
            Copies = new ObservableCollection<BookCopyGridDisplayDto>();

            CloseCommand = new RelayCommand(ExecuteClose);
            EditBookCommand = new RelayCommand(ExecuteEditBook);
            ManageCopiesCommand = new RelayCommand(ExecuteManageCopies);

            LoadBookDetails();
        }

        public void LoadBookDetails()
        {
            try
            {
                var allBooks = _bookService.GetAllBooks();
                var found = allBooks?.FirstOrDefault(b => b.BookId == _bookId);
                if (found != null)
                {
                    Book = found;
                }
                else
                {
                    var entity = _bookService.GetBookById(_bookId);
                    if (entity != null)
                    {
                        var cats = _metaService.GetAllCategories();
                        var authors = _metaService.GetAllAuthors();
                        var pubs = _metaService.GetAllPublishers();

                        Book = new BookGridDisplayDto
                        {
                            BookId = entity.BookId,
                            ISBN = entity.ISBN,
                            Title = entity.Title,
                            CategoryId = entity.CategoryId,
                            CategoryName = cats?.FirstOrDefault(c => c.CategoryId == entity.CategoryId)?.CategoryName ?? "Chưa phân loại",
                            AuthorId = entity.AuthorId,
                            AuthorName = authors?.FirstOrDefault(a => a.AuthorId == entity.AuthorId)?.AuthorName ?? "Chưa rõ",
                            PublisherId = entity.PublisherId,
                            PublisherName = pubs?.FirstOrDefault(p => p.PublisherId == entity.PublisherId)?.PublisherName ?? "Chưa rõ",
                            PublishYear = entity.PublishYear,
                            Price = entity.Price,
                            ShelfLocation = entity.ShelfLocation,
                            CoverImagePath = entity.CoverImagePath,
                            Summary = entity.Summary
                        };
                    }
                }

                // Load copies
                var copyList = _bookService.GetCopiesByBookId(_bookId);
                Copies.Clear();
                if (copyList != null)
                {
                    foreach (var copy in copyList)
                    {
                        Copies.Add(copy);
                    }
                }

                if (Book != null)
                {
                    Book.TotalQuantity = Copies.Count(c => c.Status != "LOST");
                    Book.AvailableQuantity = Copies.Count(c => c.Status == "AVAILABLE");
                    OnPropertyChanged(nameof(AvailabilityDisplay));
                }
            }
            catch (Exception ex)
            {
                DialogService.ShowError("Không thể nạp chi tiết sách: " + ex.Message);
            }
        }

        private void ExecuteEditBook()
        {
            var editVm = new BookAddEditViewModel(_bookId);
            if (DialogService.ShowBookAddEditDialog(editVm) == true)
            {
                IsModified = true;
                LoadBookDetails();
            }
        }

        private void ExecuteManageCopies()
        {
            var copyVm = new BookCopyBarcodeViewModel(_bookId);
            DialogService.ShowBookCopyManagementDialog(copyVm);
            IsModified = true;
            LoadBookDetails();
        }

        private void ExecuteClose()
        {
            RequestClose?.Invoke();
        }
    }
}
