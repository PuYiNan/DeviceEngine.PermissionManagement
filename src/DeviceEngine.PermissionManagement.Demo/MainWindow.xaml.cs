using DeviceEngine.PermissionManagement.Editors;
using DeviceEngine.PermissionManagement.Managers;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace DeviceEngine.PermissionManagement.Demo
{
    public partial class MainWindow : Window
    {
        private readonly IPermissionManager _permissionManager;
        private int _dynamicButtonCount = 0;

        public MainWindow(IPermissionManager permissionManager)
        {
            _permissionManager = permissionManager;
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadRoleComboBox();
            _permissionManager.ScanControls(this);
            _permissionManager.ApplyPermissions(this);
        }

        private void LoadRoleComboBox()
        {
            var config = _permissionManager.GetConfig();
            if (config == null) return;

            var currentRole = config.CurrentRole;
            cmbRole.Items.Clear();
            foreach (var role in config.Roles)
            {
                cmbRole.Items.Add(role.Name);
            }

            if (!string.IsNullOrEmpty(currentRole))
            {
                cmbRole.SelectedItem = currentRole;
            }
            else if (cmbRole.Items.Count > 0)
            {
                cmbRole.SelectedIndex = 0;
            }
        }

        private void cmbRole_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbRole.SelectedItem is string roleName && !string.IsNullOrEmpty(roleName))
            {
                _permissionManager.SetCurrentRole(roleName);
                _permissionManager.ApplyPermissions(this);
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

            _permissionManager.RegisterControl(dynamicBtn.Name, dynamicBtn);
            pnlDynamicControls.Children.Add(dynamicBtn);
            _permissionManager.RequestScan();
        }

        private void btnConfig_Click(object sender, RoutedEventArgs e)
        {
            var editor = new PermissionConfigEditor
            {
                Owner = this
            };

            editor.Closed += (s, args) =>
            {
                _permissionManager.ReloadConfiguration();
                LoadRoleComboBox();
                _permissionManager.ScanControls(this);
                _permissionManager.ApplyPermissions(this);
            };

            editor.ShowDialog();
        }
    }
}
