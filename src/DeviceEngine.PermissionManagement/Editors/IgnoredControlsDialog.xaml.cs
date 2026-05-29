using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DeviceEngine.PermissionManagement.Editors
{
    public partial class IgnoredControlsDialog : Window
    {
        private List<string> _ignoredControls;

        public IgnoredControlsDialog(List<string> ignoredControls)
        {
            InitializeComponent();
            _ignoredControls = new List<string>(ignoredControls ?? new List<string>());
            lstIgnored.ItemsSource = _ignoredControls;
        }

        public List<string> Result => _ignoredControls;

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            string name = txtNewControl.Text?.Trim();
            if (!string.IsNullOrEmpty(name) && !_ignoredControls.Contains(name))
            {
                _ignoredControls.Add(name);
                lstIgnored.Items.Refresh();
                txtNewControl.Clear();
            }
        }

        private void btnRemove_Click(object sender, RoutedEventArgs e)
        {
            var selected = lstIgnored.SelectedItems.Cast<string>().ToList();
            foreach (var item in selected)
                _ignoredControls.Remove(item);
            lstIgnored.Items.Refresh();
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            _ignoredControls.Clear();
            lstIgnored.Items.Refresh();
        }

        private void lstIgnored_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
                btnRemove_Click(sender, e);
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
