# CÁC CHỨC NĂNG CỦA THỦ THƯ (LIBRARIAN)

---

## 1. DANH SÁCH CÁC CHỨC NĂNG TRÊN SIDEBAR (DANH MỤC)

Theo cấu trúc giao diện menu điều hướng (Sidebar) của hệ thống LibOps, tài khoản có vai trò **Thủ Thư (LIBRARIAN)** được phân quyền toàn bộ 12 mục chức năng vận hành và tác nghiệp sau:

1. **Bảng Tổng Quan Vận Hành** (Dashboard Overview)
2. **Danh Mục Đầu Sách** (Books Catalog Management)
3. **Quản Lý Thể Loại** (Category Management)
4. **Quản Lý Tác Giả** (Author Management)
5. **Quản Lý Nhà Xuất Bản** (Publisher Management)
6. **Quản Lý Bản Sao & In Mã Vạch Barcode** (Book Copies & Barcode Management)
7. **Quản Lý Độc Giả & Nghiệp Vụ Tài Chính Thẻ** (Member & Finance Operations)
8. **Lập Phiếu Mượn Sách** (Create Borrow Slip)
9. **Tiếp Nhận Trả Sách & Xử Lý Nợ Phạt** (Return Book Processing & Fine Collection)
10. **Tra Cứu & Quản Lý Phiếu Mượn/Trả** (Borrow Slip Lookup & Renewal)
11. **Báo Cáo & Thống Kê** (Reporting & Analytics)
12. **Duyệt Yêu Cầu Bạn Đọc** (Reader Requests Processing)
13. **Tiện Ích Đổi Mật Khẩu Cá Nhân & Đăng Xuất** (User Profile Utilities)

---

## 2. CHI TIẾT TỪNG CHỨC NĂNG CỦA THỦ THƯ

---

### 2.1. Bảng Tổng Quan Vận Hành (Dashboard Overview)

#### A. Mô tả chức năng:
- Trung tâm giám sát tức thời toàn bộ hoạt động của thư viện ngay khi thủ thư vừa đăng nhập vào hệ thống.
- **Thủ thư dùng chức năng này vào trường hợp nào? Khi nào?**
  - **Đầu ca làm việc:** Nắm bắt nhanh các chỉ số KPI thời gian thực: Tổng số đầu sách, Tổng số sách đang được mượn, Số độc giả đang hoạt động, Tổng số lượt mượn trong tháng.
  - **Theo dõi biểu đồ xu hướng:** Xem biểu đồ đường trực quan thể hiện tần suất mượn sách theo các ngày trong tuần/tháng.
  - **Cảnh báo sách quá hạn khẩn cấp:** Xem bảng danh sách các cuốn sách mượn quá hạn nhiều ngày nhất cần ưu tiên liên hệ đôn đốc thu hồi.
  - **Theo dõi thị hiếu bạn đọc:** Xem danh sách Top 5 đầu sách được mượn nhiều nhất trong thư viện.
  - **Cảnh báo yêu cầu trực tuyến mới:** Nhận diện ngay số lượng yêu cầu tự phục vụ (hủy thẻ, cấp lại thẻ) độc giả vừa gửi lên đang chờ thủ thư xử lý.

#### B. Các trang giao diện:
- **Trang chính:**
  - `DashboardOverviewView.xaml` (Giao diện Bảng Tổng Quan).
  - Nằm tại: `LibOps/PresentationLayer/Views/Common/Shells/DashboardOverviewView.xaml`.
- **Trang phụ:** Không có.

#### C. Các file mã nguồn xử lý (Code Files & Layers):
- **Views:** `DashboardOverviewView.xaml` & `.cs`
- **ViewModels:** `DashboardOverviewViewModel.cs`
- **Business Services:** `ReportService.cs`
- **Data Repositories:** `BookRepository.cs`, `MemberRepository.cs`, `BorrowSlipRepository.cs`, `ReaderRequestRepository.cs`
- **DTOs:** `DashboardKpiSummaryDto.cs`, `DashboardDetailsDto.cs` (chứa `OverdueBorrowItemDto`, `TopBorrowedBookItemDto`, `PendingReaderRequestItemDto`)

---

