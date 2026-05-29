using System.Collections.ObjectModel;

namespace DeviceEngine.PermissionManagement.Models
{
    public class Permission
    {
        public string Name { get; set; }

        public ObservableCollection<string> DisabledControls { get; set; } = new ObservableCollection<string>();

        public ObservableCollection<string> HiddenControls { get; set; } = new ObservableCollection<string>();
    }
}