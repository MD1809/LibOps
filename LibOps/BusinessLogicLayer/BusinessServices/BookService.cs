using System;
using System.Collections.Generic;
using System.Drawing;
using LibOps.DataAccessLayer.DataRepositories;
using LibOps.DataModels.DataTransferObjects;
using LibOps.DataModels.Entities;

namespace LibOps.BusinessLogicLayer.BusinessServices
{
    /// <summary>
    /// Dịch vụ nghiệp vụ quản lý Đầu sách, Bản sao và Mã vạch Barcode
    /// </summary>
    public class BookService
    {
        private readonly BookRepository _bookRepo;
        private readonly BookCopyRepository _copyRepo;

        public BookService()
        {
            _bookRepo = new BookRepository();
            _copyRepo = new BookCopyRepository();
        }

        public BookService(BookRepository bookRepo, BookCopyRepository copyRepo)
        {
            _bookRepo = bookRepo;
            _copyRepo = copyRepo;
        }

        #region Nghiệp vụ Đầu Sách (Books)

        public virtual List<BookGridDisplayDto> GetAllBooks(string keyword = null, int? categoryId = null, int? authorId = null, int? publisherId = null)
        {
            return _bookRepo.GetAllBooks(keyword, categoryId, authorId, publisherId);
        }

        public virtual BookEntity GetBookById(int bookId)
        {
            return _bookRepo.GetBookById(bookId);
        }

        public virtual List<int> GetAuthorIdsByBookId(int bookId)
        {
            return _bookRepo.GetAuthorIdsByBookId(bookId);
        }

        public virtual List<AuthorEntity> GetAuthorsByBookId(int bookId)
        {
            return _bookRepo.GetAuthorsByBookId(bookId);
        }

        public virtual bool SaveBook(BookEntity book, out string errorMessage)
        {
            var authorIds = book != null && book.AuthorId > 0 ? new List<int> { book.AuthorId } : null;
            return SaveBook(book, authorIds, out errorMessage);
        }

