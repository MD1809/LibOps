using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LibOps.DataModels.DataTransferObjects
{
    /// <summary>
    /// DTO đại diện cho một cuốn sách trong giỏ trả sách hàng loạt (Bulk Return Cart)
    /// </summary>
    public class BulkReturnItemDto : INotifyPropertyChanged
    {
        public int BorrowSlipDetailId { get; set; }
        public int BorrowSlipId { get; set; }
        public string SlipCode { get; set; }
        public int CopyId { get; set; }
        public string Barcode { get; set; }
        public int BookId { get; set; }
        public string Title { get; set; }
        public string BookTitle => Title;
        public decimal BookPrice { get; set; }
        public string ShelfLocation { get; set; }

        public int MemberId { get; set; }
        public string MemberCardCode { get; set; }
        public string MemberFullName { get; set; }
        public string MemberPhone { get; set; }
        public decimal MemberDepositBalance { get; set; }

        public DateTime BorrowDate { get; set; }
        public DateTime DueDate { get; set; }
        public int OverdueDays { get; set; }
        public string OverdueDaysDisplay => OverdueDays > 0 ? $"Quá {OverdueDays} ngày" : "Đúng hạn";
        public decimal OverdueFine { get; set; }

        private string _conditionType = "AVAILABLE";
        private decimal _conditionFine = 0;
        private string _conditionNote = string.Empty;

        public decimal DamagedRate { get; set; } = 0.5m;
        public decimal LostRate { get; set; } = 2.0m;

        public event Action ConditionChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Trạng thái sách khi trả: AVAILABLE (Nguyên vẹn), DAMAGED (Hư hỏng nhẹ), LOST (Làm mất sách)
        /// </summary>
        public string ConditionType
        {
            get => _conditionType;
            set
            {
                if (_conditionType != value)
                {
                    _conditionType = value;
                    ApplyConditionFine();
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ConditionType));
                    OnPropertyChanged(nameof(ConditionFine));
                    OnPropertyChanged(nameof(ConditionNote));
                    OnPropertyChanged(nameof(ConditionDisplay));
                    OnPropertyChanged(nameof(TotalFine));
                    ConditionChanged?.Invoke();
                }
            }
        }

        public void ApplyConditionFine()
        {
            if (_conditionType == "DAMAGED")
            {
                _conditionFine = Math.Round(BookPrice * DamagedRate, 0);
                _conditionNote = "Hư hỏng";
            }
            else if (_conditionType == "LOST")
            {
                _conditionFine = Math.Round(BookPrice * LostRate, 0);
                _conditionNote = "Làm mất sách";
            }
            else
            {
                _conditionFine = 0;
                _conditionNote = "Nguyên vẹn";
            }
        }

        /// <summary>
        /// Tiền phạt hư hỏng hoặc bồi thường mất sách
        /// </summary>
        public decimal ConditionFine
        {
            get => _conditionFine;
            set
            {
                if (_conditionFine != value)
                {
                    _conditionFine = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TotalFine));
                }
            }
        }

        /// <summary>
        /// Ghi chú tình trạng sách lúc trả
        /// </summary>
        public string ConditionNote
        {
            get => _conditionNote;
            set
            {
                if (_conditionNote != value)
                {
                    _conditionNote = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Chuỗi hiển thị trên ComboBox của lưới giỏ trả sách (hỗ trợ two-way binding)
        /// </summary>
        public string ConditionDisplay
        {
            get
            {
                if (_conditionType == "DAMAGED") return $"Hư hỏng nhẹ ({DamagedRate * 100:0.#}%)";
                if (_conditionType == "LOST") return $"Làm mất sách ({LostRate * 100:0.#}%)";
                return "Nguyên vẹn (0 đ)";
            }
            set
            {
                if (value != null && (value.Contains("Hư hỏng") || value.Contains("DAMAGED")))
                {
                    ConditionType = "DAMAGED";
                }
                else if (value != null && (value.Contains("mất") || value.Contains("LOST")))
                {
                    ConditionType = "LOST";
                }
                else
                {
                    ConditionType = "AVAILABLE";
                }
            }
        }

        /// <summary>
        /// Tổng tiền phạt của cuốn sách này = Phạt quá hạn + Phạt hỏng/mất
        /// </summary>
        public decimal TotalFine => OverdueFine + ConditionFine;
    }
}
