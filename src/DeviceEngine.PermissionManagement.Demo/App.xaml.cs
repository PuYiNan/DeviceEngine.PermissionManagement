using DeviceEngine.PermissionManagement;
using DeviceEngine.PermissionManagement.Managers;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Windows;

namespace DeviceEngine.PermissionManagement.Demo
{
    public partial class App : Application
    {
        private IServiceProvider _serviceProvider;

        protected override void OnStartup(StartupEventArgs e)
        {
            var services = new ServiceCollection();
            services.AddSingleton<IPermissionManager, PermissionManager>();
            _serviceProvider = services.BuildServiceProvider();

            ServiceLocator.Current = _serviceProvider;

            var pm = _serviceProvider.GetRequiredService<IPermissionManager>();
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "permissions.json");
            pm.Initialize(configPath);
            pm.SetCurrentRole("Operator");

            var mainWindow = new MainWindow(pm);
            mainWindow.Show();
        }
    }
}
