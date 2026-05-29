using DeviceEngine.PermissionManagement.Managers;
using DeviceEngine.PermissionManagement.Models;
using Microsoft.Win32;
using Newtonsoft.Json;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DeviceEngine.PermissionManagement.Editors
{
    public partial class PermissionConfigEditor : Window, INotifyPropertyChanged
    {
        private readonly IPermissionManager _permissionManager;

        public RoleListViewModel RoleList { get; } = new RoleListViewModel();
        public PermissionEditorViewModel PermissionEditor { get; } = new PermissionEditorViewModel();
        public ControlTreeViewModel ControlTree { get; } = new ControlTreeViewModel();

        public PermissionConfigEditor()
        {
            _permissionManager = ServiceLocator.Current.GetService(typeof(IPermissionManager)) as IPermissionManager;

            InitializeComponent();
            DataContext = this;

            RoleList.RoleSelected += RoleList_RoleSelected;
            PermissionEditor.PermissionSelected += PermissionEditor_PermissionSelected;

            LoadConfig();
            RefreshControls();
        }

        private void LoadConfig()
        {
            var config = _permissionManager?.GetConfig();
            if (config != null)
            {
                RoleList.LoadRoles(config);
                PermissionEditor.LoadPermissions(config);
                if (RoleList.Roles.Count > 0)
                {
                    RoleList.SelectedRole = RoleList.Roles[0];
                }
            }
        }

        private void RefreshControls()
        {
            var config = _permissionManager?.GetConfig();
            foreach (var window in Application.Current.Windows)
            {
                if (window is Window w && w != this)
                {
                    ControlTree.ScanMode = config?.ScanMode ?? ScanMode.Hybrid;
                    ControlTree.IgnoredControls = config?.IgnoredControls;
                    ControlTree.ScanControls(w);
                    break;
                }
            }
        }

        private void RoleList_RoleSelected(object sender, Role role)
        {
            if (role != null)
            {
                var config = _permissionManager?.GetConfig();
                RoleList.RefreshBindings(config);
                PermissionEditor.LoadPermissions(config);
                RefreshControlSelection();
            }
        }

        private void PermissionEditor_PermissionSelected(object sender, Permission permission)
        {
            RefreshControlSelection();
        }

        private void RefreshControlSelection()
        {
            if (PermissionEditor.SelectedPermission != null)
            {
                var allRestricted = PermissionEditor.SelectedPermission.DisabledControls
                    .Concat(PermissionEditor.SelectedPermission.HiddenControls).ToList();
                ControlTree.SetSelectedPaths(allRestricted);
            }
            else
            {
                ControlTree.ClearSelection();
            }
        }

        private void btnAddRole_Click(object sender, RoutedEventArgs e)
        {
            var config = _permissionManager?.GetConfig();
            if (config == null) return;

            var dialog = new NameDescriptionDialog("添加角色", "角色名称:", "角色描述:");
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                RoleList.AddRole(config, dialog.InputName, dialog.InputDescription);
                RoleList.RefreshBindings(config);
            }
        }

        private void btnRemoveRole_Click(object sender, RoutedEventArgs e)
        {
            var config = _permissionManager?.GetConfig();
            if (RoleList.SelectedRole != null && config != null)
            {
                if (MessageBox.Show($"确定要删除角色 \"{RoleList.SelectedRole.Name}\" 吗?", "确认删除", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    RoleList.RemoveRole(config, RoleList.SelectedRole);
                    RoleList.RefreshBindings(config);
                }
            }
        }

        private void btnAddPermission_Click(object sender, RoutedEventArgs e)
        {
            var config = _permissionManager?.GetConfig();
            if (config == null) return;

            var dialog = new NameDescriptionDialog("添加权限", "权限名称:", "权限描述:");
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                PermissionEditor.AddPermission(config, dialog.InputName, dialog.InputDescription);
                RoleList.RefreshBindings(config);
            }
        }

        private void btnRemovePermission_Click(object sender, RoutedEventArgs e)
        {
            var config = _permissionManager?.GetConfig();
            if (config != null && PermissionEditor.SelectedPermission != null)
            {
                PermissionEditor.RemovePermission(config, PermissionEditor.SelectedPermission);
                RoleList.RefreshBindings(config);
            }
        }

        private void btnAddSelectedToDisabled_Click(object sender, RoutedEventArgs e)
        {
            if (PermissionEditor.SelectedPermission != null)
            {
                var selected = ControlTree.GetSelectedPaths();
                foreach (var path in selected)
                {
                    PermissionEditor.AddDisabledControl(PermissionEditor.SelectedPermission, path);
                }
            }
        }

        private void btnAddSelectedToHidden_Click(object sender, RoutedEventArgs e)
        {
            if (PermissionEditor.SelectedPermission != null)
            {
                var selected = ControlTree.GetSelectedPaths();
                foreach (var path in selected)
                {
                    PermissionEditor.AddHiddenControl(PermissionEditor.SelectedPermission, path);
                }
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            SaveConfig();
            MessageBox.Show("配置已保存!", "保存成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SaveConfig()
        {
            var config = _permissionManager?.GetConfig();
            if (config != null)
            {
                _permissionManager.SaveConfig();
            }
        }

        private void btnImport_Click(object sender, RoutedEventArgs e)
        {
            var config = _permissionManager?.GetConfig();
            if (config == null) return;

            var dialog = new OpenFileDialog { Filter = "JSON Files|*.json" };
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    string json = File.ReadAllText(dialog.FileName);
                    var imported = JsonConvert.DeserializeObject<PermissionConfig>(json);
                    if (imported != null)
                    {
                        config.Roles = imported.Roles;
                        config.Permissions = imported.Permissions;
                        config.ScanMode = imported.ScanMode;

                        RoleList.LoadRoles(config);
                        PermissionEditor.LoadPermissions(config);
                        if (RoleList.Roles.Count > 0)
                        {
                            RoleList.SelectedRole = RoleList.Roles[0];
                        }
                        RoleList.RefreshBindings(config);
                    }
                    MessageBox.Show("配置导入成功!", "导入成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch
                {
                    MessageBox.Show("导入失败，请检查文件格式!", "导入失败", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            var config = _permissionManager?.GetConfig();
            if (config == null) return;

            var dialog = new SaveFileDialog { Filter = "JSON Files|*.json", FileName = "permissions.json" };
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    string json = JsonConvert.SerializeObject(config, Formatting.Indented);
                    File.WriteAllText(dialog.FileName, json);
                    MessageBox.Show("配置导出成功!", "导出成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch
                {
                    MessageBox.Show("导出失败!", "导出失败", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void btnRefreshControls_Click(object sender, RoutedEventArgs e)
        {
            RefreshControls();
            RefreshControlSelection();
        }

        private void btnEditIgnored_Click(object sender, RoutedEventArgs e)
        {
            var config = _permissionManager?.GetConfig();
            if (config == null) return;

            var dialog = new IgnoredControlsDialog(config.IgnoredControls);
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                config.IgnoredControls = dialog.Result;
                RefreshControls();
                RefreshControlSelection();
            }
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            SaveConfig();
            _permissionManager?.ReloadConfiguration();
            DialogResult = true;
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void btnRemoveDisabled_Click(object sender, RoutedEventArgs e)
        {
            RemoveSelectedFromDisabledList();
        }

        private void btnClearDisabled_Click(object sender, RoutedEventArgs e)
        {
            if (PermissionEditor.SelectedPermission != null)
                PermissionEditor.SelectedPermission.DisabledControls.Clear();
        }

        private void lstDisabledControls_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
                RemoveSelectedFromDisabledList();
        }

        private void btnRemoveHidden_Click(object sender, RoutedEventArgs e)
        {
            RemoveSelectedFromHiddenList();
        }

        private void btnClearHidden_Click(object sender, RoutedEventArgs e)
        {
            if (PermissionEditor.SelectedPermission != null)
                PermissionEditor.SelectedPermission.HiddenControls.Clear();
        }

        private void lstHiddenControls_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
                RemoveSelectedFromHiddenList();
        }

        private void RemoveSelectedFromDisabledList()
        {
            if (PermissionEditor.SelectedPermission == null) return;
            var selected = lstDisabledControls.SelectedItems.Cast<string>().ToList();
            foreach (var path in selected)
                PermissionEditor.RemoveDisabledControl(PermissionEditor.SelectedPermission, path);
        }

        private void RemoveSelectedFromHiddenList()
        {
            if (PermissionEditor.SelectedPermission == null) return;
            var selected = lstHiddenControls.SelectedItems.Cast<string>().ToList();
            foreach (var path in selected)
                PermissionEditor.RemoveHiddenControl(PermissionEditor.SelectedPermission, path);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
