using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using LibOps.BusinessLogicLayer.BusinessServices;
using LibOps.DataModels.DataTransferObjects;
using LibOps.PresentationLayer.MvvmCore;

namespace LibOps.PresentationLayer.ViewModels
{
    public class ReturnBookProcessViewModel : ViewModelBase
    {
        private readonly FineCalculationService _fineService;
        private string _returnBarcode;

        private decimal _totalOverdueFine;
        private decimal _totalConditionFine;
        private decimal _grandTotalFine;

        private bool _isCashSelected = true;
        private bool _isDepositSelected;
        private bool _isDebtSelected;

        private bool _canDeductDeposit;
        private string _depositStatusText;

        public ObservableCollection<BulkReturnItemDto> ReturnCart { get; }

        public string ReturnBarcode
        {
            get => _returnBarcode;
            set => SetProperty(ref _returnBarcode, value);
        }

        public decimal TotalOverdueFine
        {
            get => _totalOverdueFine;
            set => SetProperty(ref _totalOverdueFine, value);
        }

        public decimal TotalConditionFine
        {
            get => _totalConditionFine;
            set => SetProperty(ref _totalConditionFine, value);
        }

        public decimal GrandTotalFine
        {
            get => _grandTotalFine;
            set => SetProperty(ref _grandTotalFine, value);
        }

        public bool IsCashSelected
        {
            get => _isCashSelected;
            set
            {
                if (SetProperty(ref _isCashSelected, value) && value)
                {
                    UpdateDepositStatusText();
                }
            }
        }

        public bool IsDepositSelected
        {
            get => _isDepositSelected;
            set
            {
                if (SetProperty(ref _isDepositSelected, value) && value)
                {
                    UpdateDepositStatusText();
                }
            }
        }

        public bool IsDebtSelected
        {
            get => _isDebtSelected;
            set
            {
                if (SetProperty(ref _isDebtSelected, value) && value)
                {
                    UpdateDepositStatusText();
                }
            }
        }

        public bool CanDeductDeposit
        {
            get => _canDeductDeposit;
            set => SetProperty(ref _canDeductDeposit, value);
        }

        public string DepositStatusText
        {
            get => _depositStatusText;
            set => SetProperty(ref _depositStatusText, value);
        }

        public ICommand AddBarcodeCommand { get; }
        public ICommand SearchByMemberCommand { get; }
        public ICommand RemoveItemCommand { get; }
        public ICommand ClearCartCommand { get; }
        public ICommand ConfirmReturnCommand { get; }

        public event Action<string> ShowMessageRequested;

        public ReturnBookProcessViewModel()
        {
            _fineService = new FineCalculationService();
            ReturnCart = new ObservableCollection<BulkReturnItemDto>();

            AddBarcodeCommand = new RelayCommand(ExecuteAddBarcode);
            SearchByMemberCommand = new RelayCommand(ExecuteSearchByMember);
            RemoveItemCommand = new RelayCommand<BulkReturnItemDto>(ExecuteRemoveItem);
            ClearCartCommand = new RelayCommand(ExecuteClearCart);
            ConfirmReturnCommand = new RelayCommand(ExecuteConfirmReturn, () => ReturnCart.Count > 0);

            UpdateCartSummary();
        }

        public void ExecuteAddBarcode()
        {
            if (string.IsNullOrWhiteSpace(ReturnBarcode)) return;

            string code = ReturnBarcode.Trim();
            if (ReturnCart.Any(x => x.Barcode.Equals(code, StringComparison.OrdinalIgnoreCase)))
            {
                ShowMessageRequested?.Invoke($"Cuốn sách '{code}' đã có trong giỏ trả!");
                return;
            }

            var lookup = _fineService.LookupBookForReturn(code, out string lookupError);
            if (lookup == null)
            {
                ShowMessageRequested?.Invoke(string.IsNullOrWhiteSpace(lookupError) ? $"Không tìm thấy thông tin lượt mượn cho mã vạch '{code}'." : lookupError);
                return;
            }

            var item = new BulkReturnItemDto
            {
                BorrowSlipDetailId = lookup.BorrowSlipDetailId,
                BorrowSlipId = lookup.BorrowSlipId,
                SlipCode = lookup.SlipCode,
                CopyId = lookup.CopyId,
                Barcode = lookup.Barcode,
                BookId = lookup.BookId,
                Title = lookup.Title,
                BookPrice = lookup.BookPrice,
                ShelfLocation = lookup.ShelfLocation,
                MemberId = lookup.MemberId,
                MemberCardCode = lookup.MemberCardCode,
                MemberFullName = lookup.MemberFullName,
                MemberPhone = lookup.MemberPhone,
                MemberDepositBalance = lookup.MemberDepositBalance,
                BorrowDate = lookup.BorrowDate,
                DueDate = lookup.DueDate,
                OverdueDays = lookup.OverdueDays,
                OverdueFine = lookup.OverdueFine,
                ConditionType = "AVAILABLE",
                ConditionFine = 0,
                ConditionNote = "Nguyên vẹn"
            };

            item.ConditionChanged += UpdateCartSummary;
            ReturnCart.Add(item);
            ReturnBarcode = string.Empty;
            UpdateCartSummary();
        }

