using System.Collections.Generic;

namespace DeviceEngine.PermissionManagement.Models
{
    public class Role
    {
        public string Name { get; set; }

        public string Description { get; set; } = "";

        public List<string> PermissionNames { get; set; } = new List<string>();
    }
}