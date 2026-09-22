using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using LibOps.BusinessLogicLayer.BusinessServices;
using LibOps.DataModels.DataTransferObjects;
using LibOps.DataModels.Entities;
using LibOps.PresentationLayer.MvvmCore;

namespace LibOps.PresentationLayer.ViewModels
{
    public class CreateBorrowSlipViewModel : ViewModelBase
    {
        private readonly BorrowReturnService _borrowService;
        private readonly BookService _bookService;

        private string _memberCardCode;
        private MemberEntity _currentMember;
        private int _currentBorrowCount;
        private bool _isMemberEligible;
        private string _eligibilityMessage;

        private string _bookBarcode;
        private string _borrowNotes;

        public ObservableCollection<BorrowItemDisplayDto> CartItems { get; }

        public string MemberCardCode
        {
            get => _memberCardCode;
            set
            {
                if (SetProperty(ref _memberCardCode, value))
                {
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        CurrentMember = null;
                        IsMemberEligible = false;
                        EligibilityMessage = string.Empty;
                    }
                }
            }
        }

        public MemberEntity CurrentMember
        {
            get => _currentMember;
            set
            {
                if (SetProperty(ref _currentMember, value))
                {
                    OnPropertyChanged(nameof(HasCurrentMember));
                    OnPropertyChanged(nameof(MemberCardCodeDisplay));
                    OnPropertyChanged(nameof(MemberFullNameDisplay));
                    OnPropertyChanged(nameof(MemberPhoneDisplay));
                    OnPropertyChanged(nameof(MemberSummaryDisplay));
                    OnPropertyChanged(nameof(DepositBalanceDisplay));
                    OnPropertyChanged(nameof(TotalDebtDisplay));
                }
            }
        }

        public bool HasCurrentMember => CurrentMember != null;
        public string MemberCardCodeDisplay => CurrentMember != null ? CurrentMember.MemberCardCode : string.Empty;
        public string MemberFullNameDisplay => CurrentMember != null ? CurrentMember.FullName : string.Empty;
        public string MemberPhoneDisplay => CurrentMember != null ? CurrentMember.PhoneNumber : string.Empty;

        public int CurrentBorrowCount
        {
            get => _currentBorrowCount;
            set => SetProperty(ref _currentBorrowCount, value);
        }

        public bool IsMemberEligible
        {
            get => _isMemberEligible;
            set => SetProperty(ref _isMemberEligible, value);
        }

        public string EligibilityMessage
        {
            get => _eligibilityMessage;
            set
            {
                if (SetProperty(ref _eligibilityMessage, value))
                {
                    OnPropertyChanged(nameof(MemberSummaryDisplay));
                }
            }
        }

        public string MemberSummaryDisplay => CurrentMember != null
            ? $"{CurrentMember.MemberCardCode} - {CurrentMember.FullName} | SĐT: {CurrentMember.PhoneNumber}"
            : "Chưa chọn độc giả";

        public string DepositBalanceDisplay => CurrentMember != null ? $"{CurrentMember.DepositBalance:N0} VNĐ" : "0 VNĐ";
        public string TotalDebtDisplay => CurrentMember != null ? $"{CurrentMember.TotalDebt:N0} VNĐ" : "0 VNĐ";

        public string BookBarcode
        {
            get => _bookBarcode;
            set => SetProperty(ref _bookBarcode, value);
        }

        public string BorrowNotes
        {
            get => _borrowNotes;
            set => SetProperty(ref _borrowNotes, value);
        }

        private string _eligibilityReason = string.Empty;

        public string EligibilityReason
        {
            get => _eligibilityReason;
            set => SetProperty(ref _eligibilityReason, value);
        }

        public ICommand CheckMemberCommand { get; }
        public ICommand AddBookCommand { get; }
        public ICommand RemoveItemCommand { get; }
        public ICommand ConfirmBorrowCommand { get; }

        public event Action<string> ShowMessageRequested;

