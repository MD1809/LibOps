using System;
using System.Data.SqlClient;
using LibOps.DataAccessLayer.DatabaseConnection;

namespace LibOps.DataAccessLayer.DatabaseTransactions
{
    /// <summary>
    /// Quản lý vòng đời của giao dịch dữ liệu SqlTransaction theo mô hình IDisposable.
    /// Tự động Rollback khi gặp lỗi hoặc không được Commit rõ ràng.
    /// </summary>
    public sealed class TransactionExecutionScope : IDisposable
    {
        private SqlConnection _connection;
        private SqlTransaction _transaction;
        private bool _isCommitted;
        private bool _isDisposed;

        /// <summary>
        /// Khởi tạo và bắt đầu một giao dịch SqlTransaction mới
        /// </summary>
        public TransactionExecutionScope()
        {
            _connection = DatabaseConnectionHelper.CreateAndOpenConnection();
            _transaction = _connection.BeginTransaction();
            _isCommitted = false;
        }

        /// <summary>
        /// Đối tượng giao dịch SqlTransaction hiện hành
        /// </summary>
        public SqlTransaction Transaction => _transaction;

        /// <summary>
        /// Kết nối SqlConnection hiện hành của giao dịch
        /// </summary>
        public SqlConnection Connection => _connection;

        /// <summary>
        /// Xác nhận lưu toàn bộ các thay đổi trong giao dịch vào CSDL
        /// </summary>
        public void Commit()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(TransactionExecutionScope));
            }

            if (_isCommitted)
            {
                return;
            }

            _transaction?.Commit();
            _isCommitted = true;
        }

        /// <summary>
        /// Hủy bỏ toàn bộ các thay đổi trong giao dịch và hoàn nguyên dữ liệu
        /// </summary>
        public void Rollback()
        {
            if (_isDisposed || _isCommitted)
            {
                return;
            }

            try
            {
                _transaction?.Rollback();
            }
            catch
            {
                // Bỏ qua lỗi nếu kết nối đã bị đóng trước đó
            }
        }

        /// <summary>
        /// Giải phóng tài nguyên kết nối và giao dịch
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            if (!_isCommitted)
            {
                Rollback();
            }

            _transaction?.Dispose();
            _transaction = null;

            _connection?.Close();
            _connection?.Dispose();
            _connection = null;

            _isDisposed = true;
        }
    }
}
