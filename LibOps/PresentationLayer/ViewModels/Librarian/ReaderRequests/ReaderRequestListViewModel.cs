using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Windows.Input;
using LibOps.BusinessLogicLayer.BusinessServices;
using LibOps.DataModels.DataTransferObjects;
using LibOps.PresentationLayer.MvvmCore;
using LibOps.PresentationLayer.Services;
using LibOps.PresentationLayer.Views;

namespace LibOps.PresentationLayer.ViewModels
{
    public class ReaderRequestListViewModel : ViewModelBase
    {
        private readonly ReaderRequestService _requestService;
        private List<ReaderRequestDisplayDto> _allRawRequests = new List<ReaderRequestDisplayDto>();

        private string _searchKeyword;
        private string _selectedStatusFilter = "ALL";
        private string _selectedTypeFilter = "ALL";
        private ReaderRequestDisplayDto _selectedRequest;
        private bool _isLoading;

        // KPI Counters
        private int _pendingCount;
        private int _refundCount;
        private int _reissueCount;
        private int _approvedCount;

        public int PendingCount
        {
            get => _pendingCount;
            set => SetProperty(ref _pendingCount, value);
        }

        public int RefundCount
        {
            get => _refundCount;
            set => SetProperty(ref _refundCount, value);
        }

        public int ReissueCount
        {
            get => _reissueCount;
            set => SetProperty(ref _reissueCount, value);
        }

        public int ApprovedCount
        {
            get => _approvedCount;
            set => SetProperty(ref _approvedCount, value);
        }

        public PaginationController<ReaderRequestDisplayDto> Paging { get; } = new PaginationController<ReaderRequestDisplayDto>(10);
        public ObservableCollection<ReaderRequestDisplayDto> Requests => Paging.CurrentPageItems;

        public string SearchKeyword
        {
            get => _searchKeyword;
            set
            {
                if (SetProperty(ref _searchKeyword, value))
                {
                    ApplyFilters();
                }
            }
        }

        public string SelectedStatusFilter
        {
            get => _selectedStatusFilter;
            set
            {
                if (SetProperty(ref _selectedStatusFilter, value))
                {
                    ApplyFilters();
                }
            }
        }

        public string SelectedTypeFilter
        {
            get => _selectedTypeFilter;
            set
            {
                if (SetProperty(ref _selectedTypeFilter, value))
                {
                    ApplyFilters();
                }
            }
        }

