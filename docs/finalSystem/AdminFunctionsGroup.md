# CÁC CHỨC NĂNG DÀNH RIÊNG CHO QUẢN TRỊ VIÊN (ADMIN)

---

## 1. DANH SÁCH CÁC CHỨC NĂNG (DANH MỤC)

Trong hệ thống phân quyền của LibOps, có **2 chức năng đặc quyền tuyệt đối chỉ riêng Quản Trị Viên (ADMIN)** mới có quyền truy cập và thao tác:

1. **Quản Lý Tài Khoản Nhân Viên & Phân Quyền Hệ Thống** (Staff Accounts & Role Management)
2. **Cấu Hình Tham Số Nghiệp Vụ & Kết Nối Tích Hợp Hệ Thống** (System Settings & Integration)

---

## 2. CHI TIẾT TỪNG CHỨC NĂNG ĐẶC QUYỀN CỦA ADMIN

---

### 2.1. Quản Lý Tài Khoản Nhân Viên & Phân Quyền Hệ Thống

#### A. Mô tả chức năng:
- Cho phép Quản trị viên quản lý toàn bộ danh sách tài khoản của nhân viên/thủ thư trong hệ thống thư viện.
- **Admin dùng chức năng này vào trường hợp nào? Khi nào?**
  - **Khi tuyển dụng nhân sự/thủ thư mới:** Admin tạo tài khoản đăng nhập mới, gán vai trò (`LIBRARIAN` hoặc `ADMIN`), cấp mật khẩu khởi tạo ban đầu và kích hoạt tài khoản để nhân viên có thể đăng nhập vào phần mềm.
  - **Khi nhân viên cập nhật thông tin:** Admin cập nhật lại họ tên, số điện thoại, email làm việc hoặc thay đổi phân quyền vai trò công tác của nhân viên.
  - **Khi nhân viên quên mật khẩu:** Admin sử dụng tính năng cấp lại/đặt lại mật khẩu mới cho nhân viên một cách an toàn.
  - **Khi nhân viên nghỉ việc hoặc tạm dừng công tác:** Admin thực hiện khóa tài khoản tức thì mà không làm mất dữ liệu lịch sử mượn trả do nhân viên đó từng lập. Khi nhân viên quay lại làm việc, Admin có thể mở khóa lại tài khoản bất kỳ lúc nào.
  - **Khi tra cứu danh sách nhân sự:** Tìm kiếm nhanh nhân viên theo tên đăng nhập, họ tên hoặc số điện thoại.

#### B. Các trang giao diện:
- **Trang chính:** 
  - `StaffManagementView.xaml` (Giao diện Danh sách Tài khoản Nhân viên).
  - Nằm tại đường dẫn: `LibOps/PresentationLayer/Views/Admin/StaffAccounts/StaffManagementView.xaml`.
  - Hiển thị bảng danh sách nhân viên, vai trò, trạng thái kích hoạt, thanh tìm kiếm, phân trang và thanh công cụ thao tác (Thêm mới, Chỉnh sửa, Khóa/Mở khóa tài khoản, Đặt lại mật khẩu).
- **Trang phụ (Hộp thoại / Dialog con):**
  - `StaffDetailDialog.xaml` (Hộp thoại Thêm mới / Cập nhật thông tin nhân viên).
  - Nằm tại đường dẫn: `LibOps/PresentationLayer/Views/Admin/StaffAccounts/StaffDetailDialog.xaml`.
  - Nhập liệu các thông tin: Tên đăng nhập, Mật khẩu ban đầu (khi tạo mới), Họ tên, Số điện thoại, Email, Vai trò (`ADMIN` / `LIBRARIAN`), Trạng thái hoạt động.

#### C. Các file mã nguồn xử lý (Code Files & Layers):
- **Tầng Giao Diện (Presentation Layer - Views & Code-behind):**
  - `PresentationLayer/Views/Admin/StaffAccounts/StaffManagementView.xaml`: Giao diện XAML danh sách nhân viên.
  - `PresentationLayer/Views/Admin/StaffAccounts/StaffManagementView.xaml.cs`: Code-behind gắn DataContext `StaffManagementViewModel`.
  - `PresentationLayer/Views/Admin/StaffAccounts/StaffDetailDialog.xaml`: Giao diện XAML form thêm/sửa nhân viên.
  - `PresentationLayer/Views/Admin/StaffAccounts/StaffDetailDialog.xaml.cs`: Code-behind điều khiển đóng/mở dialog.
