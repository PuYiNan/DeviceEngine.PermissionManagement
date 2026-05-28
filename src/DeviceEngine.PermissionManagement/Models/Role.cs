using System.Collections.Generic;

namespace DeviceEngine.PermissionManagement.Models
{
    public class Role
    {
        public string Name { get; set; }
        
        public List<Permission> Permissions { get; set; } = new List<Permission>();
    }
}