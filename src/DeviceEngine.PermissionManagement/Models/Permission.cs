using System.Collections.Generic;

namespace DeviceEngine.PermissionManagement.Models
{
    public class Permission
    {
        public string Name { get; set; }
        
        public List<string> DisabledControls { get; set; } = new List<string>();
        
        public List<string> HiddenControls { get; set; } = new List<string>();
    }
}