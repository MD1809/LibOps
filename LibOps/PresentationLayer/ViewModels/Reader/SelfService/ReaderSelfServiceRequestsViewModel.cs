using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using LibOps.BusinessLogicLayer.BusinessServices;
using LibOps.DataModels.DataTransferObjects;
using LibOps.PresentationLayer.MvvmCore;
using LibOps.PresentationLayer.Services;
using LibOps.PresentationLayer.Views;

namespace LibOps.PresentationLayer.ViewModels
{
    /// <summary>
    /// ViewModel trung tâm theo dõi tiến trình và gửi các loại yêu cầu tự phục vụ trực tuyến của độc giả
    /// </summary>
    public class ReaderSelfServiceRequestsViewModel : ViewModelBase
    {
        private readonly ReaderPortalService _readerService;
        private readonly UserSessionDto _currentSession;

        private ReaderCardSummaryDto _memberCardSummary;
        private List<ReaderRequestDisplayDto> _allRequests = new List<ReaderRequestDisplayDto>();
        private string _selectedStatusFilter = "ALL";
        private bool _isLoading;

        public ReaderCardSummaryDto MemberCardSummary
        {
            get => _memberCardSummary;
            set => SetProperty(ref _memberCardSummary, value);
        }

        public string SelectedStatusFilter
        {
            get => _selectedStatusFilter;
            set
            {
                if (SetProperty(ref _selectedStatusFilter, value))
                {
                    ApplyFilter();
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public PaginationController<ReaderRequestDisplayDto> Paging { get; } = new PaginationController<ReaderRequestDisplayDto>(10);
        public ObservableCollection<ReaderRequestDisplayDto> MyRequests => Paging.CurrentPageItems;

        public ICommand CreateGeneralRequestCommand { get; }
        public ICommand RefreshCommand { get; }

        public ReaderSelfServiceRequestsViewModel(UserSessionDto session = null)
        {
            _currentSession = session ?? AuthService.CurrentSession;
            _readerService = new ReaderPortalService();

            CreateGeneralRequestCommand = new RelayCommand(ExecuteCreateGeneralRequest);
            RefreshCommand = new RelayCommand(LoadData);

            LoadData();
        }

        public void LoadData()
        {
            LoadMemberCardSummary();
            LoadMyRequests();
        }

        private void LoadMemberCardSummary()
        {
            try
            {
                if (_currentSession?.MemberId == null) return;
                MemberCardSummary = _readerService.GetCardSummary(_currentSession.MemberId.Value);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LoadMemberCardSummary Error]: {ex.Message}");
            }
        }

        private void LoadMyRequests()
        {
            try
            {
                IsLoading = true;
                if (_currentSession?.MemberId == null) return;
                _allRequests = _readerService.GetMyRequests(_currentSession.MemberId.Value) ?? new List<ReaderRequestDisplayDto>();
                ApplyFilter();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LoadMyRequests Error]: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ApplyFilter()
        {
            if (_allRequests == null)
            {
                Paging.SetSource(new List<ReaderRequestDisplayDto>());
                return;
            }

            var filtered = _allRequests.AsEnumerable();

            if (!string.IsNullOrEmpty(SelectedStatusFilter) && SelectedStatusFilter != "ALL")
            {
                filtered = filtered.Where(r => string.Equals(r.Status, SelectedStatusFilter, StringComparison.OrdinalIgnoreCase));
            }

            Paging.SetSource(filtered.ToList());
        }

        private void ExecuteCreateGeneralRequest()
        {
            if (_currentSession?.MemberId == null) return;

            string phone = _memberCardSummary?.PhoneNumber ?? string.Empty;
            string email = _memberCardSummary?.Email ?? string.Empty;
            string address = _memberCardSummary?.Address ?? string.Empty;

            var dialog = new CreateSelfServiceRequestDialog(phone, email, address);
            if (dialog.ShowDialog() != true)
            {
                return;
            }

            bool ok = false;
            string requestCode = string.Empty;
            string errorMsg = string.Empty;

            if (dialog.RequestType == "REISSUE_CARD")
            {
                ok = _readerService.SubmitReissueCardRequest(_currentSession.MemberId.Value, dialog.Reason, out requestCode, out errorMsg);
                if (ok)
                {
                    DialogService.ShowInformation(
                        $"Đã gửi yêu cầu báo mất & cấp lại thẻ #{requestCode} thành công!\n\nThẻ thư viện của bạn đã được TỰ ĐỘNG TẠM KHÓA để bảo vệ an toàn. Vui lòng liên hệ quầy thủ thư để nhận thẻ mới.",
                        "Gửi Yêu Cầu Thành Công");
                }
            }
            else if (dialog.RequestType == "UPDATE_INFO")
            {
                ok = _readerService.SubmitUpdateInfoRequest(
                    _currentSession.MemberId.Value,
                    dialog.NewPhone,
                    dialog.NewEmail,
                    dialog.NewAddress,
                    dialog.Reason,
                    out requestCode,
                    out errorMsg);

                if (ok)
                {
                    DialogService.ShowInformation(
                        $"Đã gửi yêu cầu cập nhật thông tin #{requestCode} thành công!\n\nThủ thư sẽ kiểm tra và cập nhật hồ sơ của bạn sớm nhất.",
                        "Gửi Yêu Cầu Thành Công");
                }
            }
            else if (dialog.RequestType == "FEEDBACK_INQUIRY")
            {
                ok = _readerService.SubmitFeedbackRequest(
                    _currentSession.MemberId.Value,
                    dialog.FeedbackSubject,
                    dialog.FeedbackContent,
                    out requestCode,
                    out errorMsg);

                if (ok)
                {
                    DialogService.ShowInformation(
                        $"Đã gửi góp ý / yêu cầu hỗ trợ #{requestCode} thành công!\n\nBan quản lý thư viện sẽ phản hồi trực tiếp cho bạn tại trang này.",
                        "Gửi Yêu Cầu Thành Công");
                }
            }

            if (ok)
            {
                LoadData();
            }
            else
            {
                DialogService.ShowError($"Gửi yêu cầu thất bại: {errorMsg}", "Thất Bại");
            }
        }
    }
}
