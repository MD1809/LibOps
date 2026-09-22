using System;
using System.Windows.Input;
using LibOps.BusinessLogicLayer.BusinessServices;
using LibOps.PresentationLayer.MvvmCore;

namespace LibOps.PresentationLayer.ViewModels
{
    public class SystemSettingsViewModel : ViewModelBase
    {
        private readonly SystemSettingsService _configService;

        private decimal _cardIssuanceFee;
        private decimal _defaultDeposit;
        private int _validityDays;
        private int _maxBorrowDays;
        private int _maxBooksPerMember;
        private decimal _finePerOverdueDay;
        private decimal _damagedRatePercent;
        private decimal _lostRatePercent;
        private int _maxRenewalCount;
        private int _renewalDays;

        // SMTP Settings Properties
        private string _smtpHost;
        private int _smtpPort;
        private bool _smtpEnableSsl;
        private string _smtpUsername;
        private string _smtpPassword;
        private string _smtpFromName;
        private bool _systemEmailNotificationEnabled;

        // Group Editing State Flags
        private bool _isEditingMembershipRules;
        private bool _isEditingBorrowRules;
        private bool _isEditingPenaltyRules;
        private bool _isEditingSmtpSettings;

        public bool IsEditingMembershipRules
        {
            get => _isEditingMembershipRules;
            set => SetProperty(ref _isEditingMembershipRules, value);
        }

        public bool IsEditingBorrowRules
        {
            get => _isEditingBorrowRules;
            set => SetProperty(ref _isEditingBorrowRules, value);
        }

        public bool IsEditingPenaltyRules
        {
            get => _isEditingPenaltyRules;
            set => SetProperty(ref _isEditingPenaltyRules, value);
        }

        public bool IsEditingSmtpSettings
        {
            get => _isEditingSmtpSettings;
            set => SetProperty(ref _isEditingSmtpSettings, value);
        }

        public decimal CardIssuanceFee
        {
            get => _cardIssuanceFee;
            set => SetProperty(ref _cardIssuanceFee, value);
        }

        public decimal DefaultDeposit
        {
            get => _defaultDeposit;
            set => SetProperty(ref _defaultDeposit, value);
        }

        public int ValidityDays
        {
            get => _validityDays;
            set => SetProperty(ref _validityDays, value);
        }

        public int MaxBorrowDays
        {
            get => _maxBorrowDays;
            set => SetProperty(ref _maxBorrowDays, value);
        }

        public int MaxBooksPerMember
        {
            get => _maxBooksPerMember;
            set => SetProperty(ref _maxBooksPerMember, value);
        }

        public decimal FinePerOverdueDay
        {
            get => _finePerOverdueDay;
            set => SetProperty(ref _finePerOverdueDay, value);
        }

        public decimal DamagedRatePercent
        {
            get => _damagedRatePercent;
            set => SetProperty(ref _damagedRatePercent, value);
        }

        public decimal LostRatePercent
        {
            get => _lostRatePercent;
            set => SetProperty(ref _lostRatePercent, value);
        }

        public int MaxRenewalCount
        {
            get => _maxRenewalCount;
            set => SetProperty(ref _maxRenewalCount, value);
        }

        public int RenewalDays
        {
            get => _renewalDays;
            set => SetProperty(ref _renewalDays, value);
        }

        public string SmtpHost
        {
            get => _smtpHost;
            set => SetProperty(ref _smtpHost, value);
        }

        public int SmtpPort
        {
            get => _smtpPort;
            set => SetProperty(ref _smtpPort, value);
        }

        public bool SmtpEnableSsl
        {
            get => _smtpEnableSsl;
            set => SetProperty(ref _smtpEnableSsl, value);
        }

        public string SmtpUsername
        {
            get => _smtpUsername;
            set => SetProperty(ref _smtpUsername, value);
        }

