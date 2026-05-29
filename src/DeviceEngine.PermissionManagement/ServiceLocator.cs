using System;

namespace DeviceEngine.PermissionManagement
{
    public static class ServiceLocator
    {
        public static IServiceProvider Current { get; set; }
    }
}
