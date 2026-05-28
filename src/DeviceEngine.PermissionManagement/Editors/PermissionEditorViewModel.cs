using DeviceEngine.PermissionManagement.Models;
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
        public event System.EventHandler<Permission> PermissionSelected;

        public void LoadPermissions(Role role)
        {
            Permissions.Clear();
            if (role != null)
            {
                foreach (var permission in role.Permissions)
                {
                    Permissions.Add(permission);
                }
                if (Permissions.Count > 0)
                {
                    SelectedPermission = Permissions[0];
                }
            }
        }

        public void AddPermission(Role role, string name)
        {
            if (role != null && !string.IsNullOrEmpty(name) && !role.Permissions.Any(p => p.Name == name))
            {
                var permission = new Permission { Name = name };
                role.Permissions.Add(permission);
                Permissions.Add(permission);
                SelectedPermission = permission;
            }
        }

        public void RemovePermission(Role role, Permission permission)
        {
            if (role != null && role.Permissions.Count > 1)
            {
                role.Permissions.Remove(permission);
                Permissions.Remove(permission);
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
                OnPropertyChanged(nameof(Permissions));
            }
        }

        public void RemoveDisabledControl(Permission permission, string controlPath)
        {
            if (permission != null)
            {
                permission.DisabledControls.Remove(controlPath);
                OnPropertyChanged(nameof(Permissions));
            }
        }

        public void AddHiddenControl(Permission permission, string controlPath)
        {
            if (permission != null && !string.IsNullOrEmpty(controlPath) && !permission.HiddenControls.Contains(controlPath))
            {
                permission.HiddenControls.Add(controlPath);
                OnPropertyChanged(nameof(Permissions));
            }
        }

        public void RemoveHiddenControl(Permission permission, string controlPath)
        {
            if (permission != null)
            {
                permission.HiddenControls.Remove(controlPath);
                OnPropertyChanged(nameof(Permissions));
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}