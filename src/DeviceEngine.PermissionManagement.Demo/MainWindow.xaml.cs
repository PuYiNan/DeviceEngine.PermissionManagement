using DeviceEngine.PermissionManagement.Editors;
using DeviceEngine.PermissionManagement.Managers;
using System.Windows;
using System.Windows.Controls;

namespace DeviceEngine.PermissionManagement.Demo
{
    public partial class MainWindow : Window
    {
        private int _dynamicButtonCount = 0;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            PermissionManager.Instance.ScanControls(this);
        }

        private void cmbRole_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbRole.SelectedItem is ComboBoxItem selectedItem)
            {
                PermissionManager.Instance.SetCurrentRole(selectedItem.Content.ToString());
            }
        }

        private void btnAddDynamic_Click(object sender, RoutedEventArgs e)
        {
            _dynamicButtonCount++;
            var dynamicBtn = new Button
            {
                Name = $"btnDynamic{_dynamicButtonCount}",
                Content = $"动态按钮 {_dynamicButtonCount}",
                Margin = new Thickness(5),
                Padding = new Thickness(10, 5, 10, 5)
            };

            PermissionManager.Instance.RegisterControl(dynamicBtn.Name, dynamicBtn);

            pnlDynamicControls.Children.Add(dynamicBtn);

            PermissionManager.Instance.RequestScan();
        }

        private void btnConfig_Click(object sender, RoutedEventArgs e)
        {
            var editor = new PermissionConfigEditor
            {
                Owner = this
            };

            editor.Closed += (s, args) =>
            {
                PermissionManager.Instance.ReloadConfiguration();
                PermissionManager.Instance.ScanControls(this);
            };

            editor.ShowDialog();
        }
    }
}