using DeviceEngine.PermissionManagement.Managers;
using System.Windows;

namespace DeviceEngine.PermissionManagement.Behaviors
{
    public static class PermissionBehavior
    {
        public static readonly DependencyProperty PermissionTagProperty =
            DependencyProperty.RegisterAttached(
                "PermissionTag",
                typeof(string),
                typeof(PermissionBehavior),
                new PropertyMetadata(null, OnPermissionTagChanged));

        public static readonly DependencyProperty AutoCheckProperty =
            DependencyProperty.RegisterAttached(
                "AutoCheck",
                typeof(bool),
                typeof(PermissionBehavior),
                new PropertyMetadata(true));

        public static readonly DependencyProperty CheckModeProperty =
            DependencyProperty.RegisterAttached(
                "CheckMode",
                typeof(CheckMode),
                typeof(PermissionBehavior),
                new PropertyMetadata(CheckMode.Both));

        public static string GetPermissionTag(DependencyObject obj)
        {
            return (string)obj.GetValue(PermissionTagProperty);
        }

        public static void SetPermissionTag(DependencyObject obj, string value)
        {
            obj.SetValue(PermissionTagProperty, value);
        }

        public static bool GetAutoCheck(DependencyObject obj)
        {
            return (bool)obj.GetValue(AutoCheckProperty);
        }

        public static void SetAutoCheck(DependencyObject obj, bool value)
        {
            obj.SetValue(AutoCheckProperty, value);
        }

        public static CheckMode GetCheckMode(DependencyObject obj)
        {
            return (CheckMode)obj.GetValue(CheckModeProperty);
        }

        public static void SetCheckMode(DependencyObject obj, CheckMode value)
        {
            obj.SetValue(CheckModeProperty, value);
        }

        private static void OnPermissionTagChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FrameworkElement element && e.NewValue is string tag && !string.IsNullOrEmpty(tag))
            {
                if (GetAutoCheck(element))
                {
                    Application.Current.Dispatcher.BeginInvoke(new System.Action(() =>
                    {
                        PermissionManager.Instance.RegisterControl(tag, element);
                        PermissionManager.Instance.ApplyControlPermission(element, tag);
                    }));
                }
            }
        }
    }

    public enum CheckMode
    {
        Enabled,
        Visible,
        Both
    }
}