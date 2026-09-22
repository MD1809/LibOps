# CÁC CHỨC NĂNG DÀNH CHO ĐỘC GIẢ (READER)

---

## 1. DANH SÁCH CÁC CHỨC NĂNG TRÊN SIDEBAR (DANH MỤC)

Cổng thông tin tự phục vụ trực tuyến của Độc giả (OPAC Portal) trong LibOps cung cấp 6 phân hệ chức năng tác nghiệp và tra cứu:

1. **Trang Chủ Khám Phá Tri Thức** (Reader Home & Discovery Hub)
2. **Tra Cứu Sách Trực Tuyến (OPAC Catalog)** (Online Book Search & Details)
3. **Mượn Và Trả Sách** (Loans & Return History)
4. **Thẻ Thư Viện & Quản Lý Tài Chính** (Library Card & VietQR Auto-Banking)
5. **Yêu Cầu Tự Phục Vụ Trực Tuyến** (Self-Service Online Requests)
6. **Thông Tin Cá Nhân & Đổi Mật Khẩu** (Reader Profile & Security)

---

## 2. CHI TIẾT TỪNG CHỨC NĂNG CỦA ĐỘC GIẢ

---

### 2.1. Trang Chủ Khám Phá Tri Thức (Reader Home & Discovery Hub)

#### A. Mô tả chức năng:
- Màn hình khởi đầu ấn tượng giúp độc giả nhanh chóng khám phá kho tàng sách, các tựa sách mới nhất và sách đang thịnh hành.
- **Độc giả dùng chức năng này vào trường hợp nào? Khi nào?**
  - **Khi vừa đăng nhập:** Độc giả xem các banner nổi bật, tìm kiếm nhanh sách ngay trên thanh công cụ tìm kiếm Hero Banner.
  - **Khi tìm kiếm gợi ý đọc:** Xem danh sách "Top Sách Mượn Nhiều Nhất" (Trending Books) và "Sách Mới Cập Nhật" (New Arrivals).
  - **Khi duyệt sách theo chủ đề:** Bấm vào các thẻ Thể loại nổi bật (Công nghệ thông tin, Kinh tế, Văn học...) để tự động chuyển sang trang tìm kiếm lọc theo thể loại đó.
  - **Khi xem chi tiết sách:** Bấm vào thẻ bìa sách bất kỳ để mở trang thông tin chi tiết của tác phẩm.

#### B. Các trang giao diện:
- **Trang chính:**
  - `ReaderHomeView.xaml` (Giao diện Trang Chủ Khám Phá).
  - Nằm tại: `LibOps/PresentationLayer/Views/Reader/Home/ReaderHomeView.xaml`.
- **Trang phụ / Chuyển hướng:**
  - `ReaderBookDetailView.xaml` (Xem chi tiết sách khi bấm vào bìa sách).

#### C. Các file mã nguồn xử lý (Code Files & Layers):
- **Views:** `ReaderHomeView.xaml` & `.cs`
- **ViewModels:** `ReaderHomeViewModel.cs`
- **Business Services:** `ReaderPortalService.cs` (hàm `GetTrendingBooks`, `GetNewArrivals`, `GetCategoryStats`)
- **Data Repositories:** `BookRepository.cs`, `CategoryRepository.cs`
- **DTOs:** `ReaderBookSearchDto`, `CategoryCountDto` (trong `ReaderPortalDtos.cs`)

---

### 2.2. Tra Cứu Sách Trực Tuyến (OPAC Catalog & Book Details)

