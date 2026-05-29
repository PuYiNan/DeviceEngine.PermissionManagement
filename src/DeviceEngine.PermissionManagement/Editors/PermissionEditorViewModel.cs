using DeviceEngine.PermissionManagement.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace DeviceEngine.PermissionManagement.Editors
{
    public class PermissionEditorViewModel : INotifyPropertyChanged
    {
        private Permission _selectedPermission;

        public ObservableCollection<Permission> Permissions { get; set; } = new ObservableCollection<Permission>();

        public Permission SelectedPermission
        {
            get => _selectedPermission;
            set
            {
                _selectedPermission = value;
                OnPropertyChanged(nameof(SelectedPermission));
                PermissionSelected?.Invoke(this, value);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<Permission> PermissionSelected;

        public void LoadPermissions(PermissionConfig config)
        {
            Permissions.Clear();
            if (config?.Permissions != null)
            {
                foreach (var permission in config.Permissions)
                {
                    Permissions.Add(permission);
                }
                if (Permissions.Count > 0)
                {
                    SelectedPermission = Permissions[0];
                }
            }
        }

        public void AddPermission(PermissionConfig config, string name)
        {
            if (config != null && !string.IsNullOrEmpty(name) && !config.Permissions.Any(p => p.Name == name))
            {
                var permission = new Permission { Name = name };
                config.Permissions.Add(permission);
                Permissions.Add(permission);
                SelectedPermission = permission;
            }
        }

        public void RemovePermission(PermissionConfig config, Permission permission)
        {
            if (config != null && permission != null && config.Permissions.Count > 1)
            {
                config.Permissions.Remove(permission);
                Permissions.Remove(permission);

                foreach (var role in config.Roles)
                {
                    role.PermissionNames.Remove(permission.Name);
                }

                if (SelectedPermission == permission)
                {
                    SelectedPermission = Permissions.Count > 0 ? Permissions[0] : null;
                }
            }
        }

        public void AddDisabledControl(Permission permission, string controlPath)
        {
            if (permission != null && !string.IsNullOrEmpty(controlPath) && !permission.DisabledControls.Contains(controlPath))
            {
                permission.DisabledControls.Add(controlPath);
            }
        }

        public void RemoveDisabledControl(Permission permission, string controlPath)
        {
            if (permission != null)
            {
                permission.DisabledControls.Remove(controlPath);
            }
        }

        public void AddHiddenControl(Permission permission, string controlPath)
        {
            if (permission != null && !string.IsNullOrEmpty(controlPath) && !permission.HiddenControls.Contains(controlPath))
            {
                permission.HiddenControls.Add(controlPath);
            }
        }

        public void RemoveHiddenControl(Permission permission, string controlPath)
        {
            if (permission != null)
            {
                permission.HiddenControls.Remove(controlPath);
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
