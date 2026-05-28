using DeviceEngine.PermissionManagement.Models;
using System.Windows;

namespace DeviceEngine.PermissionManagement.Managers
{
    public interface IPermissionManager
    {
        ScanMode ScanMode { get; set; }
        
        bool EnableHotReload { get; set; }
        
        void LoadConfiguration(string filePath);
        
        void ReloadConfiguration();
        
        void SetCurrentRole(string roleName);
        
        Role GetCurrentRole();
        
        bool CheckControlEnabled(string controlPath);
        
        bool CheckControlVisible(string controlPath);
        
        void ApplyPermissions(DependencyObject rootElement);
        
        void ScanControls(DependencyObject root);
        
        void RequestScan();
        
        void RegisterControl(string tag, FrameworkElement control);
        
        void UnregisterControl(string tag);
    }
}