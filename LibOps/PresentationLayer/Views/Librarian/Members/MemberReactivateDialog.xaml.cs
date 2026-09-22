using System.Windows;
using LibOps.PresentationLayer.ViewModels;

namespace LibOps.PresentationLayer.Views.Members
{
    public partial class MemberReactivateDialog : Window
    {
        public MemberReactivateDialog(MemberReactivateViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            viewModel.RequestClose += () =>
            {
                DialogResult = viewModel.IsSuccess;
                Close();
            };
        }
    }
}
