using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using WindowsADExplorer.Injection;
using WindowsADExplorer.Models;

namespace WindowsADExplorer
{
    public partial class ManageUsersDialog : Window
    {
        private readonly ManagerUsersModel model;

        public ManageUsersDialog()
        {
            InitializeComponent();
            var serviceLocator = this.FindResource<ModelServiceLocator>("serviceLocator");
            this.model = serviceLocator.AddUserModel;
        }

        public void SetGroup(GroupModel group)
        {
            model.SetGroup(group);
        }

        private void searchUserClicked(object sender, RoutedEventArgs e)
        {
            model.Search(txtSearch.Text);
        }

        private void removeUserClicked(object sender, RoutedEventArgs e)
        {
            Button button = e.Source as Button;
            if (button == null)
            {
                return;
            }
            UserModel user = button.DataContext as UserModel;
            if (user == null)
            {
                return;
            }
            ConfirmationDialog errorDialog = new ConfirmationDialog();
            errorDialog.Owner = this;
            errorDialog.Title = "Confirmation Required";
            errorDialog.ConfirmationDetails.Message = "Are you sure you want to remove this user?";
            if (errorDialog.ShowDialog() == true)
            {
                model.RemoveMember(user);
            }
        }

        private void windowClosing(object sender, CancelEventArgs e)
        {
            model.Cancel();
        }
    }
}