- **Tầng ViewModel (Presentation Layer - ViewModels & Services):**
  - `PresentationLayer/ViewModels/Admin/StaffAccounts/StaffManagementViewModel.cs`: ViewModel quản lý danh sách nhân viên, tìm kiếm, phân trang, lệnh khóa/mở khóa tài khoản, lệnh đặt lại mật khẩu.
  - `PresentationLayer/ViewModels/Admin/StaffAccounts/StaffDetailViewModel.cs`: ViewModel xử lý logic form nhập liệu thêm mới / chỉnh sửa thông tin nhân viên.
  - `PresentationLayer/Services/WpfDialogService.cs` & `IDialogService.cs`: Service mở hộp thoại `StaffDetailDialog` từ ViewModel theo chuẩn MVVM.
- **Tầng Nghiệp Vụ (Business Logic Layer):**
  - `BusinessLogicLayer/BusinessServices/AuthService.cs`: Xử lý logic nghiệp vụ tạo tài khoản mới, xác thực thông tin, đặt lại mật khẩu, kích hoạt/khóa tài khoản nhân viên.
- **Tầng Truy Cập Dữ Liệu (Data Access Layer):**
  - `DataAccessLayer/DataRepositories/UserRepository.cs`: Thực thi các câu lệnh SQL truy vấn danh sách nhân viên, thêm mới nhân viên, cập nhật thông tin và cập nhật trạng thái `IsActive` trong bảng `UserAccounts`.
- **Tầng Mô Hình Dữ Liệu & Tiện Ích (Data Models & Common Utilities):**
  - `DataModels/Entities/UserAccountEntity.cs`: Thực thể ánh xạ bảng tài khoản người dùng `UserAccounts`.
  - `DataModels/Entities/RoleEntity.cs`: Thực thể ánh xạ bảng vai trò `Roles`.
  - `DataModels/DataTransferObjects/UserSessionDto.cs`: DTO lưu thông tin phiên đăng nhập của người dùng.
  - `CommonUtilities/Security/PasswordHashingUtility.cs`: Tiện ích tạo chuỗi Salt ngẫu nhiên và băm mật khẩu bảo mật chuẩn SHA-256.
  - `CommonUtilities/Validation/InputValidationUtility.cs`: Tiện ích kiểm tra định dạng dữ liệu đầu vào chuẩn hóa (Tên đăng nhập, Mật khẩu, Số điện thoại, Email).

---

### 2.2. Cấu Hình Tham Số Nghiệp Vụ & Kết Nối Tích Hợp Hệ Thống

#### A. Mô tả chức năng:
- Bảng điều khiển trung tâm cho phép Quản trị viên tùy biến toàn bộ các quy tắc nghiệp vụ vận hành thư viện và cấu hình các cổng tích hợp bên thứ ba mà không cần chỉnh sửa mã nguồn phần mềm.
- **Admin dùng chức năng này vào trường hợp nào? Khi nào?**
  - **Khi thay đổi chính sách phát hành thẻ độc giả:**
    - Điều chỉnh mức phí dịch vụ mở thẻ mới (`CARD_ISSUANCE_FEE`).
    - Thay đổi mức tiền cọc thế chân tối thiểu bắt buộc (`MEMBER_CARD_DEFAULT_DEPOSIT`).
    - Thay đổi mức phí thường niên khi gia hạn thẻ (`MEMBER_CARD_ANNUAL_FEE`).
    - Điều chỉnh thời hạn hiệu lực của thẻ độc giả (`MEMBER_CARD_VALIDITY_DAYS`).
  - **Khi thay đổi quy chế mượn - trả sách:**
    - Cài đặt số lượng sách tối đa một độc giả được phép mượn đồng thời (`MAX_BOOKS_PER_MEMBER`).
    - Cài đặt thời hạn mượn sách tối đa cho mỗi lượt (`MAX_BORROW_DAYS`).
    - Giới hạn số lần được phép gia hạn phiếu mượn (`MAX_RENEWAL_COUNT`) và số ngày gia hạn thêm (`RENEWAL_DAYS`).
  - **Khi điều chỉnh chế tài xử lý vi phạm & nợ phạt:**
    - Thay đổi mức phạt trả sách trễ hạn theo ngày (`FINE_PER_OVERDUE_DAY`).
    - Điều chỉnh tỷ lệ phần trăm tiền đền bù khi làm hư hỏng sách (`DAMAGED_BOOK_FINE_RATE`).
    - Điều chỉnh tỷ lệ phần trăm tiền đền bù khi làm mất sách (`LOST_BOOK_FINE_RATE`).
  - **Khi thiết lập hạ tầng gửi Email thông báo tự động:**
    - Cấu hình địa chỉ máy chủ SMTP (`SMTP_HOST`), số cổng kết nối (`SMTP_PORT`), tùy chọn bảo mật (`SMTP_ENABLE_SSL`).
    - Nhập tài khoản email hệ thống (`SMTP_USERNAME`), mật khẩu ứng dụng (`SMTP_PASSWORD`) và tên hiển thị người gửi (`SMTP_FROM_NAME`).
    - Bấm nút "Gửi Thử Email Kiểm Tra" trực tiếp ngay trên giao diện để kiểm thử kết nối gửi email.
    - Bật hoặc tắt toàn bộ tính năng gửi email tự động toàn hệ thống (`SYSTEM_EMAIL_NOTIFICATION_ENABLED`).
  - **Khi cập nhật tài khoản nhận tiền thụ hưởng:** Cấu hình tài khoản ngân hàng và các khóa kết nối API cổng thanh toán VietQR PayOS Auto-Banking.