### 2.2. Danh Mục Đầu Sách (Books Catalog Management)

#### A. Mô tả chức năng:
- Quản lý toàn bộ danh mục các đầu sách/tác phẩm trong thư viện, vị trí lưu trữ kệ sách và số lượng tồn kho khả dụng.
- **Thủ thư dùng chức năng này vào trường hợp nào? Khi nào?**
  - **Khi nhập thêm đầu sách mới về thư viện:** Tạo mới đầu sách, nhập mã ISBN, tựa đề, giá bìa, vị trí kệ, ảnh bìa, tóm tắt nội dung và gán thể loại, tác giả, nhà xuất bản.
  - **Khi cập nhật thông tin sách:** Chỉnh sửa giá bìa, đổi vị trí kệ sách hoặc cập nhật lại ảnh bìa, mô tả sách.
  - **Khi kiểm tra tồn kho & tình trạng sách:** Xem nhanh tổng số bản sao hiện có (`TotalQuantity`) và số cuốn đang sẵn sàng cho mượn (`AvailableQuantity`).
  - **Khi tra cứu sách:** Tìm kiếm nhanh theo tựa đề sách, mã ISBN, hoặc lọc danh sách theo thể loại, tác giả, nhà xuất bản.

#### B. Các trang giao diện:
- **Trang chính:**
  - `BookListView.xaml` (Giao diện Danh sách Đầu Sách).
  - Nằm tại: `LibOps/PresentationLayer/Views/Librarian/Catalog/Books/BookListView.xaml`.
- **Trang phụ (Hộp thoại / Dialog con):**
  - `BookAddEditDialog.xaml` (Hộp thoại Thêm mới / Chỉnh sửa thông tin đầu sách).
  - `BookDetailDialog.xaml` (Hộp thoại Xem chi tiết đầu sách và danh sách toàn bộ các bản sao cuốn sách).
  - `QuickAddMetadataDialog.xaml` (Hộp thoại thêm nhanh Thể loại/Tác giả/NXB ngay trên form sách).

#### C. Các file mã nguồn xử lý (Code Files & Layers):
- **Views:** `BookListView.xaml`, `BookAddEditDialog.xaml`, `BookDetailDialog.xaml`, `QuickAddMetadataDialog.xaml`
- **ViewModels:** `BookListViewModel.cs`, `BookAddEditViewModel.cs`, `BookDetailDialogViewModel.cs`, `QuickAddMetadataViewModel.cs`
- **Business Services:** `BookService.cs`, `BookMetadataService.cs`
- **Data Repositories:** `BookRepository.cs`, `BookCopyRepository.cs`
- **Entities & DTOs:** `BookEntity.cs`, `BookGridDisplayDtos.cs`

---

### 2.3. Quản Lý Thể Loại (Category Management)

#### A. Mô tả chức năng:
- Quản lý danh mục các thể loại/chủ đề sách trong thư viện (Công nghệ thông tin, Kinh tế, Văn học, Khoa học...).
- **Thủ thư dùng chức năng này vào trường hợp nào? Khi nào?**
  - **Khi mở rộng danh mục sách theo chủ đề mới:** Tạo mới thể loại sách kèm mô tả chi tiết.
  - **Khi chuẩn hóa danh mục:** Chỉnh sửa tên thể loại hoặc mô tả chuyên mục.
  - **Khi tra cứu số lượng sách theo thể loại:** Thống kê xem mỗi thể loại hiện có bao nhiêu đầu sách.

#### B. Các trang giao diện:
- **Trang chính:**
  - `CategoryManagementView.xaml` (Giao diện Quản Lý Thể Loại).
  - Nằm tại: `LibOps/PresentationLayer/Views/Librarian/Catalog/Categories/CategoryManagementView.xaml`.
- **Trang phụ:** Không có.

#### C. Các file mã nguồn xử lý (Code Files & Layers):
- **Views:** `CategoryManagementView.xaml` & `.cs`
- **ViewModels:** `CategoryManagementViewModel.cs`
- **Business Services:** `BookMetadataService.cs`
- **Data Repositories:** `CategoryRepository.cs`
- **Entities:** `CategoryEntity.cs`

---

### 2.4. Quản Lý Tác Giả (Author Management)

