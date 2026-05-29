using System.Collections.Generic;

namespace DeviceEngine.PermissionManagement.Models
{
    public class PermissionConfig
    {
        public ScanMode ScanMode { get; set; } = ScanMode.Hybrid;

        public string CurrentRole { get; set; }

        public List<Permission> Permissions { get; set; } = new List<Permission>();

        public List<Role> Roles { get; set; } = new List<Role>();

        public List<string> IgnoredControls { get; set; } = new List<string>
        {
            "border", "contentPresenter", "templateRoot",
            "PART_Popup", "PART_EditableTextBox", "splitBorder", "Arrow",
            "toggleButton", "Header", "ScrollViewer", "ScrollContentPresenter"
        };
    }
}