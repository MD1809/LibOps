using System;
using LibOps.CommonUtilities.Security;
using LibOps.DataAccessLayer.DataRepositories;
using LibOps.DataModels.DataTransferObjects;
using LibOps.DataModels.Entities;

namespace LibOps.BusinessLogicLayer.BusinessServices
{
    /// <summary>
    /// Dịch vụ xử lý nghiệp vụ xác thực đăng nhập, phân quyền và đổi mật khẩu người dùng
    /// </summary>
    public class AuthService
    {
        private readonly UserRepository _userRepository;

        /// <summary>
        /// Phiên đăng nhập người dùng hiện hành trong toàn hệ thống
        /// </summary>
        public static UserSessionDto CurrentSession { get; set; }

        public AuthService()
        {
            _userRepository = new UserRepository();
        }

        public AuthService(UserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        /// <summary>
        /// Xác thực thông tin đăng nhập của người dùng
        /// </summary>
        public UserSessionDto AuthenticateUser(string username, string rawPassword, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(rawPassword))
            {
                errorMessage = "Vui lòng nhập đầy đủ các trường bắt buộc.";
                return null;
            }

            UserAccountEntity user = _userRepository.GetUserByUsername(username.Trim());
            if (user == null || !string.Equals(user.Username, username.Trim(), StringComparison.Ordinal))
            {
                errorMessage = "Thông tin tài khoản hoặc mật khẩu không chính xác.";
                return null;
            }

            if (!user.IsActive)
            {
                errorMessage = "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ Quản trị viên.";
                return null;
            }

            bool isPasswordCorrect = PasswordHashUtility.VerifyPassword(rawPassword, user.PasswordHash, user.PasswordSalt);
            if (!isPasswordCorrect)
            {
                errorMessage = "Thông tin tài khoản hoặc mật khẩu không chính xác.";
                return null;
            }

            // Nếu là tài khoản Độc giả (READER), kiểm tra thẻ thư viện tương ứng
            int? memberId = user.MemberId;
            string memberCardCode = user.MemberCardCode;

            if (string.Equals(user.RoleName, "READER", StringComparison.OrdinalIgnoreCase) && memberId.HasValue)
            {
                var memberRepo = new MemberRepository();
                var member = memberRepo.GetMemberById(memberId.Value);
                if (member != null)
                {
                    memberCardCode = member.MemberCardCode;
                    if (member.CardStatus == "CLOSED")
                    {
                        errorMessage = "Thẻ thư viện của bạn đã bị HỦY. Vui lòng liên hệ quầy thư viện để đăng ký mở thẻ mới!";
                        return null;
                    }
                }
            }

            var session = new UserSessionDto
            {
                UserId = user.UserId,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                RoleId = user.RoleId,
                RoleName = user.RoleName,
                MemberId = memberId,
                MemberCardCode = memberCardCode,
                LoginTime = DateTime.Now
            };

            CurrentSession = session;
            return session;
        }

        /// <summary>
        /// Thay đổi mật khẩu người dùng cá nhân
        /// </summary>
        public bool ChangePassword(int userId, string currentPassword, string newPassword, string confirmPassword, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(currentPassword))
            {
                errorMessage = "Vui lòng nhập mật khẩu hiện tại.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(newPassword))
            {
                errorMessage = "Vui lòng nhập mật khẩu mới.";
                return false;
            }

            if (newPassword.Length < 6)
            {
                errorMessage = "Mật khẩu mới phải có độ dài tối thiểu từ 6 ký tự trở lên.";
                return false;
            }

            if (!string.Equals(newPassword, confirmPassword))
            {
                errorMessage = "Mật khẩu xác nhận không khớp với mật khẩu mới.";
                return false;
            }

            UserAccountEntity user = _userRepository.GetUserById(userId);
            if (user == null)
            {
                errorMessage = "Không tìm thấy thông tin tài khoản người dùng.";
                return false;
            }

            bool isCurrentPasswordValid = PasswordHashUtility.VerifyPassword(currentPassword, user.PasswordHash, user.PasswordSalt);
            if (!isCurrentPasswordValid)
            {
                errorMessage = "Mật khẩu hiện tại không chính xác.";
                return false;
            }

            string newSalt = PasswordHashUtility.GenerateSalt();
            string newHash = PasswordHashUtility.ComputeSha256Hash(newPassword, newSalt);

            bool isUpdated = _userRepository.UpdatePassword(userId, newHash, newSalt);
            if (!isUpdated)
            {
                errorMessage = "Không thể cập nhật mật khẩu vào CSDL. Vui lòng thử lại sau.";
                return false;
            }

            return true;
        }

        public System.Collections.Generic.List<UserAccountEntity> GetAllUsers()
        {
            return _userRepository.GetAllUsers();
        }

        public System.Collections.Generic.List<UserAccountEntity> GetStaffUsers()
        {
            return _userRepository.GetStaffUsers();
        }

        public System.Collections.Generic.List<RoleEntity> GetAllRoles()
        {
            return _userRepository.GetAllRoles();
        }