#### A. Mô tả chức năng:
- Quản lý hồ sơ danh mục các tác giả sáng tác sách trong và ngoài nước.
- **Thủ thư dùng chức năng này vào trường hợp nào? Khi nào?**
  - **Khi nhập sách của tác giả mới:** Thêm mới tên tác giả và thông tin tóm tắt tiểu sử/ghi chú.
  - **Khi cập nhật thông tin tác giả:** Chỉnh sửa tên tác giả, bổ sung thông tin ghi chú.
  - **Khi tìm kiếm tác giả:** Tìm nhanh tác giả theo tên để lọc các tác phẩm tương ứng.

#### B. Các trang giao diện:
- **Trang chính:**
  - `AuthorManagementView.xaml` (Giao diện Quản Lý Tác Giả).
  - Nằm tại: `LibOps/PresentationLayer/Views/Librarian/Catalog/Authors/AuthorManagementView.xaml`.
- **Trang phụ:** Không có.

#### C. Các file mã nguồn xử lý (Code Files & Layers):
- **Views:** `AuthorManagementView.xaml` & `.cs`
- **ViewModels:** `AuthorManagementViewModel.cs`
- **Business Services:** `BookMetadataService.cs`
- **Data Repositories:** `AuthorRepository.cs`
- **Entities:** `AuthorEntity.cs`

---

### 2.5. Quản Lý Nhà Xuất Bản (Publisher Management)

#### A. Mô tả chức năng:
- Quản lý danh mục các đơn vị nhà xuất bản phát hành sách.
- **Thủ thư dùng chức năng này vào trường hợp nào? Khi nào?**
  - **Khi nhập sách từ nhà xuất bản mới:** Thêm mới tên NXB, địa chỉ trụ sở và số điện thoại liên hệ.
  - **Khi cập nhật thông tin liên hệ NXB:** Thay đổi địa chỉ hoặc số điện thoại của nhà xuất bản.

#### B. Các trang giao diện:
- **Trang chính:**
  - `PublisherManagementView.xaml` (Giao diện Quản Lý Nhà Xuất Bản).
  - Nằm tại: `LibOps/PresentationLayer/Views/Librarian/Catalog/Publishers/PublisherManagementView.xaml`.
- **Trang phụ:** Không có.

#### C. Các file mã nguồn xử lý (Code Files & Layers):
- **Views:** `PublisherManagementView.xaml` & `.cs`
- **ViewModels:** `PublisherManagementViewModel.cs`
- **Business Services:** `BookMetadataService.cs`
- **Data Repositories:** `PublisherRepository.cs`
- **Entities:** `PublisherEntity.cs`
- **Common Utilities:** `InputValidationUtility.cs` (kiểm tra chuẩn hóa số điện thoại NXB)

---

### 2.6. Quản Lý Bản Sao & In Mã Vạch Barcode (Book Copies & Barcode Management)

#### A. Mô tả chức năng:
- Quản lý từng cuốn sách vật lý cụ thể thông qua mã vạch Barcode độc nhất dán trên gáy sách.
- **Thủ thư dùng chức năng này vào trường hợp nào? Khi nào?**
  - **Khi dán nhãn sách mới nhập:** Thủ thư chọn đầu sách, sinh mã vạch riêng cho từng cuốn sách (VD: `BC-CNTT-0001`, `BC-CNTT-0002`), in nhãn mã vạch trực tiếp để dán lên sách phục vụ quét barcode khi mượn trả.
  - **Khi kiểm kê tình trạng sách:** Cập nhật trạng thái từng cuốn sách (`AVAILABLE`, `BORROWED`, `DAMAGED`, `LOST`).
  - **Khi thanh lý hoặc ghi nhận mất sách:** Đổi trạng thái cuốn sách sang hư hại hoặc làm mất kèm ghi chú tình trạng vật lý.

#### B. Các trang giao diện:
- **Trang chính:**
  - `BookCopyBarcodeView.xaml` (Giao diện Quản Lý Mã Vạch & Bản Sao Sách).
  - Nằm tại: `LibOps/PresentationLayer/Views/Librarian/Catalog/Books/BookCopyBarcodeView.xaml`.
- **Trang phụ:** Không có.