        public virtual bool SaveBook(BookEntity book, List<int> authorIds, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (book == null)
            {
                errorMessage = "Dữ liệu đầu sách không hợp lệ.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(book.Title))
            {
                errorMessage = "Vui lòng nhập tên tựa đề sách.";
                return false;
            }

            if (book.Title.Trim().Length > 250)
            {
                errorMessage = "Tên tựa đề sách không được vượt quá 250 ký tự.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(book.ISBN))
            {
                errorMessage = "Vui lòng nhập mã chuẩn ISBN của sách.";
                return false;
            }

            string cleanIsbn = book.ISBN.Replace("-", "").Replace(" ", "").Trim();
            if (cleanIsbn.Length < 8 || cleanIsbn.Length > 20)
            {
                errorMessage = "Mã chuẩn ISBN không đúng định dạng (độ dài thông thường từ 10 đến 17 ký tự).";
                return false;
            }

            if (book.CategoryId <= 0)
            {
                errorMessage = "Vui lòng chọn thể loại sách.";
                return false;
            }

            if ((authorIds == null || authorIds.Count == 0) && book.AuthorId <= 0)
            {
                errorMessage = "Vui lòng chọn ít nhất một tác giả cho sách.";
                return false;
            }

            if (authorIds != null && authorIds.Count > 0)
            {
                book.AuthorId = authorIds[0];
            }

            if (book.PublisherId <= 0)
            {
                errorMessage = "Vui lòng chọn nhà xuất bản.";
                return false;
            }

            if (book.Price <= 0)
            {
                errorMessage = "Giá bìa sách phải lớn hơn 0 VNĐ.";
                return false;
            }

            if (book.Price > 100000000)
            {
                errorMessage = "Giá bìa sách vượt quá giới hạn cho phép (tối đa 100,000,000 VNĐ).";
                return false;
            }

            if (book.PublishYear < 1000 || book.PublishYear > DateTime.Now.Year + 1)
            {
                errorMessage = $"Năm xuất bản không hợp lệ (phải từ năm 1000 đến {DateTime.Now.Year + 1}).";
                return false;
            }

            try
            {
                if (_bookRepo.IsIsbnExists(book.ISBN.Trim(), book.BookId))
                {
                    errorMessage = "Mã chuẩn ISBN này đã tồn tại trên một đầu sách khác trong hệ thống.";
                    return false;
                }

                if (book.BookId == 0)
                {
                    int newId = _bookRepo.InsertBook(book, authorIds);
                    if (newId > 0)
                    {
                        book.BookId = newId;
                        return true;
                    }
                    errorMessage = "Lỗi hệ thống: Không thể thêm mới đầu sách vào cơ sở dữ liệu.";
                    return false;
                }
                else
                {
                    bool ok = _bookRepo.UpdateBook(book, authorIds);
                    if (!ok)
                    {
                        errorMessage = "Lỗi hệ thống: Không thể cập nhật thông tin đầu sách.";
                        return false;
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                errorMessage = "Lỗi khi lưu đầu sách: " + ex.Message;
                return false;
            }
        }

        public virtual bool DeleteBook(int bookId, out string errorMessage)
        {
            errorMessage = string.Empty;

            var copies = _copyRepo.GetCopiesByBookId(bookId);
            foreach (var copy in copies)
            {
                if (copy.Status == "BORROWED")
                {
                    errorMessage = "Không thể xóa đầu sách này vì hiện đang có bản sao đang được độc giả mượn.";
                    return false;
                }
            }

            // Xóa toàn bộ bản sao trước
            foreach (var copy in copies)
            {
                _copyRepo.DeleteCopy(copy.CopyId);
            }

            bool ok = _bookRepo.DeleteBook(bookId);
            if (!ok)
            {
                errorMessage = "Lỗi hệ thống: Không thể xóa đầu sách.";
                return false;
            }

            return true;
        }

        #endregion

        #region Nghiệp vụ Bản Sao & Sinh Mã Vạch (BookCopies & Barcode)

        public virtual List<BookCopyGridDisplayDto> GetCopiesByBookId(int bookId)
        {
            return _copyRepo.GetCopiesByBookId(bookId);
        }

        public virtual BookCopyEntity GetCopyByBarcode(string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode))
            {
                return null;
            }
            return _copyRepo.GetCopyByBarcode(barcode.Trim());
        }

        public virtual bool GenerateAndAddCopies(int bookId, int quantity, string initialCondition, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (bookId <= 0)
            {
                errorMessage = "Đầu sách không hợp lệ.";
                return false;
            }

            if (quantity <= 0 || quantity > 100)
            {
                errorMessage = "Số lượng bản sao cần tạo phải từ 1 đến 100 cuốn mỗi lần.";
                return false;
            }

            int startIndex = _copyRepo.GetNextCopyIndex(bookId);

            for (int i = 0; i < quantity; i++)
            {
                int copyIndex = startIndex + i;
                string barcode = $"BC-B{bookId:D4}-{copyIndex:D4}";

                // Đảm bảo không trùng barcode
                while (_copyRepo.IsBarcodeExists(barcode))
                {
                    copyIndex++;
                    barcode = $"BC-B{bookId:D4}-{copyIndex:D4}";
                }

                var copy = new BookCopyEntity
                {
                    BookId = bookId,
                    Barcode = barcode,
                    Status = "AVAILABLE",
                    ConditionNote = string.IsNullOrWhiteSpace(initialCondition) ? "Mới 100%" : initialCondition.Trim(),
                    AddedDate = DateTime.Now
                };

                _copyRepo.InsertCopy(copy);
            }

            // Cập nhật lại số lượng trong bảng Books
            _bookRepo.UpdateBookCopyCounts(bookId);
            return true;
        }

        public virtual bool UpdateCopyStatus(int copyId, string newStatus, string conditionNote, out string errorMessage)
        {
            errorMessage = string.Empty;

            var copy = _copyRepo.GetCopyById(copyId);
            if (copy == null)
            {
                errorMessage = "Bản sao sách không tồn tại.";
                return false;
            }

            copy.Status = newStatus;
            copy.ConditionNote = conditionNote;

            bool ok = _copyRepo.UpdateCopy(copy);
            if (ok)
            {
                _bookRepo.UpdateBookCopyCounts(copy.BookId);
                return true;
            }

            errorMessage = "Lỗi hệ thống: Không thể cập nhật trạng thái bản sao.";
            return false;
        }

        public virtual bool DeleteCopy(int copyId, out string errorMessage)
        {
            errorMessage = string.Empty;

            var copy = _copyRepo.GetCopyById(copyId);
            if (copy == null)
            {
                errorMessage = "Bản sao sách không tồn tại.";
                return false;
            }

            if (copy.Status == "BORROWED")
            {
                errorMessage = "Không thể xóa bản sao đang trong trạng thái Đang mượn.";
                return false;
            }

            int bookId = copy.BookId;
            bool ok = _copyRepo.DeleteCopy(copyId);
            if (ok)
            {
                _bookRepo.UpdateBookCopyCounts(bookId);
                return true;
            }

            errorMessage = "Lỗi hệ thống: Không thể xóa bản sao.";
            return false;
        }

        /// <summary>
        /// Tạo ảnh Bitmap mã vạch chuẩn để in tem dán lên gáy sách
        /// </summary>
        public virtual Bitmap GenerateBarcodeBitmap(string barcodeText, string title = "")
        {
            int width = 280;
            int height = 110;
            var bmp = new Bitmap(width, height);

            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);

                // Viền khung
                using (var pen = new Pen(Color.FromArgb(203, 213, 225), 1))
                {
                    g.DrawRectangle(pen, 0, 0, width - 1, height - 1);
                }

                // Tiêu đề sách (rút gọn)
                using (var fontTitle = new Font("Segoe UI", 8F, FontStyle.Bold))
                using (var brush = new SolidBrush(Color.FromArgb(30, 41, 59)))
                {
                    string dispTitle = string.IsNullOrWhiteSpace(title) ? "THƯ VIỆN LIBOPS" : title;
                    if (dispTitle.Length > 28)
                    {
                        dispTitle = dispTitle.Substring(0, 25) + "...";
                    }
                    g.DrawString(dispTitle, fontTitle, brush, new PointF(10, 8));
                }

                // Vẽ các vạch barcode mô phỏng Code128 tỷ lệ chuẩn
                using (var brushBar = new SolidBrush(Color.Black))
                {
                    int startX = 14;
                    int barHeight = 48;
                    int currentX = startX;

                    byte[] textBytes = System.Text.Encoding.ASCII.GetBytes(barcodeText ?? "LIB-0000");
                    for (int i = 0; i < textBytes.Length; i++)
                    {
                        int val = textBytes[i];
                        int barWidth = (val % 3) + 1;
                        int gap = ((val / 3) % 2) + 1;

                        g.FillRectangle(brushBar, currentX, 32, barWidth, barHeight);
                        currentX += barWidth + gap;

                        if (currentX > width - 20)
                        {
                            break;
                        }
                    }
                }

                // Mã text Barcode bên dưới
                using (var fontCode = new Font("Consolas", 9.5F, FontStyle.Bold))
                using (var brushCode = new SolidBrush(Color.FromArgb(15, 23, 42)))
                {
                    g.DrawString(barcodeText ?? string.Empty, fontCode, brushCode, new PointF(14, 84));
                }
            }

            return bmp;
        }

        #endregion
    }
}
