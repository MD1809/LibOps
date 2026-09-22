using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;
using LibOps.BusinessLogicLayer.BusinessServices;
using LibOps.DataModels.DataTransferObjects;
using LibOps.PresentationLayer.MvvmCore;
using LiveCharts;
using LiveCharts.Wpf;

namespace LibOps.PresentationLayer.ViewModels
{
    public class DashboardOverviewViewModel : ViewModelBase
    {
        private readonly ReportService _reportService;
        private DashboardKpiSummaryDto _kpiSummary;
        private bool _isLoading;

        private SeriesCollection _trendSeriesCollection;
        private string[] _trendLabels;
        private Func<double, string> _yFormatter;

        public DashboardKpiSummaryDto KpiSummary
        {
            get => _kpiSummary;
            set => SetProperty(ref _kpiSummary, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public SeriesCollection TrendSeriesCollection
        {
            get => _trendSeriesCollection;
            set => SetProperty(ref _trendSeriesCollection, value);
        }

        public string[] TrendLabels
        {
            get => _trendLabels;
            set => SetProperty(ref _trendLabels, value);
        }

        public Func<double, string> YFormatter
        {
            get => _yFormatter;
            set => SetProperty(ref _yFormatter, value);
        }

        // Tầng 3 (Trái): Sách quá hạn cần thu hồi gấp
        public ObservableCollection<OverdueBorrowItemDto> OverdueBorrows { get; }

        // Tầng 3 (Phải): Top 5 đầu sách mượn nhiều nhất
        public ObservableCollection<TopBorrowedBookItemDto> TopBooks { get; }

        // Tầng 4 (Trái): Yêu cầu chờ duyệt từ độc giả
        public ObservableCollection<PendingReaderRequestItemDto> PendingRequests { get; }

        // Tầng 4 (Phải): Nhật ký hoạt động gần đây (Live Feed)
        public ObservableCollection<RecentActivityLogItemDto> RecentActivities { get; }

        // Tầng 5: Top độc giả năng nổ mượn sách nhiều nhất
        public ObservableCollection<TopActiveMemberItemDto> TopMembers { get; }

        public ICommand RefreshCommand { get; }

        public DashboardOverviewViewModel()
        {
            _reportService = new ReportService();
            
            OverdueBorrows = new ObservableCollection<OverdueBorrowItemDto>();
            TopBooks = new ObservableCollection<TopBorrowedBookItemDto>();
            PendingRequests = new ObservableCollection<PendingReaderRequestItemDto>();
            RecentActivities = new ObservableCollection<RecentActivityLogItemDto>();
            TopMembers = new ObservableCollection<TopActiveMemberItemDto>();

            YFormatter = value => value.ToString("N0");

            RefreshCommand = new RelayCommand(LoadDashboardData);

            LoadDashboardData();
        }

        public void LoadDashboardData()
        {
            try
            {
                IsLoading = true;

                // Tầng 1: 4 Thẻ KPI Tức thời
                KpiSummary = _reportService.GetDashboardKpiSummary() ?? new DashboardKpiSummaryDto();

                // Tầng 2: Xu hướng 7 ngày ĐỘNG từ CSDL (LiveCharts)
                var trends = _reportService.GetBorrowReturnTrend(7);
                if (trends != null && trends.Count > 0)
                {
                    var borrowValues = new ChartValues<int>(trends.Select(t => t.BorrowCount));
                    var returnValues = new ChartValues<int>(trends.Select(t => t.ReturnCount));

                    var indigoColor = (Color)ColorConverter.ConvertFromString("#4F46E5");
                    var pinkColor = (Color)ColorConverter.ConvertFromString("#EC4899");
                    var labelColor = (Color)ColorConverter.ConvertFromString("#334155");

                    TrendSeriesCollection = new SeriesCollection
                    {
                        new ColumnSeries
                        {
                            Title = "Lượt Mượn",
                            Values = borrowValues,
                            Fill = new SolidColorBrush(indigoColor),
                            MaxColumnWidth = 24,
                            DataLabels = true,
                            FontSize = 11,
                            Foreground = new SolidColorBrush(labelColor)
                        },
                        new ColumnSeries
                        {
                            Title = "Lượt Trả",
                            Values = returnValues,
                            Fill = new SolidColorBrush(pinkColor),
                            MaxColumnWidth = 24,
                            DataLabels = true,
                            FontSize = 11,
                            Foreground = new SolidColorBrush(labelColor)
                        }
                    };

                    TrendLabels = trends.Select(t => t.DayLabel).ToArray();
                }
                else
                {
                    TrendSeriesCollection = new SeriesCollection();
                    TrendLabels = new string[0];
                }

                // Tầng 3 (Trái): Quá hạn cần thu hồi
                OverdueBorrows.Clear();
                var overdues = _reportService.GetOverdueBorrowList(8);
                if (overdues != null)
                {
                    foreach (var item in overdues)
                    {
                        OverdueBorrows.Add(item);
                    }
                }

                // Tầng 3 (Phải): Top 5 sách
                TopBooks.Clear();
                var topBooks = _reportService.GetTopBorrowedBooks(5);
                if (topBooks != null)
                {
                    foreach (var item in topBooks)
                    {
                        TopBooks.Add(item);
                    }
                }

                // Tầng 4 (Trái): Yêu cầu chờ duyệt
                PendingRequests.Clear();
                var requests = _reportService.GetPendingReaderRequests(5);
                if (requests != null)
                {
                    foreach (var item in requests)
                    {
                        PendingRequests.Add(item);
                    }
                }

                // Tầng 4 (Phải): Nhật ký hoạt động
                RecentActivities.Clear();
                var activities = _reportService.GetRecentActivityLog(8);
                if (activities != null)
                {
                    foreach (var item in activities)
                    {
                        RecentActivities.Add(item);
                    }
                }

                // Tầng 5: Top độc giả
                TopMembers.Clear();
                var topMembers = _reportService.GetTopActiveMembers(5);
                if (topMembers != null)
                {
                    foreach (var item in topMembers)
                    {
                        TopMembers.Add(item);
                    }
                }
            }
            catch
            {
                // Fallback nếu có sự cố kết nối CSDL
                if (KpiSummary == null)
                {
                    KpiSummary = new DashboardKpiSummaryDto();
                }
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
