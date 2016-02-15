using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using WindowsADExplorer.Injection;
using WindowsADExplorer.Models;

namespace WindowsADExplorer
{
    public partial class MainWindow : Window
    {
        private readonly ExplorerModel model;

        public MainWindow()
        {
            InitializeComponent();
            var serviceLocator = this.FindResource<ModelServiceLocator>("serviceLocator");
            this.model = serviceLocator.ExplorerModel;
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

        private void windowRendered(object sender, EventArgs e)
        {
            if (!connect())
            {
                Close();
                return;
            }
            model.RetrieveGroups(txtGroupFilter.Text);
        }

        private bool connect()
        {
            ConnectionModel connectionModel = getConnectionDetails();
            while (connectionModel != null)
            {
                try
                {
                    model.OpenConnection(connectionModel);
                    return true;
                }
                catch (Exception exception)
                {
                    showErrorDialog(exception);
                }
                connectionModel = getConnectionDetails();
            }
            return false;
        }

        private ConnectionModel getConnectionDetails()
        {
            ConnectDialog dialog = new ConnectDialog();
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                return dialog.ConnectionModel;
            }
            else
            {
                return null;
            }
        }

        private void showErrorDialog(Exception exception)
        {
            ErrorDialog errorDialog = new ErrorDialog();
            errorDialog.Owner = this;
            errorDialog.Title = "Failed to Connect";
            errorDialog.ErrorDetails.ErrorMessage = exception.Message;
            errorDialog.ErrorDetails.StackTrace = exception.StackTrace;
            errorDialog.ShowDialog();
        }

        private void groupExpanded(object sender, RoutedEventArgs e)
        {
            TreeViewItem expandedItem = e.OriginalSource as TreeViewItem;
            if (expandedItem == null)
            {
                return;
            }
            GroupModel group = expandedItem.Header as GroupModel;
            if (group == null)
            {
                return;
            }
            model.RetrieveGroupMembers(group);
        }

        private void userExpanded(object sender, RoutedEventArgs e)
        {
            TreeViewItem expandedItem = e.OriginalSource as TreeViewItem;
            if (expandedItem == null)
            {
                return;
            }
            UserModel user = expandedItem.Header as UserModel;
            if (user == null)
            {
                return;
            }
            model.RetrieveUserGroups(user);
        }

        private void itemSelected(object server, RoutedEventArgs e)
        {
            TreeViewItem selectedItem = e.OriginalSource as TreeViewItem;
            if (selectedItem == null)
            {
                return;
            }
            UserModel user = selectedItem.Header as UserModel;
            if (user != null)
            {
                model.RetrieveUserProperties(user);
                return;
            }
            GroupModel group = selectedItem.Header as GroupModel;
            if (group != null)
            {
                model.RetrieveGroupProperties(group);
                return;
            }
        }

        private void tabChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!model.IsConnected)
            {
                return;
            }
            if (tabExplorer.SelectedItem == tabGroups)
            {
                model.RetrieveGroups(txtGroupFilter.Text);
            }
            else if (tabExplorer.SelectedItem == tabUsers)
            {
                model.RetrieveUsers(txtUserFilter.Text);
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            model.Cancel();
        }

        private void groupFilterTextChanged(object sender, TextChangedEventArgs e)
        {
            model.RetrieveGroups(txtGroupFilter.Text);
        }

        private void userFilterTextChanged(object sender, TextChangedEventArgs e)
        {
            model.RetrieveUsers(txtUserFilter.Text);
        }

        private void managerUsersClicked(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = e.Source as MenuItem;
            if (menuItem == null)
            {
                return;
            }
            GroupModel group = menuItem.DataContext as GroupModel;
            if (group == null)
            {
                return;
            }
            ManageUsersDialog dialog = new ManageUsersDialog();
            dialog.Owner = this;
            var serviceLocator = this.FindResource<ModelServiceLocator>("serviceLocator");
            model.ShareConnection(serviceLocator.AddUserModel);
            dialog.SetGroup(group);
            dialog.ShowDialog();
        }
    }
}
