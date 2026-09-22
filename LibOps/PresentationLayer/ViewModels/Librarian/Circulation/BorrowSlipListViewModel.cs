using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using LibOps.BusinessLogicLayer.BusinessServices;
using LibOps.DataModels.DataTransferObjects;
using LibOps.PresentationLayer.MvvmCore;

namespace LibOps.PresentationLayer.ViewModels
{
    public class BorrowSlipListViewModel : ViewModelBase
    {
        private readonly BorrowReturnService _borrowService;
        private string _searchKeyword;
        private string _selectedStatusFilter = "ALL";
        private BorrowSlipGridDisplayDto _selectedSlip;
        private bool _isLoading;

        public PaginationController<BorrowSlipGridDisplayDto> Paging { get; } = new PaginationController<BorrowSlipGridDisplayDto>(10);
        public ObservableCollection<BorrowSlipGridDisplayDto> BorrowSlips => Paging.CurrentPageItems;
        public ObservableCollection<BorrowSlipItemDetailDto> SelectedSlipItems { get; }

        public string SearchKeyword
        {
            get => _searchKeyword;
            set
            {
                if (SetProperty(ref _searchKeyword, value))
                {
                    LoadSlips();
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
                    LoadSlips();
                }
            }
        }

        public BorrowSlipGridDisplayDto SelectedSlip
        {
            get => _selectedSlip;
            set
            {
                if (SetProperty(ref _selectedSlip, value))
                {
                    LoadSlipDetails();
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ICommand RefreshCommand { get; }
        public ICommand RenewSlipCommand { get; }

        public BorrowSlipListViewModel()
        {
            _borrowService = new BorrowReturnService();
            SelectedSlipItems = new ObservableCollection<BorrowSlipItemDetailDto>();

            RefreshCommand = new RelayCommand(LoadSlips);
            RenewSlipCommand = new RelayCommand(ExecuteRenewSlip, () => SelectedSlip != null);

            LoadSlips();
        }

        public void LoadSlips()
        {
            try
            {
                IsLoading = true;
                SelectedSlipItems.Clear();

                string status = SelectedStatusFilter == "ALL" ? null : SelectedStatusFilter;
                var list = _borrowService.GetBorrowSlips(SearchKeyword, status, null, null);
                Paging.SetSource(list);
            }
            catch
            {
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void LoadSlipDetails()
        {
            SelectedSlipItems.Clear();
            if (SelectedSlip == null) return;

            try
            {
                var items = _borrowService.GetBorrowSlipItems(SelectedSlip.BorrowSlipId);
                if (items != null)
                {
                    foreach (var item in items)
                    {
                        SelectedSlipItems.Add(item);
                    }
                }
            }
            catch
            {
            }
        }

        private void ExecuteRenewSlip()
        {
            if (SelectedSlip == null) return;

            bool ok = _borrowService.RenewBorrowSlip(SelectedSlip.BorrowSlipId, out string errorMsg, out DateTime newDueDate);
            if (ok)
            {
                DialogService.ShowInformation($"Gia hạn phiếu mượn '{SelectedSlip.SlipCode}' thành công!\r\nHạn trả mới: {newDueDate:dd/MM/yyyy}", "Gia Hạn Thành Công");
                LoadSlips();
            }
            else
            {
                DialogService.ShowWarning($"Không thể gia hạn: {errorMsg}", "Lỗi Gia Hạn");
            }
        }
    }
}
