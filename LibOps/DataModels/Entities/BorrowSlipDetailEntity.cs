using System;

namespace LibOps.DataModels.Entities
{
    /// <summary>
    /// Thực thể ánh xạ bảng Chi Tiết Phiếu Mượn (BorrowSlipDetails)
    /// </summary>
    public class BorrowSlipDetailEntity
    {
        public int DetailId { get; set; }
        public int BorrowSlipId { get; set; }
        public int CopyId { get; set; }
        public string BorrowConditionNote { get; set; }
    }
}