        public string SmtpPassword
        {
            get => _smtpPassword;
            set => SetProperty(ref _smtpPassword, value);
        }

        public string SmtpFromName
        {
            get => _smtpFromName;
            set => SetProperty(ref _smtpFromName, value);
        }

        public bool SystemEmailNotificationEnabled
        {
            get => _systemEmailNotificationEnabled;
            set => SetProperty(ref _systemEmailNotificationEnabled, value);
        }

        // Per-Group Commands
        public ICommand EditMembershipRulesCommand { get; }
        public ICommand CancelMembershipRulesCommand { get; }
        public ICommand SaveMembershipRulesCommand { get; }

        public ICommand EditBorrowRulesCommand { get; }
        public ICommand CancelBorrowRulesCommand { get; }
        public ICommand SaveBorrowRulesCommand { get; }

        public ICommand EditPenaltyRulesCommand { get; }
        public ICommand CancelPenaltyRulesCommand { get; }
        public ICommand SavePenaltyRulesCommand { get; }

        public ICommand EditSmtpSettingsCommand { get; }
        public ICommand CancelSmtpSettingsCommand { get; }
        public ICommand SaveSmtpSettingsCommand { get; }

        public SystemSettingsViewModel()
        {
            _configService = new SystemSettingsService();

            // Group 1: Membership
            EditMembershipRulesCommand = new RelayCommand(() => IsEditingMembershipRules = true);
            CancelMembershipRulesCommand = new RelayCommand(ExecuteCancelMembershipRules);
            SaveMembershipRulesCommand = new RelayCommand(ExecuteSaveMembershipRules);

            // Group 2: Borrow
            EditBorrowRulesCommand = new RelayCommand(() => IsEditingBorrowRules = true);
            CancelBorrowRulesCommand = new RelayCommand(ExecuteCancelBorrowRules);
            SaveBorrowRulesCommand = new RelayCommand(ExecuteSaveBorrowRules);

            // Group 3: Penalty
            EditPenaltyRulesCommand = new RelayCommand(() => IsEditingPenaltyRules = true);
            CancelPenaltyRulesCommand = new RelayCommand(ExecuteCancelPenaltyRules);
            SavePenaltyRulesCommand = new RelayCommand(ExecuteSavePenaltyRules);

            // Group 4: SMTP
            EditSmtpSettingsCommand = new RelayCommand(() => IsEditingSmtpSettings = true);
            CancelSmtpSettingsCommand = new RelayCommand(ExecuteCancelSmtpSettings);
            SaveSmtpSettingsCommand = new RelayCommand(ExecuteSaveSmtpSettings);

            LoadSettings();
        }

        public void LoadSettings()
        {
            CardIssuanceFee = _configService.GetCardIssuanceFee();
            DefaultDeposit = _configService.GetDefaultDeposit();
            ValidityDays = _configService.GetMemberValidityDays();
            MaxBorrowDays = _configService.GetMaxBorrowDays();
            MaxBooksPerMember = _configService.GetMaxBooksPerMember();
            FinePerOverdueDay = _configService.GetFinePerOverdueDay();
            DamagedRatePercent = _configService.GetDamagedBookFineRate() * 100.0m;
            LostRatePercent = _configService.GetLostBookFineRate() * 100.0m;
            MaxRenewalCount = _configService.GetMaxRenewalCount();
            RenewalDays = _configService.GetRenewalDays();

            SmtpHost = _configService.GetSmtpHost();
            SmtpPort = _configService.GetSmtpPort();
            SmtpEnableSsl = _configService.GetSmtpEnableSsl();
            SmtpUsername = _configService.GetSmtpUsername();
            SmtpPassword = _configService.GetSmtpPassword();
            SmtpFromName = _configService.GetSmtpFromName();
            SystemEmailNotificationEnabled = _configService.GetEmailNotificationEnabled();

            IsEditingMembershipRules = false;
            IsEditingBorrowRules = false;
            IsEditingPenaltyRules = false;
            IsEditingSmtpSettings = false;
        }

