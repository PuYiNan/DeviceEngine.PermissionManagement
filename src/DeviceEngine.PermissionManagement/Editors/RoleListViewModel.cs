using DeviceEngine.PermissionManagement.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.ComponentModel;

namespace DeviceEngine.PermissionManagement.Editors
{
    public class RoleListViewModel : INotifyPropertyChanged
    {
        private Role _selectedRole;

        public ObservableCollection<Role> Roles { get; set; } = new ObservableCollection<Role>();

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

        public void LoadRoles(List<Role> roles)
        {
            Roles.Clear();
            foreach (var role in roles)
            {
                Roles.Add(role);
            }
        }

        public void AddRole(string name)
        {
            if (!string.IsNullOrEmpty(name) && !Roles.Any(r => r.Name == name))
            {
                var role = new Role { Name = name };
                role.Permissions.Add(new Permission { Name = $"{name}Access" });
                Roles.Add(role);
                SelectedRole = role;
            }
        }

        public void RemoveRole(Role role)
        {
            if (Roles.Count > 1)
            {
                Roles.Remove(role);
                if (SelectedRole == role)
                {
                    SelectedRole = Roles[0];
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