#### C. Các file mã nguồn xử lý (Code Files & Layers):
- **Views:** `BookCopyBarcodeView.xaml` & `.cs`
- **ViewModels:** `BookCopyBarcodeViewModel.cs`
- **Business Services:** `BookService.cs`
- **Data Repositories:** `BookCopyRepository.cs`
- **Entities & DTOs:** `BookCopyEntity.cs`, `BookGridDisplayDtos.cs`

---

### 2.7. Quản Lý Độc Giả & Nghiệp Vụ Tài Chính Thẻ (Member & Finance Operations)

#### A. Mô tả chức năng:
- Quản lý hồ sơ độc giả, cấp phát thẻ, nạp tiền cọc, gia hạn thẻ, đóng thẻ hoàn cọc và đối soát cấn trừ nợ thẻ hết hạn.
- **Thủ thư dùng chức năng này vào trường hợp nào? Khi nào?**
  - **Khi độc giả đăng ký mở thẻ tại quầy:** Nhập thông tin độc giả, thu phí mở thẻ và nạp tiền cọc ban đầu để kích hoạt thẻ `ACTIVE`.
  - **Khi độc giả nộp thêm tiền cọc tại quầy:** Mở dialog Nạp cọc, nhập số tiền nạp, ghi nhận sổ cái `DepositTransactions` và in phiếu thu.
  - **Khi thẻ độc giả hết hạn:** Mở dialog Gia hạn thẻ, thu phí thường niên và gia hạn hiệu lực thêm 365 ngày.
  - **Khi kích hoạt lại thẻ bị khóa:** Mở dialog Kích hoạt lại thẻ sau khi độc giả đã khắc phục vi phạm.
  - **Khi độc giả đóng/hủy thẻ tại quầy:** Kiểm tra điều kiện (đã trả hết sách, không còn nợ), hoàn trả tiền cọc thế chân và in Phiếu Chi hoàn cọc nhiệt Bitmap.
  - **Khi đối soát cấn trừ nợ thẻ hết hạn:** Mở chức năng đối soát nợ thẻ hết hạn, hệ thống tự động cấn trừ số nợ phạt vào tiền cọc, cập nhật nợ về 0, chuyển thẻ sang `CLOSED` và xuất phiếu chi hoàn lại tiền cọc thừa (nếu có).

#### B. Các trang giao diện:
- **Trang chính:**
  - `MemberListView.xaml` (Giao diện Quản Lý Độc Giả).
  - Nằm tại: `LibOps/PresentationLayer/Views/Librarian/Members/MemberListView.xaml`.
- **Trang phụ (Hộp thoại / Dialog con):**
  - `MemberDetailDialog.xaml` (Thêm mới / Chỉnh sửa hồ sơ độc giả).
  - `MemberDepositDialog.xaml` (Nộp thêm tiền cọc thế chân tại quầy).
  - `MemberRenewCardDialog.xaml` (Gia hạn thời hạn thẻ & thu phí thường niên).
  - `MemberReactivateDialog.xaml` (Kích hoạt lại thẻ bị khóa).
  - `MemberClosureDialog.xaml` (Đóng/Hủy thẻ & hoàn trả tiền cọc tại quầy).
  - `ExpenseVoucherDialog.xaml` (Xem trước & In phiếu chi hoàn cọc Bitmap).
  - `ExpiredDebtDialog.xaml` (Đối soát và cấn trừ nợ đối với các thẻ đã hết hạn).

#### C. Các file mã nguồn xử lý (Code Files & Layers):
- **Views:** `MemberListView.xaml`, `MemberDetailDialog.xaml`, `MemberDepositDialog.xaml`, `MemberRenewCardDialog.xaml`, `MemberReactivateDialog.xaml`, `MemberClosureDialog.xaml`, `ExpenseVoucherDialog.xaml`, `ExpiredDebtDialog.xaml`
- **ViewModels:** `MemberListViewModel.cs`, `MemberDetailViewModel.cs`, `MemberDepositViewModel.cs`, `MemberRenewCardViewModel.cs`, `MemberReactivateViewModel.cs`, `MemberClosureViewModel.cs`, `ExpiredDebtViewModel.cs`
- **Business Services:** `MemberService.cs`
- **Data Repositories:** `MemberRepository.cs`, `DepositRepository.cs`, `ServiceFeeRepository.cs`, `FineReceiptRepository.cs`
- **Entities & DTOs:** `MemberEntity.cs`, `DepositTransactionEntity.cs`, `ServiceFeeReceiptEntity.cs`, `MemberGridDisplayDto.cs`, `ExpiredMemberDebtDto.cs`
- **Common Utilities:** `InputValidationUtility.cs` (kiểm tra chuẩn hóa SĐT, CCCD, Email, Ngày sinh), `CardAndReceiptPrintUtility.cs`

