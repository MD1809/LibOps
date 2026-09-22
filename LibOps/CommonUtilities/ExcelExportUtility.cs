using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using ClosedXML.Excel;
using Microsoft.Win32;
using LibOps.DataModels.DataTransferObjects;

namespace LibOps.CommonUtilities
{
    /// <summary>
    /// Tiện ích xuất dữ liệu ra tệp Microsoft Excel chuẩn OpenXML (*.xlsx)
    /// </summary>
    public static class ExcelExportUtility
    {
        private static readonly XLColor PrimaryHeaderColor = XLColor.FromHtml("#1E3A8A"); // Xanh Navy đậm sang trọng
        private static readonly XLColor TableHeaderColor = XLColor.FromHtml("#2563EB");  // Xanh Royal Blue
        private static readonly XLColor ZebraLightColor = XLColor.FromHtml("#F8FAFC");   // Xám trắng nhạt xen kẽ

        /// <summary>
        /// Xuất dữ liệu từ DataTable ra tệp Excel (.xlsx)
        /// </summary>
        public static bool ExportDataTableToExcel(DataTable dt, string defaultFileName, string reportTitle = null)
        {
            if (dt == null || dt.Rows.Count == 0)
            {
                MessageBox.Show("Không có dữ liệu để xuất báo cáo!", "Thông Báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }

            var saveDialog = new SaveFileDialog
            {
                Filter = "Tệp Microsoft Excel (*.xlsx)|*.xlsx",
                FileName = $"{defaultFileName}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                Title = "Lưu Báo Cáo Excel"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    using (var workbook = new XLWorkbook())
                    {
                        var ws = workbook.Worksheets.Add("Báo Cáo");
                        int currentRow = 1;

                        // Tiêu đề báo cáo
                        if (!string.IsNullOrWhiteSpace(reportTitle))
                        {
                            ws.Cell(currentRow, 1).Value = reportTitle.ToUpper();
                            ws.Cell(currentRow, 1).Style.Font.Bold = true;
                            ws.Cell(currentRow, 1).Style.Font.FontSize = 14;
                            ws.Cell(currentRow, 1).Style.Font.FontColor = PrimaryHeaderColor;
                            ws.Range(currentRow, 1, currentRow, dt.Columns.Count).Merge();
                            currentRow++;

                            ws.Cell(currentRow, 1).Value = $"Thời gian xuất báo cáo: {DateTime.Now:dd/MM/yyyy HH:mm:ss}";
                            ws.Cell(currentRow, 1).Style.Font.Italic = true;
                            ws.Cell(currentRow, 1).Style.Font.FontSize = 10;
                            ws.Cell(currentRow, 1).Style.Font.FontColor = XLColor.DarkGray;
                            ws.Range(currentRow, 1, currentRow, dt.Columns.Count).Merge();
                            currentRow += 2;
                        }

                        int headerStartRow = currentRow;

                        // Header Columns
                        for (int c = 0; c < dt.Columns.Count; c++)
                        {
                            var cell = ws.Cell(currentRow, c + 1);
                            cell.Value = dt.Columns[c].ColumnName;
                            cell.Style.Font.Bold = true;
                            cell.Style.Font.FontColor = XLColor.White;
                            cell.Style.Fill.BackgroundColor = TableHeaderColor;
                            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        }
                        ws.Row(currentRow).Height = 26;
                        currentRow++;

                        // Data Rows
                        int dataStartRow = currentRow;
                        for (int r = 0; r < dt.Rows.Count; r++)
                        {
                            var row = dt.Rows[r];
                            for (int c = 0; c < dt.Columns.Count; c++)
                            {
                                var cell = ws.Cell(currentRow, c + 1);
                                object val = row[c];
                                cell.Value = val != DBNull.Value ? val.ToString() : string.Empty;

                                // Zebra striping
                                if (r % 2 == 1)
                                {
                                    cell.Style.Fill.BackgroundColor = ZebraLightColor;
                                }
                            }
                            currentRow++;
                        }

                        // Kẻ viền (Border) toàn bộ bảng dữ liệu
                        var dataRange = ws.Range(headerStartRow, 1, currentRow - 1, dt.Columns.Count);
                        dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                        dataRange.Style.Border.InsideBorderColor = XLColor.LightGray;
                        dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
                        dataRange.Style.Border.OutsideBorderColor = TableHeaderColor;

                        // Tự động co giãn độ rộng cột
                        ws.Columns().AdjustToContents();

                        workbook.SaveAs(saveDialog.FileName);
                    }

                    var result = MessageBox.Show(
                        $"Xuất dữ liệu thành công ra tệp Excel (.xlsx):\r\n{saveDialog.FileName}\r\n\r\nBạn có muốn mở tệp ngay không?",
                        "Xuất Thành Công",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information
                    );

                    if (result == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start(saveDialog.FileName);
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi khi ghi tệp Excel: " + ex.Message, "Lỗi Xuất File", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }

            return false;
        }

        /// <summary>
        /// Xuất báo cáo sách quá hạn sang tệp Excel (.xlsx) với định dạng chi tiết
        /// </summary>
        public static bool ExportOverdueReportToExcel(IEnumerable<OverdueReportDto> items)
        {
            var list = items?.ToList() ?? new List<OverdueReportDto>();
            if (list.Count == 0)
            {
                MessageBox.Show("Không có dữ liệu phiếu mượn quá hạn để xuất!", "Thông Báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }

            var saveDialog = new SaveFileDialog
            {
                Filter = "Tệp Microsoft Excel (*.xlsx)|*.xlsx",
                FileName = $"BaoCao_PhieuMuon_QuaHan_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                Title = "Lưu Báo Cáo Sách Quá Hạn"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    using (var workbook = new XLWorkbook())
                    {
                        var ws = workbook.Worksheets.Add("Sach_Qua_Han");

                        // 1. Tiêu đề
                        ws.Cell(1, 1).Value = "BÁO CÁO DANH SÁCH ĐỘC GIẢ MƯỢN SÁCH QUÁ HẠN";
                        ws.Cell(1, 1).Style.Font.Bold = true;
                        ws.Cell(1, 1).Style.Font.FontSize = 14;
                        ws.Cell(1, 1).Style.Font.FontColor = PrimaryHeaderColor;
                        ws.Range(1, 1, 1, 11).Merge();

                        ws.Cell(2, 1).Value = $"Thời gian xuất: {DateTime.Now:dd/MM/yyyy HH:mm:ss} | Tổng số lượt quá hạn: {list.Count:N0}";
                        ws.Cell(2, 1).Style.Font.Italic = true;
                        ws.Cell(2, 1).Style.Font.FontSize = 10;
                        ws.Cell(2, 1).Style.Font.FontColor = XLColor.DarkGray;
                        ws.Range(2, 1, 2, 11).Merge();

                        // 2. Headers
                        string[] headers = {
                            "STT", "Mã Phiếu", "Mã Thẻ", "Tên Độc Giả", "Số Điện Thoại",
                            "Tựa Đề Sách", "Mã Vạch", "Ngày Mượn", "Hạn Trả", "Số Ngày Quá Hạn", "Dự Tính Phạt"
                        };

                        int headerRow = 4;
                        for (int i = 0; i < headers.Length; i++)
                        {
                            var cell = ws.Cell(headerRow, i + 1);
                            cell.Value = headers[i];
                            cell.Style.Font.Bold = true;
                            cell.Style.Font.FontColor = XLColor.White;
                            cell.Style.Fill.BackgroundColor = TableHeaderColor;
                            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        }
                        ws.Row(headerRow).Height = 26;

                        // 3. Data Rows
                        int currentRow = 5;
                        decimal totalFine = 0;

                        for (int i = 0; i < list.Count; i++)
                        {
                            var item = list[i];
                            totalFine += item.EstimatedFine;

                            ws.Cell(currentRow, 1).Value = i + 1;
                            ws.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                            ws.Cell(currentRow, 2).Value = item.SlipCode;
                            ws.Cell(currentRow, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                            ws.Cell(currentRow, 3).Value = item.MemberCardCode;
                            ws.Cell(currentRow, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                            ws.Cell(currentRow, 4).Value = item.MemberFullName;
                            ws.Cell(currentRow, 5).Value = item.PhoneNumber;
                            ws.Cell(currentRow, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                            ws.Cell(currentRow, 6).Value = item.BookTitle;

                            ws.Cell(currentRow, 7).Value = item.Barcode;
                            ws.Cell(currentRow, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                            ws.Cell(currentRow, 8).Value = item.BorrowDate.ToString("dd/MM/yyyy");
                            ws.Cell(currentRow, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                            ws.Cell(currentRow, 9).Value = item.DueDate.ToString("dd/MM/yyyy");
                            ws.Cell(currentRow, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                            ws.Cell(currentRow, 10).Value = item.OverdueDays;
                            ws.Cell(currentRow, 10).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            ws.Cell(currentRow, 10).Style.Font.FontColor = XLColor.Red;
                            ws.Cell(currentRow, 10).Style.Font.Bold = true;

                            ws.Cell(currentRow, 11).Value = item.EstimatedFine;
                            ws.Cell(currentRow, 11).Style.NumberFormat.Format = "#,##0 \"VNĐ\"";
                            ws.Cell(currentRow, 11).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                            // Zebra row
                            if (i % 2 == 1)
                            {
                                ws.Range(currentRow, 1, currentRow, 11).Style.Fill.BackgroundColor = ZebraLightColor;
                            }

                            currentRow++;
                        }

                        // 4. Dòng Tổng Cộng
                        ws.Cell(currentRow, 1).Value = "TỔNG CỘNG";
                        ws.Cell(currentRow, 1).Style.Font.Bold = true;
                        ws.Range(currentRow, 1, currentRow, 10).Merge();
                        ws.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                        ws.Cell(currentRow, 11).Value = totalFine;
                        ws.Cell(currentRow, 11).Style.NumberFormat.Format = "#,##0 \"VNĐ\"";
                        ws.Cell(currentRow, 11).Style.Font.Bold = true;
                        ws.Cell(currentRow, 11).Style.Font.FontColor = XLColor.DarkRed;
                        ws.Cell(currentRow, 11).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                        ws.Range(currentRow, 1, currentRow, 11).Style.Fill.BackgroundColor = XLColor.FromHtml("#FEF3C7"); // Vàng nhạt nổi bật

                        // 5. Borders & Auto-fit
                        var dataRange = ws.Range(headerRow, 1, currentRow, 11);
                        dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                        dataRange.Style.Border.InsideBorderColor = XLColor.LightGray;
                        dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
                        dataRange.Style.Border.OutsideBorderColor = TableHeaderColor;

                        ws.Columns().AdjustToContents();

                        workbook.SaveAs(saveDialog.FileName);
                    }

                    var result = MessageBox.Show(
                        $"Xuất dữ liệu thành công ra tệp Excel (.xlsx):\r\n{saveDialog.FileName}\r\n\r\nBạn có muốn mở tệp ngay không?",
                        "Xuất Báo Cáo Thành Công",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information
                    );

                    if (result == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start(saveDialog.FileName);
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi khi xuất tệp Excel: " + ex.Message, "Lỗi Xuất File", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }

            return false;
        }

        /// <summary>
        /// Xuất báo cáo tài chính & dòng tiền sang tệp Excel (.xlsx) với định dạng chi tiết
        /// </summary>
        public static bool ExportCashFlowReportToExcel(
            IEnumerable<FinancialTransactionDisplayDto> items,
            DateTime fromDate,
            DateTime toDate,
            decimal totalInflow,
            decimal totalOutflow,
            decimal netCashFlow)
        {
            var list = items?.ToList() ?? new List<FinancialTransactionDisplayDto>();
            if (list.Count == 0)
            {
                MessageBox.Show("Không có dữ liệu dòng tiền trong khoảng thời gian này!", "Thông Báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }

            var saveDialog = new SaveFileDialog
            {
                Filter = "Tệp Microsoft Excel (*.xlsx)|*.xlsx",
                FileName = $"BaoCao_DongTien_{fromDate:yyyyMMdd}_den_{toDate:yyyyMMdd}_{DateTime.Now:HHmmss}.xlsx",
                Title = "Lưu Báo Cáo Dòng Tiền"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    using (var workbook = new XLWorkbook())
                    {
                        var ws = workbook.Worksheets.Add("Dong_Tien_Thu_Chi");

                        // 1. Tiêu đề
                        ws.Cell(1, 1).Value = "BÁO CÁO DÒNG TIỀN VÀ THU CHI TÀI CHÍNH";
                        ws.Cell(1, 1).Style.Font.Bold = true;
                        ws.Cell(1, 1).Style.Font.FontSize = 14;
                        ws.Cell(1, 1).Style.Font.FontColor = PrimaryHeaderColor;
                        ws.Range(1, 1, 1, 11).Merge();

                        ws.Cell(2, 1).Value = $"Khoảng thời gian: từ {fromDate:dd/MM/yyyy} đến {toDate:dd/MM/yyyy} | Tổng thu: {totalInflow:N0} VNĐ | Tổng chi: {totalOutflow:N0} VNĐ | Dòng tiền thuần: {netCashFlow:N0} VNĐ";
                        ws.Cell(2, 1).Style.Font.Italic = true;
                        ws.Cell(2, 1).Style.Font.FontSize = 10;
                        ws.Cell(2, 1).Style.Font.FontColor = XLColor.DarkGray;
                        ws.Range(2, 1, 2, 11).Merge();

                        // 2. Headers
                        string[] headers = {
                            "STT", "Mã Giao Dịch", "Loại Dòng Tiền", "Phân Loại Chi Tiết", "Số Tiền (VNĐ)",
                            "Hướng Dòng Tiền", "Hình Thức", "Mã Thẻ", "Tên Độc Giả", "Thời Gian", "Ghi Chú"
                        };

                        int headerRow = 4;
                        for (int i = 0; i < headers.Length; i++)
                        {
                            var cell = ws.Cell(headerRow, i + 1);
                            cell.Value = headers[i];
                            cell.Style.Font.Bold = true;
                            cell.Style.Font.FontColor = XLColor.White;
                            cell.Style.Fill.BackgroundColor = TableHeaderColor;
                            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        }
                        ws.Row(headerRow).Height = 26;

                        // 3. Data Rows
                        int currentRow = 5;

                        for (int i = 0; i < list.Count; i++)
                        {
                            var item = list[i];

                            ws.Cell(currentRow, 1).Value = i + 1;
                            ws.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                            ws.Cell(currentRow, 2).Value = item.Code;
                            ws.Cell(currentRow, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                            ws.Cell(currentRow, 3).Value = item.TransactionCategory;
                            ws.Cell(currentRow, 4).Value = item.TransactionTypeDisplay;

                            // Số tiền
                            ws.Cell(currentRow, 5).Value = item.Amount;
                            ws.Cell(currentRow, 5).Style.NumberFormat.Format = "#,##0 \"VNĐ\"";
                            ws.Cell(currentRow, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                            ws.Cell(currentRow, 5).Style.Font.Bold = true;
                            ws.Cell(currentRow, 5).Style.Font.FontColor = item.IsInflow ? XLColor.FromHtml("#16A34A") : XLColor.FromHtml("#DC2626");

                            // Hướng dòng tiền
                            ws.Cell(currentRow, 6).Value = item.IsInflow ? "THU VÀO (+)" : "CHI RA (-)";
                            ws.Cell(currentRow, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            ws.Cell(currentRow, 6).Style.Font.FontColor = item.IsInflow ? XLColor.FromHtml("#16A34A") : XLColor.FromHtml("#DC2626");

                            ws.Cell(currentRow, 7).Value = item.PaymentMethod;
                            ws.Cell(currentRow, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                            ws.Cell(currentRow, 8).Value = item.MemberCardCode;
                            ws.Cell(currentRow, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                            ws.Cell(currentRow, 9).Value = item.MemberFullName;
                            ws.Cell(currentRow, 10).Value = item.TransactionDate.ToString("dd/MM/yyyy HH:mm");
                            ws.Cell(currentRow, 10).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                            ws.Cell(currentRow, 11).Value = item.Notes;

                            if (i % 2 == 1)
                            {
                                ws.Range(currentRow, 1, currentRow, 11).Style.Fill.BackgroundColor = ZebraLightColor;
                            }

                            currentRow++;
                        }

                        // 4. Tổng Kết Dòng Tiền
                        ws.Cell(currentRow, 1).Value = "TỔNG THU:";
                        ws.Cell(currentRow, 1).Style.Font.Bold = true;
                        ws.Range(currentRow, 1, currentRow, 4).Merge();
                        ws.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                        ws.Cell(currentRow, 5).Value = totalInflow;
                        ws.Cell(currentRow, 5).Style.NumberFormat.Format = "#,##0 \"VNĐ\"";
                        ws.Cell(currentRow, 5).Style.Font.Bold = true;
                        ws.Cell(currentRow, 5).Style.Font.FontColor = XLColor.FromHtml("#16A34A");
                        ws.Range(currentRow, 1, currentRow, 11).Style.Fill.BackgroundColor = XLColor.FromHtml("#DCFCE7");
                        currentRow++;

                        ws.Cell(currentRow, 1).Value = "TỔNG CHI:";
                        ws.Cell(currentRow, 1).Style.Font.Bold = true;
                        ws.Range(currentRow, 1, currentRow, 4).Merge();
                        ws.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                        ws.Cell(currentRow, 5).Value = totalOutflow;
                        ws.Cell(currentRow, 5).Style.NumberFormat.Format = "#,##0 \"VNĐ\"";
                        ws.Cell(currentRow, 5).Style.Font.Bold = true;
                        ws.Cell(currentRow, 5).Style.Font.FontColor = XLColor.FromHtml("#DC2626");
                        ws.Range(currentRow, 1, currentRow, 11).Style.Fill.BackgroundColor = XLColor.FromHtml("#FEE2E2");
                        currentRow++;

                        ws.Cell(currentRow, 1).Value = "DÒNG TIỀN THUẦN (NET CASH FLOW):";
                        ws.Cell(currentRow, 1).Style.Font.Bold = true;
                        ws.Range(currentRow, 1, currentRow, 4).Merge();
                        ws.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                        ws.Cell(currentRow, 5).Value = netCashFlow;
                        ws.Cell(currentRow, 5).Style.NumberFormat.Format = "#,##0 \"VNĐ\"";
                        ws.Cell(currentRow, 5).Style.Font.Bold = true;
                        ws.Cell(currentRow, 5).Style.Font.FontColor = PrimaryHeaderColor;
                        ws.Range(currentRow, 1, currentRow, 11).Style.Fill.BackgroundColor = XLColor.FromHtml("#E0E7FF");

                        // 5. Borders & Auto-fit
                        var dataRange = ws.Range(headerRow, 1, currentRow, 11);
                        dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                        dataRange.Style.Border.InsideBorderColor = XLColor.LightGray;
                        dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
                        dataRange.Style.Border.OutsideBorderColor = TableHeaderColor;

                        ws.Columns().AdjustToContents();

                        workbook.SaveAs(saveDialog.FileName);
                    }

                    var result = MessageBox.Show(
                        $"Xuất dữ liệu thành công ra tệp Excel (.xlsx):\r\n{saveDialog.FileName}\r\n\r\nBạn có muốn mở tệp ngay không?",
                        "Xuất Báo Cáo Thành Công",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information
                    );

                    if (result == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start(saveDialog.FileName);
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi khi xuất tệp Excel: " + ex.Message, "Lỗi Xuất File", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }

            return false;
        }
    }
}
