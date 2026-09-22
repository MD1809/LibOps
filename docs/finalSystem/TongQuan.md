# TỔNG QUAN HỆ THỐNG QUẢN LÝ THƯ VIỆN LIBOPS

---

## 1. THÔNG TIN VỀ DỰ ÁN

- **Tên dự án:** LibOps - Hệ Thống Quản Lý Thư Viện Thông Minh & Vận Hành Tự Động (Smart Library Operations System)
- **Mô tả & Giới thiệu:**
  - LibOps là phần mềm ứng dụng desktop chuyên nghiệp dành cho việc quản lý và vận hành toàn diện thư viện hiện đại.
  - Dự án được xây dựng trên nền tảng WPF (Windows Presentation Foundation) với ngôn ngữ C# theo mô hình kiến trúc MVVM 3 tầng (3-Tier Layered Architecture).
  - Hệ thống tích hợp các công nghệ hiện đại như thanh toán tự động VietQR PayOS, in ấn đồ họa phiếu thu/chi bitmap chuẩn in nhiệt, tra cứu trực tuyến OPAC cho độc giả, gửi email thông báo tự động, đối soát cấn trừ tài chính thẻ hết hạn và báo cáo thống kê thời gian thực.
- **Phiên bản hiện tại:** Phiên bản 2.1 (LibOps v2.1 Final Release)

---

## 2. MỤC ĐÍCH HỆ THỐNG

### 2.1. Hệ thống này giúp giải quyết vấn đề gì?
- **Xóa bỏ quản lý thủ công rời rạc:** Thay thế hoàn toàn việc ghi chép sổ sách hoặc file Excel phân tán, hạn chế tối đa sai sót, thất lạc dữ liệu và khó khăn khi tra cứu lịch sử mượn trả.
- **Tự động hóa quy trình lưu thông sách:** Quản lý từng bản sao cuốn sách theo mã vạch cá biệt (Barcode), lập phiếu mượn và xử lý trả sách nhanh chóng, chính xác.
- **Quản lý tài chính và dòng tiền minh bạch:**
  - Theo dõi chi tiết sổ quỹ tiền cọc thế chân của độc giả (nạp cọc ban đầu, nạp thêm qua VietQR, hoàn trả cọc khi hủy thẻ).
  - Tự động tính toán tiền phạt quá hạn, tiền đền bù hư hỏng/mất sách và thu phí gia hạn thẻ thường niên.
  - Cung cấp cơ chế đối soát cấn trừ nợ tự động đối với các thẻ thư viện đã hết hạn.
- **Tối ưu trải nghiệm độc giả với cổng tự phục vụ (OPAC):**
  - Độc giả có thể tự tra cứu danh mục sách, xem tình trạng sách còn/hết.
  - Độc giả theo dõi lịch sử mượn trả, kiểm tra số dư cọc, nợ phạt hiện tại.
  - Độc giả có thể nạp tiền cọc trực tuyến qua mã VietQR động với cơ chế tự động khớp lệnh giao dịch.
  - Độc giả gửi yêu cầu trực tuyến (hủy thẻ hoàn cọc, cấp lại thẻ, sửa đổi thông tin cá nhân).
- **Cảnh báo và tương tác tự động qua Email:** Hệ thống tự động gửi email thông báo khi độc giả mượn sách, trả sách, nhắc nhở sắp đến hạn trả và cảnh báo quá hạn.
- **Báo cáo và ra quyết định thông minh:** Cung cấp bảng điều khiển Dashboard KPI trực quan, biểu đồ mượn trả, báo cáo sách quá hạn và báo cáo biến động dòng tiền thu - chi theo thời gian thực.

### 2.2. Những ai sử dụng hệ thống này?
- **Quản trị viên hệ thống (Admin):**
  - Quản lý danh sách tài khoản nhân viên và phân quyền sử dụng.
  - Cấu hình các tham số vận hành nghiệp vụ toàn hệ thống (số sách mượn tối đa, số ngày mượn chuẩn, mức phí cọc tối thiểu, phí phạt theo ngày, tỷ lệ đền bù mất sách).
  - Cấu hình máy chủ Email SMTP và thông số cổng thanh toán VietQR.
  - Theo dõi các báo cáo tổng quan cấp cao về doanh thu và dòng tiền thư viện.
  - Các chức năng của thủ thư.
- **Thủ thư (Librarian):**
  - Quản lý danh mục sách, thể loại, tác giả, nhà xuất bản và cấp phát mã vạch bản sao sách.
  - Quản lý hồ sơ độc giả, cấp mới thẻ, gia hạn thời hạn thẻ, kích hoạt lại thẻ hoặc khóa thẻ.
  - Thực hiện nghiệp vụ mượn sách, trả sách, tính phí phạt và in biên lai thu/chi.
  - Tiếp nhận, xét duyệt và xử lý các yêu cầu tự phục vụ trực tuyến từ độc giả.
  - Thực hiện đối soát cấn trừ nợ thẻ hết hạn và xuất phiếu chi tiền cọc thừa.