        private void ExecuteCancelMembershipRules()
        {
            DefaultDeposit = _configService.GetDefaultDeposit();
            CardIssuanceFee = _configService.GetCardIssuanceFee();
            ValidityDays = _configService.GetMemberValidityDays();
            IsEditingMembershipRules = false;
        }

        private void ExecuteSaveMembershipRules()
        {
            bool ok = _configService.SaveMembershipSettings(DefaultDeposit, ValidityDays, CardIssuanceFee, out string errorMsg);
            if (!ok)
            {
                DialogService.ShowError($"Lỗi lưu quy định thẻ độc giả: {errorMsg}");
                return;
            }

            IsEditingMembershipRules = false;
            DialogService.ShowInformation("Đã lưu quy định thẻ độc giả và phí dịch vụ thành công!");
        }

        private void ExecuteCancelBorrowRules()
        {
            MaxBorrowDays = _configService.GetMaxBorrowDays();
            MaxBooksPerMember = _configService.GetMaxBooksPerMember();
            MaxRenewalCount = _configService.GetMaxRenewalCount();
            RenewalDays = _configService.GetRenewalDays();
            IsEditingBorrowRules = false;
        }

        private void ExecuteSaveBorrowRules()
        {
            bool ok = _configService.SaveBorrowRules(MaxBorrowDays, MaxBooksPerMember, MaxRenewalCount, RenewalDays, out string errorMsg);
            if (!ok)
            {
                DialogService.ShowError($"Lỗi lưu quy định mượn trả: {errorMsg}");
                return;
            }

            IsEditingBorrowRules = false;
            DialogService.ShowInformation("Đã lưu quy định mượn trả và gia hạn sách thành công!");
        }

        private void ExecuteCancelPenaltyRules()
        {
            FinePerOverdueDay = _configService.GetFinePerOverdueDay();
            DamagedRatePercent = _configService.GetDamagedBookFineRate() * 100.0m;
            LostRatePercent = _configService.GetLostBookFineRate() * 100.0m;
            IsEditingPenaltyRules = false;
        }

        private void ExecuteSavePenaltyRules()
        {
            bool ok = _configService.SavePenaltyRules(FinePerOverdueDay, LostRatePercent, DamagedRatePercent, out string errorMsg);
            if (!ok)
            {
                DialogService.ShowError($"Lỗi lưu chế tài phạt & bồi thường: {errorMsg}");
                return;
            }

            IsEditingPenaltyRules = false;
            DialogService.ShowInformation("Đã lưu chế tài phạt quá hạn và bồi thường thành công!");
        }

        private void ExecuteCancelSmtpSettings()
        {
            SmtpHost = _configService.GetSmtpHost();
            SmtpPort = _configService.GetSmtpPort();
            SmtpEnableSsl = _configService.GetSmtpEnableSsl();
            SmtpUsername = _configService.GetSmtpUsername();
            SmtpPassword = _configService.GetSmtpPassword();
            SmtpFromName = _configService.GetSmtpFromName();
            SystemEmailNotificationEnabled = _configService.GetEmailNotificationEnabled();
            IsEditingSmtpSettings = false;
        }

        private void ExecuteSaveSmtpSettings()
        {
            bool ok = _configService.SaveSmtpSettings(
                SmtpHost,
                SmtpPort,
                SmtpEnableSsl,
                SmtpUsername,
                SmtpPassword,
                SmtpFromName,
                SystemEmailNotificationEnabled,
                out string errorSmtp);

            if (!ok)
            {
                DialogService.ShowError($"Lỗi lưu cấu hình SMTP: {errorSmtp}");
                return;
            }

            IsEditingSmtpSettings = false;
            DialogService.ShowInformation("Đã lưu cấu hình máy chủ Email (SMTP) thành công!");
        }
    }
}
