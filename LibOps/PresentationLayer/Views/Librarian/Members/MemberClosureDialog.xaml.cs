using System.Windows;
using LibOps.PresentationLayer.ViewModels;
using LibOps.PresentationLayer.Views.Members;

namespace LibOps.PresentationLayer.Views
{
    public partial class MemberClosureDialog : Window
    {
        public MemberClosureDialog(MemberClosureViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            viewModel.RequestClose += () =>
            {
                var owner = this.Owner;
                DialogResult = true;
                Close();

                if (viewModel.IsSuccess && viewModel.IsAutoPrintVoucher && viewModel.GeneratedVoucherBitmap != null)
                {
                    var voucherDialog = new ExpenseVoucherDialog(viewModel.GeneratedVoucherBitmap);
                    if (owner != null)
                    {
                        voucherDialog.Owner = owner;
                    }
                    voucherDialog.ShowDialog();
                }
            };
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