---

### 2.8. Lập Phiếu Mượn Sách (Create Borrow Slip)

#### A. Mô tả chức năng:
- Lập phiếu mượn sách mới cho độc giả bằng máy quét mã vạch hoặc nhập liệu thủ công.
- **Thủ thư dùng chức năng này vào trường hợp nào? Khi nào?**
  - **Khi độc giả mang sách đến quầy mượn:**
    - Quét mã thẻ độc giả: Hệ thống kiểm tra tức thì tính hợp lệ (Thẻ ACTIVE, còn hạn, không nợ phạt, đủ tiền cọc tối thiểu, chưa vượt số sách tối đa).
    - Quét mã vạch Barcode các cuốn sách muốn mượn: Tự động kiểm tra sách có sẵn (`AVAILABLE`) và thêm vào danh sách mượn.
    - Nhấn "Xác Nhận Cho Mượn": Lưu phiếu mượn, trừ số lượng tồn kho khả dụng và tự động gửi email thông báo xác nhận mượn kèm hạn trả cho độc giả.

#### B. Các trang giao diện:
- **Trang chính:**
  - `CreateBorrowSlipView.xaml` (Giao diện Lập Phiếu Mượn Sách).
  - Nằm tại: `LibOps/PresentationLayer/Views/Librarian/Circulation/CreateBorrowSlipView.xaml`.
- **Trang phụ:** Không có.

#### C. Các file mã nguồn xử lý (Code Files & Layers):
- **Views:** `CreateBorrowSlipView.xaml` & `.cs`
- **ViewModels:** `CreateBorrowSlipViewModel.cs`
- **Business Services:** `BorrowReturnService.cs`, `Notification/EmailNotificationService.cs`
- **Data Repositories:** `BorrowSlipRepository.cs`, `BorrowDetailRepository.cs`, `MemberRepository.cs`, `BookCopyRepository.cs`
- **Entities & DTOs:** `BorrowSlipEntity.cs`, `BorrowSlipDetailEntity.cs`, `BorrowItemDisplayDto.cs`

---

### 2.9. Tiếp Nhận Trả Sách & Xử Lý Nợ Phạt (Return Book Processing & Fine Collection)

#### A. Mô tả chức năng:
- Tiếp nhận sách trả lại, tự động phát hiện trả quá hạn, tính tiền phạt và tiền đền bù hư hại/mất sách.
- **Thủ thư dùng chức năng này vào trường hợp nào? Khi nào?**
  - **Khi độc giả mang sách đến quầy trả:**
    - Quét mã vạch Barcode cuốn sách trả: Hệ thống tự động tra cứu phiếu mượn gốc, tính số ngày quá hạn và tiền phạt quá hạn theo ngày.
    - Đánh giá tình trạng sách: Chọn tình trạng (Nguyên vẹn, Rách nát, Hư hỏng nặng, Bị mất) để hệ thống tự tính tiền bồi thường theo tỷ lệ % giá bìa sách.
    - Thu tiền phạt: Chọn trừ trực tiếp vào tiền cọc thế chân, thu tiền mặt tại quầy hoặc quét mã VietQR.
    - Nhấn "Xác Nhận Trả Sách": Cập nhật sách về trạng thái khả dụng, giải phóng lượt mượn và gửi email biên nhận trả sách cho độc giả.

#### B. Các trang giao diện:
- **Trang chính:**
  - `ReturnBookProcessView.xaml` (Giao diện Tiếp Nhận Trả Sách & Xử Lý Phạt).
  - Nằm tại: `LibOps/PresentationLayer/Views/Librarian/Circulation/ReturnBookProcessView.xaml`.