        private void ExecuteSearchByMember()
        {
            var dialogVm = new SelectBorrowingBookViewModel();
            bool? result = DialogService.ShowSelectBorrowingBookDialog(dialogVm);
            if (result == true && dialogVm.SelectedBook != null)
            {
                var book = dialogVm.SelectedBook;
                if (ReturnCart.Any(x => x.Barcode.Equals(book.Barcode, StringComparison.OrdinalIgnoreCase)))
                {
                    ShowMessageRequested?.Invoke($"Cuốn sách '{book.Barcode}' ({book.Title}) đã có trong giỏ trả!");
                    return;
                }

                string conditionType = dialogVm.SelectedCondition ?? "AVAILABLE";
                decimal conditionFine = 0;
                string conditionNote = "Nguyên vẹn";

                if (conditionType == "LOST")
                {
                    conditionFine = book.BookPrice * 2.0m;
                    conditionNote = "Làm mất sách";
                }
                else if (conditionType == "DAMAGED")
                {
                    conditionFine = book.BookPrice * 0.5m;
                    conditionNote = "Hư hỏng";
                }

                var item = new BulkReturnItemDto
                {
                    BorrowSlipDetailId = book.BorrowSlipDetailId,
                    BorrowSlipId = book.BorrowSlipId,
                    SlipCode = book.SlipCode,
                    CopyId = book.CopyId,
                    Barcode = book.Barcode,
                    BookId = book.BookId,
                    Title = book.Title,
                    BookPrice = book.BookPrice,
                    ShelfLocation = book.ShelfLocation,
                    MemberId = book.MemberId,
                    MemberCardCode = book.MemberCardCode,
                    MemberFullName = book.MemberFullName,
                    MemberPhone = book.MemberPhone,
                    MemberDepositBalance = book.MemberDepositBalance,
                    BorrowDate = book.BorrowDate,
                    DueDate = book.DueDate,
                    OverdueDays = book.OverdueDays,
                    OverdueFine = book.OverdueFine,
                    ConditionType = conditionType,
                    ConditionFine = conditionFine,
                    ConditionNote = conditionNote
                };

                item.ConditionChanged += UpdateCartSummary;
                ReturnCart.Add(item);
                UpdateCartSummary();
            }
        }

        private void ExecuteRemoveItem(BulkReturnItemDto item)
        {
            if (item != null)
            {
                item.ConditionChanged -= UpdateCartSummary;
                ReturnCart.Remove(item);
                UpdateCartSummary();
            }
        }

        private void ExecuteClearCart()
        {
            foreach (var item in ReturnCart)
            {
                item.ConditionChanged -= UpdateCartSummary;
            }
            ReturnCart.Clear();
            UpdateCartSummary();
        }

        public void UpdateCartSummary()
        {
            TotalOverdueFine = ReturnCart.Sum(x => x.OverdueFine);
            TotalConditionFine = ReturnCart.Sum(x => x.ConditionFine);
            GrandTotalFine = TotalOverdueFine + TotalConditionFine;

            // Kiểm tra khả năng khấu trừ cọc
            if (ReturnCart.Count == 0)
            {
                CanDeductDeposit = false;
                DepositStatusText = "(Chưa có sách trong giỏ trả)";
                return;
            }

            var groups = ReturnCart.GroupBy(x => x.MemberId).ToList();
            if (groups.Count == 1)
            {
                var group = groups[0];
                var member = group.First();
                decimal memberDeposit = member.MemberDepositBalance;
                decimal groupPayable = group.Sum(x => x.TotalFine);

                var memberRepo = new LibOps.DataAccessLayer.DataRepositories.MemberRepository();
                var configService = new SystemSettingsService();
                int currentBorrowing = memberRepo.GetCurrentlyBorrowingCount(member.MemberId);
                int returnedCount = group.Count();
                int remainingBorrowCount = Math.Max(0, currentBorrowing - returnedCount);
                decimal minDeposit = configService.GetDefaultDeposit();
                decimal spendableDeposit = (remainingBorrowCount > 0) ? Math.Max(0, memberDeposit - minDeposit) : memberDeposit;

                CanDeductDeposit = spendableDeposit >= groupPayable;
                if (!CanDeductDeposit && IsDepositSelected)
                {
                    IsCashSelected = true;
                }
            }
            else
            {
                CanDeductDeposit = false;
                if (IsDepositSelected) IsCashSelected = true;
            }

            UpdateDepositStatusText();
            ((RelayCommand)ConfirmReturnCommand)?.RaiseCanExecuteChanged();
        }

