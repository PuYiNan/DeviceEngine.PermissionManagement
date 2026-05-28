using DeviceEngine.PermissionManagement.Managers;
using DeviceEngine.PermissionManagement.Models;
using Microsoft.Win32;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace DeviceEngine.PermissionManagement.Editors
{
    public partial class PermissionConfigEditor : Window, INotifyPropertyChanged
    {
        public RoleListViewModel RoleList { get; } = new RoleListViewModel();
        public PermissionEditorViewModel PermissionEditor { get; } = new PermissionEditorViewModel();
        public ControlTreeViewModel ControlTree { get; } = new ControlTreeViewModel();

        public List<string> SelectedDisabledControls => PermissionEditor.SelectedPermission?.DisabledControls ?? new List<string>();
        public List<string> SelectedHiddenControls => PermissionEditor.SelectedPermission?.HiddenControls ?? new List<string>();

        public PermissionConfigEditor()
        {
            InitializeComponent();
            DataContext = this;

            RoleList.RoleSelected += RoleList_RoleSelected;
            PermissionEditor.PermissionSelected += PermissionEditor_PermissionSelected;

            LoadConfig();
            RefreshControls();
        }

        private void LoadConfig()
        {
            var config = PermissionManager.Instance.GetType().GetField("_config", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.GetValue(PermissionManager.Instance) as PermissionConfig;
            if (config != null)
            {
                RoleList.LoadRoles(config.Roles);
            }
        }

        private void RefreshControls()
        {
            foreach (var window in Application.Current.Windows)
            {
                if (window is Window w && w != this)
                {
                    ControlTree.ScanControls(w);
                    break;
                }
            }
        }

        private void RoleList_RoleSelected(object sender, Role role)
        {
            PermissionEditor.LoadPermissions(role);
            RefreshControlSelection();
        }

        private void PermissionEditor_PermissionSelected(object sender, Permission permission)
        {
            RefreshControlSelection();
            OnPropertyChanged(nameof(SelectedDisabledControls));
            OnPropertyChanged(nameof(SelectedHiddenControls));
        }

        private void RefreshControlSelection()
        {
            if (PermissionEditor.SelectedPermission != null)
            {
                var allRestricted = PermissionEditor.SelectedPermission.DisabledControls.Concat(PermissionEditor.SelectedPermission.HiddenControls).ToList();
                ControlTree.SetSelectedPaths(allRestricted);
            }
            else
            {
                ControlTree.ClearSelection();
            }
        }

        private void btnAddRole_Click(object sender, RoutedEventArgs e)
        {
            var input = new InputDialog("添加角色", "请输入角色名称:");
            if (input.ShowDialog() == true)
            {
                RoleList.AddRole(input.InputText);
            }
        }

        private void btnRemoveRole_Click(object sender, RoutedEventArgs e)
        {
            if (RoleList.SelectedRole != null)
            {
                if (MessageBox.Show($"确定要删除角色 \"{RoleList.SelectedRole.Name}\" 吗?", "确认删除", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    RoleList.RemoveRole(RoleList.SelectedRole);
                }
            }
        }

        private void btnAddPermission_Click(object sender, RoutedEventArgs e)
        {
            if (RoleList.SelectedRole != null)
            {
                PermissionEditor.AddPermission(RoleList.SelectedRole, txtPermissionName.Text);
                txtPermissionName.Clear();
            }
        }

        private void btnRemovePermission_Click(object sender, RoutedEventArgs e)
        {
            if (RoleList.SelectedRole != null && PermissionEditor.SelectedPermission != null)
            {
                PermissionEditor.RemovePermission(RoleList.SelectedRole, PermissionEditor.SelectedPermission);
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
                OnPropertyChanged(nameof(SelectedDisabledControls));
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
                OnPropertyChanged(nameof(SelectedHiddenControls));
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            SaveConfig();
            MessageBox.Show("配置已保存!", "保存成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SaveConfig()
        {
            var configField = PermissionManager.Instance.GetType().GetField("_config", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var config = configField?.GetValue(PermissionManager.Instance) as PermissionConfig;
            if (config != null)
            {
                config.Roles = RoleList.Roles.ToList();
                PermissionManager.Instance.GetType().GetMethod("SaveConfiguration", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.Invoke(PermissionManager.Instance, null);
            }
        }

        private void btnImport_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog { Filter = "JSON Files|*.json" };
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    string json = File.ReadAllText(dialog.FileName);
                    var config = JsonConvert.DeserializeObject<PermissionConfig>(json);
                    RoleList.LoadRoles(config.Roles);
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
            var dialog = new SaveFileDialog { Filter = "JSON Files|*.json", FileName = "permissions.json" };
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var config = new PermissionConfig { Roles = RoleList.Roles.ToList() };
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
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            SaveConfig();
            PermissionManager.Instance.ReloadConfiguration();
            DialogResult = true;
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class InputDialog : Window
    {
        public string InputText { get; set; }

        public InputDialog(string title, string prompt)
        {
            Title = title;
            Width = 300;
            Height = 150;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var label = new Label { Content = prompt, Margin = new Thickness(5) };
            Grid.SetRow(label, 0);

            var textBox = new TextBox { Margin = new Thickness(5) };
            textBox.SetBinding(TextBox.TextProperty, new System.Windows.Data.Binding("InputText") { Mode = System.Windows.Data.BindingMode.TwoWay, Source = this });
            Grid.SetRow(textBox, 1);

            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(5) };
            var okBtn = new Button { Content = "确定", Width = 60, Margin = new Thickness(2) };
            okBtn.Click += (s, e) => { DialogResult = true; Close(); };
            var cancelBtn = new Button { Content = "取消", Width = 60, Margin = new Thickness(2) };
            cancelBtn.Click += (s, e) => { DialogResult = false; Close(); };
            buttonPanel.Children.Add(okBtn);
            buttonPanel.Children.Add(cancelBtn);
            Grid.SetRow(buttonPanel, 2);

            grid.Children.Add(label);
            grid.Children.Add(textBox);
            grid.Children.Add(buttonPanel);
            Content = grid;
        }
    }
}