using DeviceEngine.PermissionManagement.Managers;
using System.Windows;

namespace DeviceEngine.PermissionManagement.Demo
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            PermissionManager.Instance.LoadConfiguration("Config/permissions.json");
            PermissionManager.Instance.SetCurrentRole("Operator");
            PermissionManager.Instance.EnableHotReload = true;

            var mainWindow = new MainWindow();
            mainWindow.Show();
        }
    }
}