        private void UpdateDepositStatusText()
        {
            if (ReturnCart.Count == 0)
            {
                DepositStatusText = "(Chưa có sách trong giỏ trả)";
                return;
            }

            var groups = ReturnCart.GroupBy(x => x.MemberId).ToList();
            if (groups.Count == 1)
            {
                var member = groups[0].First();
                decimal deposit = member.MemberDepositBalance;
                decimal payable = groups[0].Sum(x => x.TotalFine);

                var memberRepo = new LibOps.DataAccessLayer.DataRepositories.MemberRepository();
                var configService = new SystemSettingsService();
                int currentBorrowing = memberRepo.GetCurrentlyBorrowingCount(member.MemberId);
                int returnedCount = groups[0].Count();
                int remainingBorrowCount = Math.Max(0, currentBorrowing - returnedCount);
                decimal minDeposit = configService.GetDefaultDeposit();
                decimal spendableDeposit = (remainingBorrowCount > 0) ? Math.Max(0, deposit - minDeposit) : deposit;

                if (IsCashSelected)
                {
                    if (remainingBorrowCount > 0)
                    {
                        DepositStatusText = $"(Độc giả: {member.MemberFullName} - Cọc: {deposit:N0} đ, giữ tối thiểu {minDeposit:N0} đ cho {remainingBorrowCount} sách còn mượn | Nộp tiền mặt/QR)";
                    }
                    else
                    {
                        DepositStatusText = $"(Độc giả: {member.MemberFullName} - Cọc: {deposit:N0} đ | Nộp tiền mặt/QR, số dư cọc giữ nguyên)";
                    }
                }
                else if (IsDepositSelected)
                {
                    decimal remain = deposit - payable;
                    if (remainingBorrowCount > 0)
                    {
                        DepositStatusText = $"(Độc giả: {member.MemberFullName} | Cọc khả dụng trừ: {spendableDeposit:N0} đ, giữ {minDeposit:N0} đ thế chân | Sau trừ còn: {remain:N0} VNĐ)";
                    }
                    else
                    {
                        DepositStatusText = $"(Độc giả: {member.MemberFullName} | Số dư cọc: {deposit:N0} đ | Sau khi trừ: {remain:N0} VNĐ)";
                    }
                }
                else if (IsDebtSelected)
                {
                    DepositStatusText = $"GHI NHẬN NỢ: Tài khoản sẽ ghi nợ +{payable:N0} đ và tạm khóa mượn sách mới.";
                }
            }
            else
            {
                DepositStatusText = "(Giỏ trả chứa sách của nhiều độc giả khác nhau -> Chỉ hỗ trợ nộp tiền mặt)";
            }
        }

        public void ExecuteConfirmReturn()
        {
            if (ReturnCart.Count == 0) return;

            int userId = AuthService.CurrentSession?.UserId ?? 1;
            string paymentMethod = IsDepositSelected ? "DEPOSIT_DEDUCTION" : (IsDebtSelected ? "DEBT" : "CASH");

            bool ok = _fineService.ProcessBulkReturnBooks(
                ReturnCart.ToList(),
                userId,
                paymentMethod,
                out string receiptCode,
                out string errorMessage);

            if (ok)
            {
                ShowMessageRequested?.Invoke($"Xác nhận trả {ReturnCart.Count} cuốn sách thành công!");
                ReturnCart.Clear();
                UpdateCartSummary();
            }
            else
            {
                ShowMessageRequested?.Invoke("Lỗi tiếp nhận trả sách: " + errorMessage);
            }
        }
    }
}