#### B. Các trang giao diện:
- **Trang chính:**
  - `SystemSettingsView.xaml` (Giao diện Cấu hình Hệ thống).
  - Nằm tại đường dẫn: `LibOps/PresentationLayer/Views/Admin/SystemSettings/SystemSettingsView.xaml`.
  - Được chia thành 4 phân khu thẻ card trực quan:
    1. *Nhóm Cấu hình Thẻ Độc giả & Tiền cọc* (Chế độ Xem / Chỉnh sửa trực tiếp).
    2. *Nhóm Quy tắc Mượn Trả Sách* (Chế độ Xem / Chỉnh sửa trực tiếp).
    3. *Nhóm Chế tài Xử phạt & Bồi thường* (Chế độ Xem / Chỉnh sửa trực tiếp).
    4. *Nhóm Cấu hình Máy chủ Email SMTP* (Hỗ trợ nút bấm gửi thử email kiểm tra và lưu cấu hình động).
- **Trang phụ:** Không có (Toàn bộ thao tác được tích hợp tối ưu trên cùng một giao diện duy nhất).

#### C. Các file mã nguồn xử lý (Code Files & Layers):
- **Tầng Giao Diện (Presentation Layer - Views & Code-behind):**
  - `PresentationLayer/Views/Admin/SystemSettings/SystemSettingsView.xaml`: Giao diện XAML các nhóm thẻ card cấu hình.
  - `PresentationLayer/Views/Admin/SystemSettings/SystemSettingsView.xaml.cs`: Code-behind gắn DataContext `SystemSettingsViewModel`.
- **Tầng ViewModel (Presentation Layer - ViewModels):**
  - `PresentationLayer/ViewModels/Admin/SystemSettings/SystemSettingsViewModel.cs`: ViewModel quản lý trạng thái hiển thị/chỉnh sửa từng nhóm cấu hình, ràng buộc dữ liệu (Binding), lệnh lưu cấu hình và lệnh gửi email thử nghiệm.
- **Tầng Nghiệp Vụ (Business Logic Layer):**
  - `BusinessLogicLayer/BusinessServices/SystemSettingsService.cs`: Service đọc, ghi và cache các giá trị cấu hình tham số từ CSDL và file `App.config`, kiểm tra tính hợp lệ của các giá trị cấu hình.
  - `BusinessLogicLayer/BusinessServices/Notification/EmailNotificationService.cs` & `IEmailNotificationService.cs`: Dịch vụ khởi tạo kết nối SMTP và gửi email kiểm tra trực tiếp.
  - `BusinessLogicLayer/BusinessServices/Notification/HtmlEmailTemplateBuilder.cs`: Tiện ích tạo nội dung email HTML mẫu gửi kiểm thử hệ thống.
- **Tầng Truy Cập Dữ Liệu (Data Access Layer):**
  - `DataAccessLayer/DataRepositories/SystemSettingRepository.cs`: Data Repository đọc toàn bộ bảng `SystemSettings` hoặc chèn/cập nhật từng bản ghi `SettingKey` - `SettingValue`.
- **Tầng Mô Hình Dữ Liệu & Hằng Số (Data Models & Constants):**
  - `DataModels/Entities/SystemSettingEntity.cs`: Thực thể ánh xạ bảng tham số hệ thống `SystemSettings`.
  - `CommonUtilities/Constants/SystemSettingKeys.cs`: Tập hợp các hằng số tên khóa cấu hình trong toàn bộ hệ thống (VD: `SystemSettingKeys.CardIssuanceFee`, `SystemSettingKeys.SmtpHost`...).