        public CreateBorrowSlipViewModel()
        {
            _borrowService = new BorrowReturnService();
            _bookService = new BookService();

            CartItems = new ObservableCollection<BorrowItemDisplayDto>();

            CheckMemberCommand = new RelayCommand(ExecuteCheckMember);
            AddBookCommand = new RelayCommand(ExecuteAddBook, () => IsMemberEligible);
            RemoveItemCommand = new RelayCommand<BorrowItemDisplayDto>(ExecuteRemoveItem);
            ConfirmBorrowCommand = new RelayCommand(ExecuteConfirmBorrow, () => IsMemberEligible && CartItems.Count > 0);
        }

        public void ExecuteCheckMember()
        {
            if (string.IsNullOrWhiteSpace(MemberCardCode))
            {
                EligibilityMessage = "Không đủ điều kiện";
                EligibilityReason = "Vui lòng nhập mã thẻ, SĐT hoặc CCCD độc giả.";
                IsMemberEligible = false;
                CurrentMember = null;
                return;
            }

            bool ok = _borrowService.ValidateBorrowEligibility(
                MemberCardCode.Trim(),
                CartItems.Count,
                out string errorMsg,
                out MemberEntity member,
                out int curCount);

            CurrentMember = member;
            CurrentBorrowCount = curCount;
            IsMemberEligible = ok;
            EligibilityMessage = ok ? "Đủ điều kiện mượn" : "Không đủ điều kiện";
            EligibilityReason = ok ? "Độc giả đáp ứng đầy đủ điều kiện mượn sách." : errorMsg;
        }

        public void ExecuteAddBook()
        {
            if (string.IsNullOrWhiteSpace(BookBarcode)) return;

            string code = BookBarcode.Trim();
            if (CartItems.Any(x => x.Barcode.Equals(code, StringComparison.OrdinalIgnoreCase)))
            {
                ShowMessageRequested?.Invoke("Cuốn sách này đã có trong danh sách chọn mượn!");
                return;
            }

            if (CartItems.Count + CurrentBorrowCount >= 3)
            {
                ShowMessageRequested?.Invoke("Độc giả đã đạt hạn mức mượn tối đa 3 cuốn!");
                return;
            }

            var item = _borrowService.GetBookCopyForBorrow(code, out string errMsg);
            if (item == null)
            {
                ShowMessageRequested?.Invoke(string.IsNullOrWhiteSpace(errMsg) ? $"Không tìm thấy bản sao sách với mã vạch '{code}'!" : errMsg);
                return;
            }

            item.SequenceNumber = CartItems.Count + 1;
            CartItems.Add(item);
            BookBarcode = string.Empty;
        }

        private void ExecuteRemoveItem(BorrowItemDisplayDto item)
        {
            if (item != null)
            {
                CartItems.Remove(item);
                for (int i = 0; i < CartItems.Count; i++)
                {
                    CartItems[i].SequenceNumber = i + 1;
                }
            }
        }

        public void ExecuteConfirmBorrow()
        {
            if (CurrentMember == null || CartItems.Count == 0) return;

            int userId = AuthService.CurrentSession?.UserId ?? 1;
            DateTime borrowDate = DateTime.Now;
            DateTime dueDate = DateTime.Today.AddDays(BorrowReturnService.DEFAULT_BORROW_DAYS);

            bool ok = _borrowService.CreateBorrowSlip(
                CurrentMember.MemberId,
                userId,
                borrowDate,
                dueDate,
                CartItems.ToList(),
                BorrowNotes,
                out string slipCode,
                out string errorMessage);

            if (ok)
            {
                ShowMessageRequested?.Invoke($"Lập phiếu mượn '{slipCode}' thành công cho độc giả {CurrentMember.FullName}!");
                CartItems.Clear();
                MemberCardCode = string.Empty;
                CurrentMember = null;
                IsMemberEligible = false;
                EligibilityMessage = string.Empty;
            }
            else
            {
                ShowMessageRequested?.Invoke("Lỗi lập phiếu mượn: " + errorMessage);
            }
        }
    }
}