#### A. Mô tả chức năng:
- Cung cấp công cụ tìm kiếm sách trực tuyến (OPAC) thông minh, trực quan, hỗ trợ tìm kiếm thời gian thực và xem chi tiết tình trạng sách trong kho.
- **Độc giả dùng chức năng này vào trường hợp nào? Khi nào?**
  - **Khi tìm kiếm cuốn sách cụ thể:** Nhập từ khóa tên sách, tên tác giả, tên nhà xuất bản hoặc mã ISBN.
  - **Khi lọc sách theo tiêu chí:** Lọc theo Thể loại, lọc theo trạng thái cuốn sách ("Tất cả", "Chỉ sách còn khả dụng") hoặc sắp xếp theo độ phổ biến, sách mới nhất.
  - **Khi kiểm tra vị trí kệ & số lượng sách:** Mở trang chi tiết sách để xem vị trí kệ (`ShelfLocation`), tóm tắt nội dung và danh sách các mã vạch bản sao có sẵn trong thư viện trước khi đến quầy mượn.

#### B. Các trang giao diện:
- **Trang chính:**
  - `ReaderOpacSearchView.xaml` (Giao diện Tra Cứu Sách OPAC).
  - Nằm tại: `LibOps/PresentationLayer/Views/Reader/Opac/ReaderOpacSearchView.xaml`.
- **Trang xem chi tiết:**
  - `ReaderBookDetailView.xaml` (Giao diện Thông Tin Chi Tiết Cuốn Sách).
  - Nằm tại: `LibOps/PresentationLayer/Views/Reader/Opac/ReaderBookDetailView.xaml`.

#### C. Các file mã nguồn xử lý (Code Files & Layers):
- **Views:** `ReaderOpacSearchView.xaml`, `ReaderBookDetailView.xaml`
- **ViewModels:** `ReaderOpacSearchViewModel.cs`, `ReaderBookDetailViewModel.cs`
- **Business Services:** `ReaderPortalService.cs` (hàm `SearchBooks`, `GetBookDetail`)
- **Data Repositories:** `BookRepository.cs`, `BookCopyRepository.cs`, `CategoryRepository.cs`
- **DTOs:** `ReaderBookSearchDto`, `ReaderBookDetailDto` (trong `ReaderPortalDtos.cs`)

---

### 2.3. Mượn Và Trả Sách (Loans & Return History)

#### A. Mô tả chức năng:
- Giúp độc giả tự quản lý toàn bộ các cuốn sách mình đang mượn và tra cứu lịch sử mượn trả từ trước đến nay.
- **Độc giả dùng chức năng này vào trường hợp nào? Khi nào?**
  - **Kiểm tra hạn trả sách (Tab Sách Đang Mượn):** Xem danh sách các cuốn sách đang giữ, ngày mượn, hạn trả và số ngày còn lại (hoặc cảnh báo số ngày quá hạn bôi đỏ) để chủ động mang trả đúng hạn.
  - **Tra cứu lịch sử đọc sách (Tab Lịch Sử Mượn Trả):** Xem toàn bộ các đợt mượn sách trước đây, ngày trả thực tế, tình trạng sách khi trả và tiền phạt đã nộp (nếu có).
  - **Thao tác bảng dữ liệu:** Hỗ trợ thanh cuộn ngang mượt mà khi xem trên màn hình nhỏ và phân trang 10 dòng/trang.

#### B. Các trang giao diện:
- **Trang chính:**
  - `ReaderLoansView.xaml` (Giao diện Quản Lý Mượn & Trả Sách).
  - Nằm tại: `LibOps/PresentationLayer/Views/Reader/Loans/ReaderLoansView.xaml`.
  - Gồm 2 tab: *Sách Đang Mượn* và *Lịch Sử Mượn Trả*.
- **Trang phụ:** Không có.

#### C. Các file mã nguồn xử lý (Code Files & Layers):
- **Views:** `ReaderLoansView.xaml` & `.cs`
- **ViewModels:** `ReaderLoansViewModel.cs`
- **Business Services:** `ReaderPortalService.cs` (hàm `GetActiveLoans`, `GetLoanHistory`)
- **Data Repositories:** `BorrowSlipRepository.cs`, `BorrowDetailRepository.cs`, `ReturnDetailRepository.cs`
- **DTOs:** `ReaderActiveLoanDto`, `ReaderLoanHistoryDto` (trong `ReaderPortalDtos.cs`)

