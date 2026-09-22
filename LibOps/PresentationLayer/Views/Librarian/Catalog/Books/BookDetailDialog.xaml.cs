using System.Windows;
using LibOps.PresentationLayer.ViewModels;

namespace LibOps.PresentationLayer.Views
{
    public partial class BookDetailDialog : Window
    {
        public BookDetailDialog(BookDetailDialogViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            viewModel.RequestClose += () =>
            {
                DialogResult = viewModel.IsModified;
                Close();
            };
        }
    }
}