        public bool CreateUserAccount(UserAccountEntity entity, string rawPassword, out string errorMessage)
        {
            errorMessage = string.Empty;

            try
            {
                if (entity == null || string.IsNullOrWhiteSpace(entity.Username) || string.IsNullOrWhiteSpace(entity.FullName))
                {
                    errorMessage = "Vui lòng nhập đầy đủ tên đăng nhập và họ tên nhân viên.";
                    return false;
                }

                if (!LibOps.CommonUtilities.Validation.InputValidationUtility.IsValidUsername(entity.Username))
                {
                    errorMessage = "Tên đăng nhập không hợp lệ (Chỉ gồm 3-50 ký tự chữ, số và dấu gạch dưới).";
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(entity.Email) && !LibOps.CommonUtilities.Validation.InputValidationUtility.IsValidEmail(entity.Email))
                {
                    errorMessage = "Địa chỉ Email không đúng định dạng.";
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(entity.PhoneNumber) && !LibOps.CommonUtilities.Validation.InputValidationUtility.IsValidPhoneNumber(entity.PhoneNumber))
                {
                    errorMessage = "Số điện thoại không đúng định dạng (Yêu cầu 10 chữ số).";
                    return false;
                }

                if (_userRepository.IsUsernameExists(entity.Username))
                {
                    errorMessage = $"Tên đăng nhập '{entity.Username}' đã tồn tại trong hệ thống.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(rawPassword) || rawPassword.Length < 6)
                {
                    errorMessage = "Mật khẩu khởi tạo phải có độ dài từ 6 ký tự trở lên.";
                    return false;
                }

                string salt = PasswordHashUtility.GenerateSalt();
                string hash = PasswordHashUtility.ComputeSha256Hash(rawPassword, salt);

                entity.PasswordSalt = salt;
                entity.PasswordHash = hash;
                entity.IsActive = true;
                entity.PhoneNumber = LibOps.CommonUtilities.Validation.InputValidationUtility.NormalizePhoneNumber(entity.PhoneNumber);

                int newId = _userRepository.InsertUser(entity);
                if (newId <= 0)
                {
                    errorMessage = "Lỗi khi lưu tài khoản vào CSDL.";
                    return false;
                }

                entity.UserId = newId;
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = "Lỗi ngoại lệ khi tạo tài khoản: " + ex.Message;
                return false;
            }
        }

        public bool UpdateUserAccount(UserAccountEntity entity, out string errorMessage)
        {
            errorMessage = string.Empty;

            try
            {
                if (entity == null || entity.UserId <= 0 || string.IsNullOrWhiteSpace(entity.FullName))
                {
                    errorMessage = "Thông tin tài khoản không hợp lệ.";
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(entity.Email) && !LibOps.CommonUtilities.Validation.InputValidationUtility.IsValidEmail(entity.Email))
                {
                    errorMessage = "Địa chỉ Email không đúng định dạng.";
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(entity.PhoneNumber) && !LibOps.CommonUtilities.Validation.InputValidationUtility.IsValidPhoneNumber(entity.PhoneNumber))
                {
                    errorMessage = "Số điện thoại không đúng định dạng (Yêu cầu 10 chữ số).";
                    return false;
                }

                var existingUser = _userRepository.GetUserById(entity.UserId);
                if (existingUser != null)
                {
                    // Tài khoản admin hệ thống không bao giờ bị vô hiệu hóa
                    if (string.Equals(existingUser.Username, "admin", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(existingUser.RoleName, "ADMIN", StringComparison.OrdinalIgnoreCase))
                    {
                        entity.IsActive = true;
                    }
                }

                entity.PhoneNumber = LibOps.CommonUtilities.Validation.InputValidationUtility.NormalizePhoneNumber(entity.PhoneNumber);

                bool ok = _userRepository.UpdateUser(entity);
                if (!ok)
                {
                    errorMessage = "Lỗi cập nhật thông tin tài khoản.";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = "Lỗi ngoại lệ khi cập nhật tài khoản: " + ex.Message;
                return false;
            }
        }

        public bool ToggleUserStatus(int userId, bool isActive, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (userId <= 0)
            {
                errorMessage = "Mã tài khoản không hợp lệ.";
                return false;
            }

            var targetUser = _userRepository.GetUserById(userId);
            if (targetUser == null)
            {
                errorMessage = "Không tìm thấy thông tin tài khoản người dùng.";
                return false;
            }

            // Chặn vô hiệu hóa tài khoản ADMIN
            if (!isActive && (string.Equals(targetUser.Username, "admin", StringComparison.OrdinalIgnoreCase) ||
                              string.Equals(targetUser.RoleName, "ADMIN", StringComparison.OrdinalIgnoreCase)))
            {
                errorMessage = "Tài khoản Quản trị viên (ADMIN) là tài khoản tối cao của hệ thống, không thể vô hiệu hóa hoặc ngừng hoạt động.";
                return false;
            }

            // Chặn tự khóa chính mình
            if (CurrentSession != null && CurrentSession.UserId == userId && !isActive)
            {
                errorMessage = "Bạn không thể tự khóa tài khoản đang đăng nhập hiện tại.";
                return false;
            }

            bool ok = _userRepository.UpdateUserStatus(userId, isActive);
            if (!ok)
            {
                errorMessage = "Lỗi cập nhật trạng thái tài khoản.";
                return false;
            }

            return true;
        }

        public bool ResetDefaultPassword(int userId, out string defaultPassword, out string errorMessage)
        {
            defaultPassword = "LibOps@" + DateTime.Today.Year;
            errorMessage = string.Empty;

            if (userId <= 0)
            {
                errorMessage = "Mã tài khoản không hợp lệ.";
                return false;
            }

            string salt = PasswordHashUtility.GenerateSalt();
            string hash = PasswordHashUtility.ComputeSha256Hash(defaultPassword, salt);

            bool ok = _userRepository.UpdatePassword(userId, hash, salt);
            if (!ok)
            {
                errorMessage = "Lỗi thiết lập lại mật khẩu trong CSDL.";
                return false;
            }

            return true;
        }
    }
}
