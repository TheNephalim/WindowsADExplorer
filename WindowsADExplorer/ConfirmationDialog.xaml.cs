using System.Windows;
using WindowsADExplorer.Injection;
using WindowsADExplorer.Models;

namespace WindowsADExplorer
{
    public partial class ConfirmationDialog : Window
    {
        private readonly ConfirmationModel model;

        public ConfirmationDialog()
        {
            InitializeComponent();
            ModelServiceLocator serviceLocator = this.FindResource<ModelServiceLocator>("serviceLocator");
            this.model = serviceLocator.ConfirmationModel;
        }

        public ConfirmationModel ConfirmationDetails
        {
            get { return model; }
        }

        private void okayClicked(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
