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
        private readonly ManageUsersModel model;

        public ManageUsersDialog()
        {
            InitializeComponent();
            var serviceLocator = this.FindResource<ModelServiceLocator>("serviceLocator");
            this.model = serviceLocator.AddUserModel;
            this.model.ErrorOccurred += errorOccurred;
        }

        private void errorOccurred(object sender, Exception e)
        {
            ErrorDialog dialog = new ErrorDialog();
            dialog.Owner = this;
            dialog.ErrorDetails.ErrorMessage = e.Message;
            dialog.ErrorDetails.StackTrace = e.StackTrace;
            dialog.ShowDialog();
        }

        public void SetGroup(GroupModel group)
        {
            model.SetGroup(group);
        }

        private void searchUserClicked(object sender, RoutedEventArgs e)
        {
            model.Search(txtSearch.Text);
        }

        private void addUserClicked(object sender, RoutedEventArgs e)
        {
            Button button = e.Source as Button;
            if (button == null)
            {
                return;
            }
            GroupMemberModel user = button.DataContext as GroupMemberModel;
            if (user == null)
            {
                return;
            }
            model.AddMember(user);
        }

        private void removeUserClicked(object sender, RoutedEventArgs e)
        {
            Button button = e.Source as Button;
            if (button == null)
            {
                return;
            }
            GroupMemberModel user = button.DataContext as GroupMemberModel;
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
