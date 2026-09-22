using System;
using System.Collections.Generic;
using LibOps.DataAccessLayer.DataRepositories;
using LibOps.DataModels.Entities;

namespace LibOps.BusinessLogicLayer.BusinessServices
{
    /// <summary>
    /// Dịch vụ nghiệp vụ quản lý danh mục sách: Thể loại, Tác giả, Nhà xuất bản
    /// </summary>
    public class BookMetadataService
    {
        private readonly CategoryRepository _categoryRepo;
        private readonly AuthorRepository _authorRepo;
        private readonly PublisherRepository _publisherRepo;

        public BookMetadataService()
        {
            _categoryRepo = new CategoryRepository();
            _authorRepo = new AuthorRepository();
            _publisherRepo = new PublisherRepository();
        }

        public BookMetadataService(
            CategoryRepository categoryRepo,
            AuthorRepository authorRepo,
            PublisherRepository publisherRepo)
        {
            _categoryRepo = categoryRepo;
            _authorRepo = authorRepo;
            _publisherRepo = publisherRepo;
        }

        #region Nghiệp vụ Thể Loại (Categories)

        public virtual List<CategoryEntity> GetAllCategories(string keyword = null)
        {
            return _categoryRepo.GetAllCategories(keyword);
        }

        public virtual bool SaveCategory(CategoryEntity category, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (category == null || string.IsNullOrWhiteSpace(category.CategoryName))
            {
                errorMessage = "Vui lòng nhập tên thể loại sách.";
                return false;
            }

            if (_categoryRepo.IsCategoryNameExists(category.CategoryName, category.CategoryId))
            {
                errorMessage = "Tên thể loại sách này đã tồn tại trong hệ thống.";
                return false;
            }

            if (category.CategoryId == 0)
            {
                int newId = _categoryRepo.InsertCategory(category);
                if (newId > 0)
                {
                    category.CategoryId = newId;
                    return true;
                }
                errorMessage = "Lỗi hệ thống: Không thể thêm mới thể loại.";
                return false;
            }
            else
            {
                bool ok = _categoryRepo.UpdateCategory(category);
                if (!ok)
                {
                    errorMessage = "Lỗi hệ thống: Không thể cập nhật thể loại.";
                    return false;
                }
                return true;
            }
        }

        public virtual bool DeleteCategory(int categoryId, out string errorMessage)
        {
            errorMessage = string.Empty;

            int bookCount = _categoryRepo.CountBooksInCategory(categoryId);
            if (bookCount > 0)
            {
                errorMessage = $"Không thể xóa thể loại này vì hiện đang có {bookCount} đầu sách liên kết.";
                return false;
            }

            bool ok = _categoryRepo.DeleteCategory(categoryId);
            if (!ok)
            {
                errorMessage = "Lỗi hệ thống: Không thể xóa thể loại sách.";
                return false;
            }

            return true;
        }

        #endregion

        #region Nghiệp vụ Tác Giả (Authors) - Tinh gọn

        public virtual List<AuthorEntity> GetAllAuthors(string keyword = null)
        {
            return _authorRepo.GetAllAuthors(keyword);
        }

        public virtual bool SaveAuthor(AuthorEntity author, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (author == null || string.IsNullOrWhiteSpace(author.AuthorName))
            {
                errorMessage = "Vui lòng nhập họ và tên tác giả.";
                return false;
            }

            if (_authorRepo.IsAuthorNameExists(author.AuthorName, author.AuthorId))
            {
                errorMessage = "Tên tác giả này đã tồn tại trong hệ thống.";
                return false;
            }

            if (author.AuthorId == 0)
            {
                int newId = _authorRepo.InsertAuthor(author);
                if (newId > 0)
                {
                    author.AuthorId = newId;
                    return true;
                }
                errorMessage = "Lỗi hệ thống: Không thể thêm mới tác giả.";
                return false;
            }
            else
            {
                bool ok = _authorRepo.UpdateAuthor(author);
                if (!ok)
                {
                    errorMessage = "Lỗi hệ thống: Không thể cập nhật tác giả.";
                    return false;
                }
                return true;
            }
        }

        public virtual bool DeleteAuthor(int authorId, out string errorMessage)
        {
            errorMessage = string.Empty;

            int bookCount = _authorRepo.CountBooksByAuthor(authorId);
            if (bookCount > 0)
            {
                errorMessage = $"Không thể xóa tác giả này vì hiện đang có {bookCount} đầu sách liên kết.";
                return false;
            }

            bool ok = _authorRepo.DeleteAuthor(authorId);
            if (!ok)
            {
                errorMessage = "Lỗi hệ thống: Không thể xóa tác giả.";
                return false;
            }

            return true;
        }

        #endregion

        #region Nghiệp vụ Nhà Xuất Bản (Publishers)

        public virtual List<PublisherEntity> GetAllPublishers(string keyword = null)
        {
            return _publisherRepo.GetAllPublishers(keyword);
        }

        public virtual bool SavePublisher(PublisherEntity publisher, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (publisher == null || string.IsNullOrWhiteSpace(publisher.PublisherName))
            {
                errorMessage = "Vui lòng nhập tên nhà xuất bản.";
                return false;
            }

            if (_publisherRepo.IsPublisherNameExists(publisher.PublisherName, publisher.PublisherId))
            {
                errorMessage = "Tên nhà xuất bản này đã tồn tại trong hệ thống.";
                return false;
            }

            if (publisher.PublisherId == 0)
            {
                int newId = _publisherRepo.InsertPublisher(publisher);
                if (newId > 0)
                {
                    publisher.PublisherId = newId;
                    return true;
                }
                errorMessage = "Lỗi hệ thống: Không thể thêm mới nhà xuất bản.";
                return false;
            }
            else
            {
                bool ok = _publisherRepo.UpdatePublisher(publisher);
                if (!ok)
                {
                    errorMessage = "Lỗi hệ thống: Không thể cập nhật nhà xuất bản.";
                    return false;
                }
                return true;
            }
        }

        public virtual bool DeletePublisher(int publisherId, out string errorMessage)
        {
            errorMessage = string.Empty;

            int bookCount = _publisherRepo.CountBooksByPublisher(publisherId);
            if (bookCount > 0)
            {
                errorMessage = $"Không thể xóa nhà xuất bản này vì hiện đang có {bookCount} đầu sách liên kết.";
                return false;
            }

            bool ok = _publisherRepo.DeletePublisher(publisherId);
            if (!ok)
            {
                errorMessage = "Lỗi hệ thống: Không thể xóa nhà xuất bản.";
                return false;
            }

            return true;
        }

        #endregion
    }
}
