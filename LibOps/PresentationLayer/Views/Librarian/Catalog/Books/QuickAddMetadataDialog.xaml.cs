using System.Windows;
using LibOps.PresentationLayer.ViewModels;

namespace LibOps.PresentationLayer.Views
{
    /// <summary>
    /// Interaction logic for QuickAddMetadataDialog.xaml
    /// </summary>
    public partial class QuickAddMetadataDialog : Window
    {
        public QuickAddMetadataDialog()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is QuickAddMetadataViewModel oldVm)
            {
                oldVm.RequestClose -= HandleRequestClose;
            }

            if (e.NewValue is QuickAddMetadataViewModel newVm)
            {
                newVm.RequestClose += HandleRequestClose;
            }
        }

        private void HandleRequestClose()
        {
            DialogResult = true;
            Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