- **Trang phụ (Hộp thoại / Dialog con):**
  - `SelectBorrowingBookDialog.xaml` (Hộp thoại tìm và chọn sách đang mượn của độc giả khi không có mã vạch vật lý).

#### C. Các file mã nguồn xử lý (Code Files & Layers):
- **Views:** `ReturnBookProcessView.xaml`, `SelectBorrowingBookDialog.xaml`
- **ViewModels:** `ReturnBookProcessViewModel.cs`, `SelectBorrowingBookViewModel.cs`
- **Business Services:** `FineCalculationService.cs`, `Notification/EmailNotificationService.cs`
- **Data Repositories:** `ReturnDetailRepository.cs`, `FineReceiptRepository.cs`, `BorrowSlipRepository.cs`, `DepositRepository.cs`, `BookCopyRepository.cs`
- **Entities & DTOs:** `ReturnSlipDetailEntity.cs`, `FineReceiptEntity.cs`, `ReturnBookLookupDto.cs`, `BulkReturnItemDto.cs`, `ReturnedBookItemDto.cs`

---

### 2.10. Tra Cứu & Quản Lý Phiếu Mượn/Trả (Borrow Slip Lookup & Renewal)

#### A. Mô tả chức năng:
- Tra cứu lịch sử toàn bộ các phiếu mượn/trả trong thư viện và thực hiện gia hạn phiếu mượn.
- **Thủ thư dùng chức năng này vào trường hợp nào? Khi nào?**
  - **Khi tra cứu thông tin mượn trả:** Tìm kiếm phiếu mượn theo mã phiếu, mã thẻ độc giả, tên độc giả hoặc lọc theo trạng thái (`BORROWING`, `RETURNED`, `OVERDUE`).
  - **Khi độc giả yêu cầu gia hạn sách:** Thủ thư kiểm tra điều kiện gia hạn (chưa quá hạn, chưa vượt quá số lần gia hạn cho phép) và nhấn "Gia Hạn" để tăng thêm ngày mượn.

#### B. Các trang giao diện:
- **Trang chính:**
  - `BorrowSlipListView.xaml` (Giao diện Tra Cứu Phiếu Mượn/Trả).
  - Nằm tại: `LibOps/PresentationLayer/Views/Librarian/Circulation/BorrowSlipListView.xaml`.
- **Trang phụ:** Không có.

#### C. Các file mã nguồn xử lý (Code Files & Layers):
- **Views:** `BorrowSlipListView.xaml` & `.cs`
- **ViewModels:** `BorrowSlipListViewModel.cs`
- **Business Services:** `BorrowReturnService.cs`
- **Data Repositories:** `BorrowSlipRepository.cs`
- **Entities & DTOs:** `BorrowSlipEntity.cs`, `HistoryDisplayDtos.cs`

---

### 2.11. Báo Cáo & Thống Kê (Reporting & Analytics)

#### A. Mô tả chức năng:
- Cung cấp số liệu thống kê về tình trạng sách quá hạn và theo dõi các khoản thu/chi tại quầy.
- **Thủ thư dùng chức năng này vào trường hợp nào? Khi nào?**
  - **Hàng ngày:** Kiểm tra danh sách sách quá hạn để gọi điện, gửi email đôn đốc bạn đọc trả sách.
  - **Cuối ca làm việc:** Kiểm tra tổng số tiền mặt và chuyển khoản đã thu trong ca (thu cọc, thu phạt, thu phí gia hạn, chi hoàn cọc).
  - **Khi cần xuất báo cáo:** Bấm nút xuất Excel để bàn giao ca hoặc nộp báo cáo (hệ thống tự động xuất tệp `.xlsx` chuẩn OpenXML có sẵn màu sắc tiêu đề, căn lề và kẻ viền).

#### B. Các trang giao diện:
- **Trang chính:**
  - `ReportAnalyticsView.xaml` (Giao diện Báo Cáo & Thống Kê).
  - Nằm tại: `LibOps/PresentationLayer/Views/Admin/Analytics/ReportAnalyticsView.xaml`.
- **Trang phụ:**
  - Hộp thoại hệ thống `SaveFileDialog` (chọn nơi lưu tệp Excel `.xlsx`).