- **Độc giả (Reader):**
  - Đăng nhập vào cổng thông tin độc giả bằng mã thẻ thư viện.
  - Tra cứu sách trong kho (OPAC) theo tên, tác giả, thể loại, nhà xuất bản.
  - Xem thông tin thẻ thư viện, hạn dùng thẻ, số dư tiền cọc và công nợ phạt.
  - Quét mã QR thanh toán nạp tiền cọc qua ứng dụng ngân hàng di động.
  - Gửi các yêu cầu hỗ trợ trực tuyến đến thủ thư và theo dõi kết quả xét duyệt.

---

## 3. CÔNG NGHỆ VÀ THƯ VIỆN SỬ DỤNG

Hệ thống **LibOps** được xây dựng trên nền tảng Microsoft .NET với các công nghệ và thư viện chuyên dụng sau:

### 3.1. Nền tảng & Ngôn ngữ cốt lõi
- **Ngôn ngữ lập trình:** C# (Cú pháp C# 7.3 / 8.0) - Xử lý toàn bộ logic nghiệp vụ, bảo mật và kết nối dữ liệu.
- **Nền tảng Runtime:** .NET Framework 4.7.2 - Môi trường thực thi ổn định trên Windows.
- **Framework Giao diện:** WPF (Windows Presentation Foundation) - Sử dụng ngôn ngữ khai báo XAML, áp dụng mô hình kiến trúc MVVM (Model - View - ViewModel) và Data Binding 2 chiều.
- **Hệ quản trị CSDL:** Microsoft SQL Server LocalDB (`(localdb)\MSSQLLocalDB`) - Phiên bản CSDL nhỏ gọn, tích hợp sẵn, không cần cài đặt máy chủ riêng biệt.
- **Phương thức truy cập CSDL:** Pure ADO.NET (`System.Data.SqlClient`) - Sử dụng `SqlCommand`, `SqlDataReader`, `SqlTransaction` để tối đa hóa tốc độ truy vấn và kiểm soát giao dịch dữ liệu tài chính (ACID).

---

### 3.2. Danh sách các thư viện bên thứ ba (NuGet Packages)
- **ClosedXML (v0.105.1):** Hỗ trợ xuất và đọc báo cáo Excel OpenXML (`.xlsx`) chuẩn nghiệp vụ, tự động kẻ khung, bôi màu và căn chỉnh cột.
- **LiveCharts.Wpf (v0.9.7):** Vẽ biểu đồ đường, cột và tròn tương tác trên Dashboard quản trị và báo cáo tài chính.
- **MahApps.Metro.IconPacks.Material (v6.2.1):** Cung cấp hệ thống Vector Icon theo chuẩn Material Design cho toàn bộ menu, thanh điều hướng và nút bấm.
- **FontAwesome (v4.7.0):** Thư viện icon bổ trợ cho các nút thao tác nhanh và trạng thái nghiệp vụ.
- **payOS (v2.1.0):** SDK tích hợp cổng thanh toán trực tuyến VietQR PayOS Auto-Banking, tự động tạo mã QR động và xác thực giao dịch chuyển khoản.
- **QRCoder (v1.8.0):** Sinh mã QR Code nội bộ phục vụ tra cứu nhanh và quét thanh toán tại quầy.
- **Newtonsoft.Json (v13.0.4):** Xử lý tuần tự hóa và giải tuần tự hóa JSON phục vụ cấu hình và gọi API thanh toán.

---

### 3.3. Các Module & Namespace .NET tích hợp sẵn
- **System.Security.Cryptography:** Thuật toán băm một chiều SHA-256 kết hợp Salt ngẫu nhiên 32-byte để bảo mật mật khẩu người dùng (`PasswordHashingUtility`).
- **System.Net.Mail:** Module gửi Email SMTP tự động thông báo giao dịch mượn/trả sách, nhắc hạn trả và cảnh báo vi phạm.
- **System.Drawing (GDI+):** Render đồ họa phiếu thu tiền cọc, phiếu phạt và phiếu chi in nhiệt khổ 80mm định dạng Bitmap.
- **System.Configuration:** Đọc chuỗi kết nối CSDL và các tham số cấu hình hệ thống từ file `App.config`.

---

> [!NOTE]
> Chi tiết về các bước chuẩn bị mã nguồn, nạp cơ sở dữ liệu và vận hành ứng dụng đã được trình bày đầy đủ tại tài liệu riêng biệt:  
> 🔗 [HƯỚNG DẪN CÀI ĐẶT VÀ VẬN HÀNH HỆ THỐNG LIBOPS - Quản lý thư viện (ApplicationRunDocument.md)](file:///d:/Phenikaa/Dotnet/LibOps/docs/finalSystem/ApplicationRunDocument.md)
