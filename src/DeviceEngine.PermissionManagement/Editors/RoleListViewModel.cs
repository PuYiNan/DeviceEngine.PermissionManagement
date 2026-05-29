using DeviceEngine.PermissionManagement.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace DeviceEngine.PermissionManagement.Editors
{
    public class RoleListViewModel : INotifyPropertyChanged
    {
        private Role _selectedRole;

        public ObservableCollection<Role> Roles { get; set; } = new ObservableCollection<Role>();
        public ObservableCollection<PermissionBindingItem> PermissionBindingItems { get; set; } = new ObservableCollection<PermissionBindingItem>();

        public Role SelectedRole
        {
            get => _selectedRole;
            set
            {
                _selectedRole = value;
                OnPropertyChanged(nameof(SelectedRole));
                RoleSelected?.Invoke(this, value);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public event System.EventHandler<Role> RoleSelected;

        public void LoadRoles(PermissionConfig config)
        {
            Roles.Clear();
            if (config?.Roles != null)
            {
                foreach (var role in config.Roles)
                {
                    Roles.Add(role);
                }
            }
        }

        public void RefreshBindings(PermissionConfig config)
        {
            PermissionBindingItems.Clear();
            if (_selectedRole == null || config?.Permissions == null) return;

            foreach (var perm in config.Permissions)
            {
                bool isBound = _selectedRole.PermissionNames.Contains(perm.Name);
                var item = new PermissionBindingItem(perm.Name, isBound, bound =>
                {
                    if (bound)
                    {
                        if (!_selectedRole.PermissionNames.Contains(perm.Name))
                            _selectedRole.PermissionNames.Add(perm.Name);
                    }
                    else
                    {
                        _selectedRole.PermissionNames.Remove(perm.Name);
                    }
                });
                PermissionBindingItems.Add(item);
            }
        }

        public void AddRole(PermissionConfig config, string name)
        {
            if (!string.IsNullOrEmpty(name) && !Roles.Any(r => r.Name == name))
            {
                var role = new Role { Name = name };
                config.Roles.Add(role);
                Roles.Add(role);
                SelectedRole = role;
            }
        }

        public void RemoveRole(PermissionConfig config, Role role)
        {
            if (config != null && role != null && Roles.Count > 1)
            {
                config.Roles.Remove(role);
                Roles.Remove(role);
                if (SelectedRole == role)
                {
                    SelectedRole = Roles.Count > 0 ? Roles[0] : null;
                }
            }
        }

        public void RenameRole(Role role, string newName)
        {
            if (!string.IsNullOrEmpty(newName) && !Roles.Any(r => r.Name == newName && r != role))
            {
                role.Name = newName;
                OnPropertyChanged(nameof(Roles));
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
