using System;
using System.Windows;
using WindowsADExplorer.Models;

namespace WindowsADExplorer
{
    public partial class ConnectDialog : Window
    {
        public string DomainName { get; set; }

        public string UserName { get; set; }

        public ConnectDialog()
        {
            InitializeComponent();
        }

        public ConnectionModel ConnectionModel
        {
            get 
            {
                ConnectionModel result = new ConnectionModel();
                result.DomainName = String.IsNullOrWhiteSpace(DomainName) ? null : DomainName;
                result.UserName = String.IsNullOrWhiteSpace(UserName) ? null : UserName;
                result.Password = String.IsNullOrWhiteSpace(txtPassword.Password) ? null : txtPassword.Password;
                return result; 
            }
        }

        private void okayClicked(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