#### C. Các file mã nguồn xử lý (Code Files & Layers):
- **Views:** `ReportAnalyticsView.xaml` & `.cs`
- **ViewModels:** `ReportAnalyticsViewModel.cs`
- **Business Services:** `ReportService.cs`
- **Data Repositories:** `BorrowSlipRepository.cs`, `DepositRepository.cs`, `FineReceiptRepository.cs`, `ServiceFeeRepository.cs`
- **Utilities & DTOs:** `ExcelExportUtility.cs` (ClosedXML OpenXML `.xlsx` Engine), `OverdueReportDto.cs`, `FinancialTransactionDisplayDto.cs`

---

### 2.12. Duyệt Yêu Cầu Bạn Đọc (Reader Requests Processing)

#### A. Mô tả chức năng:
- Tiếp nhận, xem xét và xử lý các yêu cầu trực tuyến do độc giả gửi từ cổng OPAC.
- **Thủ thư dùng chức năng này vào trường hợp nào? Khi nào?**
  - **Khi độc giả gửi yêu cầu Hủy thẻ & Hoàn cọc trực tuyến:** Thủ thư mở chi tiết yêu cầu, kiểm tra số tiền hoàn và tài khoản ngân hàng thụ hưởng, nhấn "Phê duyệt" để đóng thẻ, ghi sổ hoàn cọc và xuất phiếu chi tiền.
  - **Khi độc giả gửi yêu cầu Cấp lại thẻ mất / Sửa thông tin:** Thủ thư xem xét lý do, nhập ghi chú phản hồi và bấm "Phê duyệt" hoặc "Từ chối".

#### B. Các trang giao diện:
- **Trang chính:**
  - `ReaderRequestListView.xaml` (Giao diện Duyệt Yêu Cầu Bạn Đọc, 4 thẻ KPI thống kê, bộ lọc trạng thái).
  - Nằm tại: `LibOps/PresentationLayer/Views/Librarian/ReaderRequests/ReaderRequestListView.xaml`.
- **Trang phụ (Hộp thoại / Dialog con):**
  - `RequestDetailDialog.xaml` (Hộp thoại Xem chi tiết, nhập ghi chú phản hồi và bấm Phê Duyệt / Từ Chối).

#### C. Các file mã nguồn xử lý (Code Files & Layers):
- **Views:** `ReaderRequestListView.xaml`, `RequestDetailDialog.xaml`
- **ViewModels:** `ReaderRequestListViewModel.cs`
- **Business Services:** `ReaderRequestService.cs`
- **Data Repositories:** `ReaderRequestRepository.cs`, `MemberRepository.cs`, `DepositRepository.cs`
- **Entities & DTOs:** `ReaderRequestEntity.cs`, `ReaderRequestDisplayDto` (trong `ReaderPortalDtos.cs`)

---

### 2.13. Tiện Ích Đổi Mật Khẩu Cá Nhân & Đăng Xuất (User Profile Utilities)

#### A. Mô tả chức năng:
- Cho phép thủ thư chủ động thay đổi mật khẩu đăng nhập của chính mình và kết thúc phiên làm việc an toàn.
- **Thủ thư dùng chức năng này vào trường hợp nào? Khi nào?**
  - **Đổi mật khẩu:** Khi nghi ngờ lộ mật khẩu hoặc thực hiện chính sách đổi mật khẩu định kỳ.
  - **Đăng xuất:** Khi hết ca làm việc hoặc rời khỏi máy trạm làm việc.

#### B. Các trang giao diện:
- **Trang phụ (Hộp thoại / Dialog con):**
  - `ChangePasswordDialog.xaml` (Hộp thoại Đổi Mật Khẩu Cá Nhân).
  - Nằm tại: `LibOps/PresentationLayer/Views/Common/Auth/ChangePasswordDialog.xaml`.

#### C. Các file mã nguồn xử lý (Code Files & Layers):
- **Views:** `ChangePasswordDialog.xaml` & `.cs`
- **ViewModels:** `ChangePasswordViewModel.cs`
- **Business Services:** `AuthService.cs`
- **Data Repositories:** `UserRepository.cs`
- **Utilities:** `PasswordHashingUtility.cs`