---

### 2.4. Thẻ Thư Viện & Quản Lý Tài Chính (Library Card & VietQR Auto-Banking)

#### A. Mô tả chức năng:
- Hiển thị thẻ thư viện số điện tử, theo dõi biến động số dư cọc thế chân, nợ phạt và tích hợp thanh toán trực tuyến tự động qua mã VietQR PayOS.
- **Độc giả dùng chức năng này vào trường hợp nào? Khi nào?**
  - **Kiểm tra thông tin thẻ:** Xem Mã thẻ (Card Code), Ngày cấp, Ngày hết hạn, Trạng thái thẻ (`ACTIVE`, `EXPIRED`, `LOCKED`, `CLOSED`).
  - **Kiểm tra tài chính:** Xem số dư tiền cọc hiện có và công nợ phạt chưa thanh toán.
  - **Nạp tiền cọc trực tuyến qua VietQR:**
    - Độc giả chọn số tiền muốn nạp (50k, 100k, 200k, 500k hoặc nhập số tùy ý).
    - Hệ thống sinh mã VietQR động chuẩn ngân hàng MBBank kèm số tiền và mã giao dịch.
    - Độc giả mở ứng dụng Mobile Banking quét mã QR chuyển tiền.
    - Cổng PayOS tự động kiểm tra trạng thái và khớp lệnh nạp tiền vào tài khoản độc giả tức thì mà không cần thủ thư can thiệp thủ công.
  - **Xem sao kê lịch sử dòng tiền:** Bảng chi tiết toàn bộ các giao dịch nạp cọc, trừ phạt, hoàn cọc theo thời gian.
  - **Gửi yêu cầu Hủy thẻ & Hoàn cọc:** Bấm nút "Hủy Thẻ & Hoàn Tiền" để mở dialog thu thập tài khoản ngân hàng nhận tiền hoàn.

#### B. Các trang giao diện:
- **Trang chính:**
  - `ReaderFinanceView.xaml` (Giao diện Thẻ Thư Viện & Quản Lý Tài Chính).
  - Nằm tại: `LibOps/PresentationLayer/Views/Reader/Finance/ReaderFinanceView.xaml`.
- **Trang phụ (Hộp thoại / Dialog con):**
  - `VietQRQuickPayDialog.xaml` (Hộp thoại quét mã QR chuyển khoản ngân hàng PayOS tự động).
  - `CloseCardRequestDialog.xaml` (Hộp thoại gửi yêu cầu hủy thẻ và nhập thông tin STK nhận tiền hoàn).

#### C. Các file mã nguồn xử lý (Code Files & Layers):
- **Views:** `ReaderFinanceView.xaml`, `VietQRQuickPayDialog.xaml`, `CloseCardRequestDialog.xaml`
- **ViewModels:** `ReaderFinanceViewModel.cs`, `VietQRQuickPayViewModel.cs`
- **Business Services:** 
  - `ReaderPortalService.cs` (hàm `GetCardSummary`, `GetCashFlowHistories`, `SubmitClosureRequest`)
  - `Payment/PayOSPaymentService.cs` (API tạo đơn thanh toán, tính HMAC-SHA256, polling trạng thái)
- **Data Repositories:** `MemberRepository.cs`, `DepositRepository.cs`, `FineReceiptRepository.cs`
- **DTOs & Utilities:** `ReaderCardSummaryDto`, `ReaderCashFlowDisplayDto`, `PaymentOrderDto`, `VietQRGeneratorUtility.cs`

---

### 2.5. Yêu Cầu Tự Phục Vụ Trực Tuyến (Self-Service Online Requests)

