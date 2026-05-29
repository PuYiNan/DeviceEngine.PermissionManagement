using System;
using System.ComponentModel;

namespace DeviceEngine.PermissionManagement.Editors
{
    public class PermissionBindingItem : INotifyPropertyChanged
    {
        private bool _isBound;
        private readonly Action<bool> _onBindingChanged;

        public string PermissionName { get; set; }

        public bool IsBound
        {
            get => _isBound;
            set
            {
                if (_isBound == value) return;
                _isBound = value;
                OnPropertyChanged(nameof(IsBound));
                _onBindingChanged?.Invoke(value);
            }
        }

        public PermissionBindingItem(string permissionName, bool isBound, Action<bool> onBindingChanged)
        {
            PermissionName = permissionName;
            _isBound = isBound;
            _onBindingChanged = onBindingChanged;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
