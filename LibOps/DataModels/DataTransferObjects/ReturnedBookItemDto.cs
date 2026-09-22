using System;

namespace LibOps.DataModels.DataTransferObjects
{
    /// <summary>
    /// DTO đại diện cho một cuốn sách được trả phục vụ biên nhận và gửi email thông báo
    /// </summary>
    public class ReturnedBookItemDto
    {
        public string Barcode { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string ConditionType { get; set; } = "AVAILABLE"; // AVAILABLE, DAMAGED, LOST
        public string ConditionNote { get; set; } = string.Empty;
        public int OverdueDays { get; set; }
        public decimal OverdueFine { get; set; }
        public decimal ConditionFine { get; set; }
        public decimal TotalFine => OverdueFine + ConditionFine;

        public string ConditionDisplay
        {
            get
            {
                if (ConditionType == "DAMAGED") return "Hư hỏng nhẹ";
                if (ConditionType == "LOST") return "Làm mất sách";
                return "Nguyên vẹn";
            }
        }
    }
}