#### A. Mô tả chức năng:
- Cho phép độc giả gửi các yêu cầu nghiệp vụ trực tuyến đến thủ thư và theo dõi tiến trình xét duyệt minh bạch.
- **Độc giả dùng chức năng này vào trường hợp nào? Khi nào?**
  - **Khi bị mất thẻ thư viện:** Độc giả gửi yêu cầu "Cấp lại thẻ do bị mất" kèm lý do và số điện thoại liên hệ.
  - **Khi cần thay đổi thông tin cá nhân:** Độc giả gửi yêu cầu "Cập nhật thông tin" (đổi số điện thoại, đổi địa chỉ, đổi email mới).
  - **Theo dõi kết quả xét duyệt:** Xem danh sách các yêu cầu đã gửi, trạng thái hiện tại (`PENDING` - Chờ duyệt, `APPROVED` - Đã duyệt, `REJECTED` - Bị từ chối) và xem phản hồi/ghi chú của thủ thư gửi lại.

#### B. Các trang giao diện:
- **Trang chính:**
  - `ReaderSelfServiceRequestsView.xaml` (Giao diện Yêu Cầu Tự Phục Vụ).
  - Nằm tại: `LibOps/PresentationLayer/Views/Reader/SelfService/ReaderSelfServiceRequestsView.xaml`.
- **Trang phụ (Hộp thoại / Dialog con):**
  - `CreateSelfServiceRequestDialog.xaml` (Hộp thoại tạo yêu cầu tự phục vụ mới).

#### C. Các file mã nguồn xử lý (Code Files & Layers):
- **Views:** `ReaderSelfServiceRequestsView.xaml`, `CreateSelfServiceRequestDialog.xaml`
- **ViewModels:** `ReaderSelfServiceRequestsViewModel.cs`
- **Business Services:** `ReaderPortalService.cs` (hàm `GetMyRequests`, `SubmitGeneralRequest`)
- **Data Repositories:** `ReaderRequestRepository.cs`
- **DTOs:** `ReaderRequestDisplayDto` (trong `ReaderPortalDtos.cs`)
- **Common Utilities:** `InputValidationUtility.cs` (kiểm tra chuẩn hóa SĐT, Email khi gửi yêu cầu cập nhật thông tin)

---

### 2.6. Thông Tin Cá Nhân & Đổi Mật Khẩu (Reader Profile & Security)

#### A. Mô tả chức năng:
- Quản lý hồ sơ định danh của độc giả và bảo mật tài khoản cá nhân.
- **Độc giả dùng chức năng này vào trường hợp nào? Khi nào?**
  - **Xem hồ sơ cá nhân:** Xem thông tin Mã độc giả, Họ tên, Số điện thoại, Email, Số CCCD, Ngày sinh, Địa chỉ cư trú.
  - **Đổi mật khẩu tài khoản:** Nhập mật khẩu hiện tại, nhập mật khẩu mới và xác nhận mật khẩu mới để đổi mật khẩu bảo mật (mật khẩu được băm SHA-256 kèm mã Salt).
  - **Đăng xuất phiên làm việc:** Nút đăng xuất an toàn tại chân thanh menu bên trái.

#### B. Các trang giao diện:
- **Trang chính:**
  - `ReaderProfileView.xaml` (Giao diện Thông Tin Cá Nhân & Đổi Mật Khẩu).
  - Nằm tại: `LibOps/PresentationLayer/Views/Reader/Profile/ReaderProfileView.xaml`.
- **Trang phụ:** Không có (Form đổi mật khẩu được tích hợp trực tiếp trên trang hồ sơ).

#### C. Các file mã nguồn xử lý (Code Files & Layers):
- **Views:** `ReaderProfileView.xaml` & `.cs`
- **ViewModels:** `ReaderProfileViewModel.cs`
- **Business Services:** `AuthService.cs` (hàm `ChangePassword`), `MemberService.cs`
- **Data Repositories:** `UserRepository.cs`, `MemberRepository.cs`
- **Common Utilities:** `PasswordHashingUtility.cs`, `InputValidationUtility.cs`
