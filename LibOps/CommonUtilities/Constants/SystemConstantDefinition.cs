namespace LibOps.CommonUtilities.Constants
{
    /// <summary>
    /// Các định nghĩa hằng số hệ thống dùng chung
    /// </summary>
    public static class SystemConstantDefinition
    {
        public static class RoleNames
        {
            public const string Admin = "ADMIN";
            public const string Librarian = "LIBRARIAN";
        }

        public static class SettingKeys
        {
            public const string CardIssuanceFee = "CARD_ISSUANCE_FEE";
            public const string MemberCardDefaultDeposit = "MEMBER_CARD_DEFAULT_DEPOSIT";
            public const string MaxBorrowDays = "MAX_BORROW_DAYS";
            public const string MaxBooksPerMember = "MAX_BOOKS_PER_MEMBER";
            public const string FinePerOverdueDay = "FINE_PER_OVERDUE_DAY";
            public const string LostBookFineRate = "LOST_BOOK_FINE_RATE";
            public const string DamagedBookFineRate = "DAMAGED_BOOK_FINE_RATE";
            public const string MemberCardValidityDays = "MEMBER_CARD_VALIDITY_DAYS";
            public const string SmtpHost = "SMTP_HOST";
            public const string SmtpPort = "SMTP_PORT";
            public const string SmtpEnableSsl = "SMTP_ENABLE_SSL";
            public const string SmtpUsername = "SMTP_USERNAME";
            public const string SmtpPassword = "SMTP_PASSWORD";
            public const string SmtpFromName = "SMTP_FROM_NAME";
            public const string SystemEmailNotificationEnabled = "SYSTEM_EMAIL_NOTIFICATION_ENABLED";
        }

        public static class BookCopyStatuses
        {
            public const string Available = "AVAILABLE";
            public const string Borrowed = "BORROWED";
            public const string Damaged = "DAMAGED";
            public const string Lost = "LOST";
        }

        public static class MemberCardStatuses
        {
            public const string Active = "ACTIVE";
            public const string Locked = "LOCKED";
            public const string Expired = "EXPIRED";
            public const string Closed = "CLOSED";
        }

        public static class PaymentMethods
        {
            public const string Cash = "CASH";
            public const string DepositDeduction = "DEPOSIT_DEDUCTION";
        }

        public static class ServiceFeeTypes
        {
            public const string CardIssuance = "CARD_ISSUANCE";
            public const string AnnualRenewal = "ANNUAL_RENEWAL";
        }

        public static class DepositTransactionTypes
        {
            public const string InitialDeposit = "INITIAL_DEPOSIT";
            public const string TopUp = "TOP_UP";
            public const string FineDeduction = "FINE_DEDUCTION";
            public const string AnnualFeeDeduction = "ANNUAL_FEE_DEDUCTION";
            public const string DebtSettlement = "DEBT_SETTLEMENT";
            public const string Refund = "REFUND";
        }
    }
}
