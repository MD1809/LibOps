using System;
using System.Globalization;
using LibOps.DataAccessLayer.DataRepositories;

namespace LibOps.BusinessLogicLayer.BusinessServices
{
    /// <summary>
    /// Dịch vụ quản lý các tham số cấu hình quy định hệ thống động
    /// </summary>
    public class SystemSettingsService
    {
        private readonly SystemSettingRepository _settingRepo;

        public SystemSettingsService()
        {
            _settingRepo = new SystemSettingRepository();
        }

        public SystemSettingsService(SystemSettingRepository repo)
        {
            _settingRepo = repo;
        }

        public decimal GetCardIssuanceFee()
        {
            string val = _settingRepo.GetSettingValue("CARD_ISSUANCE_FEE", "50000");
            return decimal.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal res) ? res : 50000m;
        }

        public decimal GetDefaultDeposit()
        {
            string val = _settingRepo.GetSettingValue("MEMBER_CARD_DEFAULT_DEPOSIT", "200000");
            return decimal.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal res) ? res : 200000m;
        }

        public int GetMemberValidityDays()
        {
            string val = _settingRepo.GetSettingValue("MEMBER_CARD_VALIDITY_DAYS", "365");
            return int.TryParse(val, out int res) ? res : 365;
        }

        public int GetMaxBorrowDays()
        {
            string val = _settingRepo.GetSettingValue("MAX_BORROW_DAYS", "14");
            return int.TryParse(val, out int res) ? res : 14;
        }

        public int GetMaxBooksPerMember()
        {
            string val = _settingRepo.GetSettingValue("MAX_BOOKS_PER_MEMBER", "5");
            return int.TryParse(val, out int res) ? res : 5;
        }

        public decimal GetFinePerOverdueDay()
        {
            string val = _settingRepo.GetSettingValue("FINE_PER_OVERDUE_DAY", "5000");
            return decimal.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal res) ? res : 5000m;
        }

        public decimal GetLostBookFineRate()
        {
            string val = _settingRepo.GetSettingValue("LOST_BOOK_FINE_RATE", "2.0");
            if (decimal.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal res))
            {
                return res > 10 ? res / 100m : res;
            }
            if (decimal.TryParse(val, NumberStyles.Any, CultureInfo.CurrentCulture, out res))
            {
                return res > 10 ? res / 100m : res;
            }
            return 2.0m;
        }

        public decimal GetDamagedBookFineRate()
        {
            string val = _settingRepo.GetSettingValue("DAMAGED_BOOK_FINE_RATE", "0.5");
            if (decimal.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal res))
            {
                return res > 10 ? res / 100m : res;
            }
            if (decimal.TryParse(val, NumberStyles.Any, CultureInfo.CurrentCulture, out res))
            {
                return res > 10 ? res / 100m : res;
            }
            return 0.5m;
        }

        public int GetMaxRenewalCount()
        {
            string val = _settingRepo.GetSettingValue("MAX_RENEWAL_COUNT", "1");
            return int.TryParse(val, out int res) ? res : 1;
        }

        public int GetRenewalDays()
        {
            string val = _settingRepo.GetSettingValue("RENEWAL_DAYS", "7");
            return int.TryParse(val, out int res) ? res : 7;
        }

        public string GetSmtpHost()
        {
            return _settingRepo.GetSettingValue("SMTP_HOST", "smtp.gmail.com");
        }

        public int GetSmtpPort()
        {
            string val = _settingRepo.GetSettingValue("SMTP_PORT", "587");
            return int.TryParse(val, out int res) ? res : 587;
        }

        public bool GetSmtpEnableSsl()
        {
            string val = _settingRepo.GetSettingValue("SMTP_ENABLE_SSL", "true");
            return bool.TryParse(val, out bool res) ? res : true;
        }

        public string GetSmtpUsername()
        {
            return _settingRepo.GetSettingValue("SMTP_USERNAME", "");
        }

        public string GetSmtpPassword()
        {
            return _settingRepo.GetSettingValue("SMTP_PASSWORD", "");
        }

        public string GetSmtpFromName()
        {
            return _settingRepo.GetSettingValue("SMTP_FROM_NAME", "LibOps - Hệ Thống Quản Trị Thư Viện Hiện Đại");
        }

        public bool GetEmailNotificationEnabled()
        {
            string val = _settingRepo.GetSettingValue("SYSTEM_EMAIL_NOTIFICATION_ENABLED", "false");
            return bool.TryParse(val, out bool res) ? res : false;
        }

        public bool SaveSmtpSettings(
            string host,
            int port,
            bool enableSsl,
            string username,
            string password,
            string fromName,
            bool enableEmail,
            out string errorMessage)
        {
            errorMessage = string.Empty;

            if (enableEmail && string.IsNullOrWhiteSpace(host))
            {
                errorMessage = "Vui lòng nhập địa chỉ máy chủ SMTP (ví dụ: smtp.gmail.com).";
                return false;
            }

            if (port <= 0 || port > 65535)
            {
                errorMessage = "Cổng SMTP không hợp lệ (thường là 587 hoặc 465 hoặc 25).";
                return false;
            }

            _settingRepo.UpdateSettingValue("SMTP_HOST", host?.Trim() ?? "smtp.gmail.com");
            _settingRepo.UpdateSettingValue("SMTP_PORT", port.ToString());
            _settingRepo.UpdateSettingValue("SMTP_ENABLE_SSL", enableSsl ? "true" : "false");
            _settingRepo.UpdateSettingValue("SMTP_USERNAME", username?.Trim() ?? string.Empty);
            _settingRepo.UpdateSettingValue("SMTP_PASSWORD", password ?? string.Empty);
            _settingRepo.UpdateSettingValue("SMTP_FROM_NAME", string.IsNullOrWhiteSpace(fromName) ? "LibOps Thư Viện" : fromName.Trim());
            _settingRepo.UpdateSettingValue("SYSTEM_EMAIL_NOTIFICATION_ENABLED", enableEmail ? "true" : "false");

            return true;
        }

        public bool SaveSystemSettings(
            decimal defaultDeposit,
            int validityDays,
            int maxBorrowDays,
            int maxBooksPerMember,
            decimal finePerDay,
            decimal lostRatePercent,
            decimal damagedRatePercent,
            int maxRenewalCount,
            int renewalDays,
            decimal cardIssuanceFee,
            out string errorMessage)
        {
            errorMessage = string.Empty;

            if (defaultDeposit < 0)
            {
                errorMessage = "Tiền cọc mặc định không được âm.";
                return false;
            }

            if (cardIssuanceFee < 0)
            {
                errorMessage = "Phí phát hành thẻ không được âm.";
                return false;
            }

            if (validityDays <= 0 || maxBorrowDays <= 0 || maxBooksPerMember <= 0)
            {
                errorMessage = "Thời hạn hiệu lực, hạn mượn và số lượng sách mượn tối đa phải lớn hơn 0.";
                return false;
            }

            if (maxRenewalCount < 0 || renewalDays <= 0)
            {
                errorMessage = "Số lần gia hạn không được âm và số ngày gia hạn phải lớn hơn 0.";
                return false;
            }

            if (finePerDay < 0 || lostRatePercent < 0 || damagedRatePercent < 0)
            {
                errorMessage = "Đơn giá phạt và tỉ lệ bồi thường không được âm.";
                return false;
            }

            decimal lostRate = lostRatePercent / 100.0m;
            decimal damagedRate = damagedRatePercent / 100.0m;

            _settingRepo.UpdateSettingValue("CARD_ISSUANCE_FEE", cardIssuanceFee.ToString(CultureInfo.InvariantCulture));
            _settingRepo.UpdateSettingValue("MEMBER_CARD_DEFAULT_DEPOSIT", defaultDeposit.ToString(CultureInfo.InvariantCulture));
            _settingRepo.UpdateSettingValue("MEMBER_CARD_VALIDITY_DAYS", validityDays.ToString());
            _settingRepo.UpdateSettingValue("MAX_BORROW_DAYS", maxBorrowDays.ToString());
            _settingRepo.UpdateSettingValue("MAX_BOOKS_PER_MEMBER", maxBooksPerMember.ToString());
            _settingRepo.UpdateSettingValue("FINE_PER_OVERDUE_DAY", finePerDay.ToString(CultureInfo.InvariantCulture));
            _settingRepo.UpdateSettingValue("LOST_BOOK_FINE_RATE", lostRate.ToString("0.00", CultureInfo.InvariantCulture));
            _settingRepo.UpdateSettingValue("DAMAGED_BOOK_FINE_RATE", damagedRate.ToString("0.00", CultureInfo.InvariantCulture));
            _settingRepo.UpdateSettingValue("MAX_RENEWAL_COUNT", maxRenewalCount.ToString());
            _settingRepo.UpdateSettingValue("RENEWAL_DAYS", renewalDays.ToString());

            return true;
        }

        public bool SaveMembershipSettings(decimal defaultDeposit, int validityDays, decimal cardIssuanceFee, out string errorMessage)
        {
            errorMessage = string.Empty;
            if (defaultDeposit < 0)
            {
                errorMessage = "Tiền ký quỹ mở thẻ không được âm.";
                return false;
            }
            if (cardIssuanceFee < 0)
            {
                errorMessage = "Phí phát hành thẻ không được âm.";
                return false;
            }
            if (validityDays <= 0)
            {
                errorMessage = "Thời hạn sử dụng thẻ phải lớn hơn 0 ngày.";
                return false;
            }

            _settingRepo.UpdateSettingValue("MEMBER_CARD_DEFAULT_DEPOSIT", defaultDeposit.ToString(CultureInfo.InvariantCulture));
            _settingRepo.UpdateSettingValue("CARD_ISSUANCE_FEE", cardIssuanceFee.ToString(CultureInfo.InvariantCulture));
            _settingRepo.UpdateSettingValue("MEMBER_CARD_VALIDITY_DAYS", validityDays.ToString());
            return true;
        }

        public bool SaveBorrowRules(int maxBorrowDays, int maxBooksPerMember, int maxRenewalCount, int renewalDays, out string errorMessage)
        {
            errorMessage = string.Empty;
            if (maxBorrowDays <= 0)
            {
                errorMessage = "Số ngày mượn sách tối đa phải lớn hơn 0.";
                return false;
            }
            if (maxBooksPerMember <= 0)
            {
                errorMessage = "Hạn mức số sách mượn cùng lúc phải lớn hơn 0.";
                return false;
            }
            if (maxRenewalCount < 0)
            {
                errorMessage = "Số lần gia hạn sách không được âm.";
                return false;
            }
            if (renewalDays <= 0)
            {
                errorMessage = "Số ngày cộng thêm mỗi lần gia hạn phải lớn hơn 0.";
                return false;
            }

            _settingRepo.UpdateSettingValue("MAX_BORROW_DAYS", maxBorrowDays.ToString());
            _settingRepo.UpdateSettingValue("MAX_BOOKS_PER_MEMBER", maxBooksPerMember.ToString());
            _settingRepo.UpdateSettingValue("MAX_RENEWAL_COUNT", maxRenewalCount.ToString());
            _settingRepo.UpdateSettingValue("RENEWAL_DAYS", renewalDays.ToString());
            return true;
        }

        public bool SavePenaltyRules(decimal finePerDay, decimal lostRatePercent, decimal damagedRatePercent, out string errorMessage)
        {
            errorMessage = string.Empty;
            if (finePerDay < 0)
            {
                errorMessage = "Đơn giá phạt quá hạn không được âm.";
                return false;
            }
            if (lostRatePercent < 0)
            {
                errorMessage = "Tỷ lệ bồi thường làm mất sách không được âm.";
                return false;
            }
            if (damagedRatePercent < 0)
            {
                errorMessage = "Tỷ lệ bồi thường làm hư hại sách không được âm.";
                return false;
            }

            decimal lostRate = lostRatePercent / 100.0m;
            decimal damagedRate = damagedRatePercent / 100.0m;

            _settingRepo.UpdateSettingValue("FINE_PER_OVERDUE_DAY", finePerDay.ToString(CultureInfo.InvariantCulture));
            _settingRepo.UpdateSettingValue("LOST_BOOK_FINE_RATE", lostRate.ToString("0.00", CultureInfo.InvariantCulture));
            _settingRepo.UpdateSettingValue("DAMAGED_BOOK_FINE_RATE", damagedRate.ToString("0.00", CultureInfo.InvariantCulture));
            return true;
        }

        public bool SaveSystemSettings(
            decimal defaultDeposit,
            int validityDays,
            int maxBorrowDays,
            int maxBooksPerMember,
            decimal finePerDay,
            decimal lostRatePercent,
            decimal damagedRatePercent,
            int maxRenewalCount,
            int renewalDays,
            out string errorMessage)
        {
            return SaveSystemSettings(
                defaultDeposit,
                validityDays,
                maxBorrowDays,
                maxBooksPerMember,
                finePerDay,
                lostRatePercent,
                damagedRatePercent,
                maxRenewalCount,
                renewalDays,
                GetCardIssuanceFee(),
                out errorMessage
            );
        }

        public bool SaveSystemSettings(
            decimal defaultDeposit,
            int validityDays,
            int maxBorrowDays,
            int maxBooksPerMember,
            decimal finePerDay,
            decimal lostRatePercent,
            decimal damagedRatePercent,
            out string errorMessage)
        {
            return SaveSystemSettings(
                defaultDeposit,
                validityDays,
                maxBorrowDays,
                maxBooksPerMember,
                finePerDay,
                lostRatePercent,
                damagedRatePercent,
                GetMaxRenewalCount(),
                GetRenewalDays(),
                GetCardIssuanceFee(),
                out errorMessage
            );
        }

        public void RestoreDefaults()
        {
            _settingRepo.UpdateSettingValue("CARD_ISSUANCE_FEE", "50000");
            _settingRepo.UpdateSettingValue("MEMBER_CARD_DEFAULT_DEPOSIT", "200000");
            _settingRepo.UpdateSettingValue("MEMBER_CARD_VALIDITY_DAYS", "365");
            _settingRepo.UpdateSettingValue("MAX_BORROW_DAYS", "14");
            _settingRepo.UpdateSettingValue("MAX_BOOKS_PER_MEMBER", "5");
            _settingRepo.UpdateSettingValue("FINE_PER_OVERDUE_DAY", "5000");
            _settingRepo.UpdateSettingValue("LOST_BOOK_FINE_RATE", "2.0");
            _settingRepo.UpdateSettingValue("DAMAGED_BOOK_FINE_RATE", "0.5");
            _settingRepo.UpdateSettingValue("MAX_RENEWAL_COUNT", "1");
            _settingRepo.UpdateSettingValue("RENEWAL_DAYS", "7");
            _settingRepo.UpdateSettingValue("SMTP_HOST", "smtp.gmail.com");
            _settingRepo.UpdateSettingValue("SMTP_PORT", "587");
            _settingRepo.UpdateSettingValue("SMTP_ENABLE_SSL", "true");
            _settingRepo.UpdateSettingValue("SMTP_USERNAME", "");
            _settingRepo.UpdateSettingValue("SMTP_PASSWORD", "");
            _settingRepo.UpdateSettingValue("SMTP_FROM_NAME", "LibOps - Hệ Thống Quản Trị Thư Viện Hiện Đại");
            _settingRepo.UpdateSettingValue("SYSTEM_EMAIL_NOTIFICATION_ENABLED", "false");
        }
    }
}
