using System.Collections.Generic;

namespace DeviceEngine.PermissionManagement.Models
{
    public class PermissionConfig
    {
        public ScanMode ScanMode { get; set; } = ScanMode.Hybrid;
        
        public string CurrentRole { get; set; }
        
        public List<Role> Roles { get; set; } = new List<Role>();
    }
}