        public ReaderRequestDisplayDto SelectedRequest
        {
            get => _selectedRequest;
            set => SetProperty(ref _selectedRequest, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ICommand RefreshCommand { get; }
        public ICommand ViewDetailCommand { get; }

        public ReaderRequestListViewModel()
        {
            _requestService = new ReaderRequestService();

            RefreshCommand = new RelayCommand(LoadRequests);
            ViewDetailCommand = new RelayCommand<ReaderRequestDisplayDto>(ExecuteViewDetail);

            LoadRequests();
        }

        public void LoadRequests()
        {
            try
            {
                IsLoading = true;
                _allRawRequests = _requestService.GetAllReaderRequests(null, null, null) ?? new List<ReaderRequestDisplayDto>();

                // Tính toán KPI
                PendingCount = _allRawRequests.Count(r => string.Equals(r.Status, LibOps.DataModels.Enums.ReaderRequestStatusEnum.PENDING, StringComparison.OrdinalIgnoreCase));
                RefundCount = _allRawRequests.Count(r => r.RequestType == LibOps.DataModels.Enums.ReaderRequestTypeEnum.CANCEL_CARD && string.Equals(r.Status, LibOps.DataModels.Enums.ReaderRequestStatusEnum.PENDING, StringComparison.OrdinalIgnoreCase));
                ReissueCount = _allRawRequests.Count(r => r.RequestType == LibOps.DataModels.Enums.ReaderRequestTypeEnum.REISSUE_CARD && string.Equals(r.Status, LibOps.DataModels.Enums.ReaderRequestStatusEnum.PENDING, StringComparison.OrdinalIgnoreCase));
                ApprovedCount = _allRawRequests.Count(r => string.Equals(r.Status, LibOps.DataModels.Enums.ReaderRequestStatusEnum.APPROVED, StringComparison.OrdinalIgnoreCase));

                ApplyFilters();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LoadRequests Error]: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ApplyFilters()
        {
            if (_allRawRequests == null)
            {
                Paging.SetSource(new List<ReaderRequestDisplayDto>());
                return;
            }

            var filtered = _allRawRequests.AsEnumerable();

            // 1. Lọc theo trạng thái
            if (!string.IsNullOrEmpty(SelectedStatusFilter) && SelectedStatusFilter != "ALL")
            {
                filtered = filtered.Where(r => string.Equals(r.Status, SelectedStatusFilter, StringComparison.OrdinalIgnoreCase));
            }

            // 2. Lọc theo loại yêu cầu
            if (!string.IsNullOrEmpty(SelectedTypeFilter) && SelectedTypeFilter != "ALL")
            {
                filtered = filtered.Where(r => string.Equals(r.RequestType, SelectedTypeFilter, StringComparison.OrdinalIgnoreCase));
            }

            // 3. Lọc theo từ khóa tìm kiếm
            if (!string.IsNullOrWhiteSpace(SearchKeyword))
            {
                string kw = SearchKeyword.Trim().ToLowerInvariant();
                filtered = filtered.Where(r => 
                    (r.RequestCode != null && r.RequestCode.ToLowerInvariant().Contains(kw)) ||
                    (r.MemberCardCode != null && r.MemberCardCode.ToLowerInvariant().Contains(kw)) ||
                    (r.MemberFullName != null && r.MemberFullName.ToLowerInvariant().Contains(kw)) ||
                    (r.Reason != null && r.Reason.ToLowerInvariant().Contains(kw)));
            }

            Paging.SetSource(filtered.ToList());
        }

        private void ExecuteViewDetail(ReaderRequestDisplayDto req)
        {
            var target = req ?? SelectedRequest;
            if (target == null) return;

            var dialog = new RequestDetailDialog(target);
            if (dialog.ShowDialog() == true)
            {
                int userId = AuthService.CurrentSession?.UserId ?? 1;

                if (dialog.IsApproved)
                {
                    if (target.RequestType == LibOps.DataModels.Enums.ReaderRequestTypeEnum.CANCEL_CARD)
                    {
                        bool ok = _requestService.ApproveRefundRequest(
                            target.RequestId,
                            userId,
                            dialog.StaffNotes,
                            out Bitmap receiptBmp,
                            out string errorMsg);

                        if (ok)
                        {
                            receiptBmp?.Dispose();
                            DialogService.ShowInformation(
                                $"Phê duyệt và giải ngân yêu cầu #{target.RequestCode} thành công!\nSố tiền: {target.Amount:N0} VNĐ ({target.PayoutMethodDisplay})\nĐã đóng thẻ và cập nhật sổ quỹ.",
                                "Thành Công");
                            LoadRequests();
                        }
                        else
                        {
                            DialogService.ShowError($"Lỗi phê duyệt: {errorMsg}", "Thất Bại");
                        }
                    }
                    else
                    {
                        bool ok = _requestService.ApproveGeneralRequest(
                            target.RequestId,
                            userId,
                            dialog.StaffNotes,
                            out string errorMsg);

                        if (ok)
                        {
                            DialogService.ShowInformation(
                                $"Phê duyệt yêu cầu #{target.RequestCode} thành công!\nĐã cập nhật hệ thống và lưu phản hồi của thủ thư.",
                                "Thành Công");
                            LoadRequests();
                        }
                        else
                        {
                            DialogService.ShowError($"Lỗi phê duyệt: {errorMsg}", "Thất Bại");
                        }
                    }
                }
                else if (dialog.IsRejected)
                {
                    bool ok = _requestService.RejectRequest(target.RequestId, userId, dialog.StaffNotes, out string errorMsg);
                    if (ok)
                    {
                        DialogService.ShowInformation($"Đã từ chối yêu cầu #{target.RequestCode}.", "Đã Từ Chối");
                        LoadRequests();
                    }
                    else
                    {
                        DialogService.ShowError($"Lỗi từ chối: {errorMsg}", "Thất Bại");
                    }
                }
            }
        }
    }
}
