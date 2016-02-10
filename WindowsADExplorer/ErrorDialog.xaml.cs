using System;
using System.Windows;
using WindowsADExplorer.Injection;
using WindowsADExplorer.Models;

namespace WindowsADExplorer
{
    public partial class ErrorDialog : Window
    {
        private readonly ErrorModel model;

        public ErrorDialog()
        {
            InitializeComponent();
            ModelServiceLocator serviceLocator = this.FindResource<ModelServiceLocator>("serviceLocator");
            this.model = serviceLocator.ErrorModel;
        }

        public ErrorModel ErrorDetails
        {
            get { return model; }
        }

        private void okayClicked(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
