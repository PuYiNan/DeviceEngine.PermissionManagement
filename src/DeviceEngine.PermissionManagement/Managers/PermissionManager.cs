using DeviceEngine.PermissionManagement.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Threading;

namespace DeviceEngine.PermissionManagement.Managers
{
    public class PermissionManager : IPermissionManager
    {
        private static PermissionManager _instance;
        private PermissionConfig _config;
        private string _configFilePath;
        private HashSet<string> _scannedPaths = new HashSet<string>();
        private Dictionary<string, WeakReference<FrameworkElement>> _controlCache = new Dictionary<string, WeakReference<FrameworkElement>>();
        private DispatcherTimer _scanDebounceTimer;
        private FileSystemWatcher _fileWatcher;
        
        public static PermissionManager Instance => _instance ?? (_instance = new PermissionManager());
        
        public ScanMode ScanMode { get; set; } = ScanMode.Hybrid;
        
        public bool EnableHotReload { get; set; } = true;
        
        public event EventHandler RoleChanged;
        
        private PermissionManager()
        {
        }
        
        public void LoadConfiguration(string filePath)
        {
            _configFilePath = filePath;
            
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                _config = JsonConvert.DeserializeObject<PermissionConfig>(json);
                
                if (_config.ScanMode != ScanMode.Explicit && _config.ScanMode != ScanMode.Auto)
                {
                    _config.ScanMode = ScanMode.Hybrid;
                }
                
                ScanMode = _config.ScanMode;
            }
            else
            {
                _config = new PermissionConfig();
                SaveConfiguration();
            }
            
            if (EnableHotReload)
            {
                SetupFileWatcher();
            }
        }
        
        public void ReloadConfiguration()
        {
            if (!string.IsNullOrEmpty(_configFilePath))
            {
                LoadConfiguration(_configFilePath);
            }
        }
        
        public void SaveConfiguration()
        {
            if (!string.IsNullOrEmpty(_configFilePath))
            {
                string json = JsonConvert.SerializeObject(_config, Formatting.Indented);
                File.WriteAllText(_configFilePath, json);
            }
        }
        
        public void SetCurrentRole(string roleName)
        {
            _config.CurrentRole = roleName;
            SaveConfiguration();
            RaiseRoleChanged();
            ApplyPermissionsToAllRegisteredControls();
        }
        
        public Role GetCurrentRole()
        {
            return _config?.Roles.FirstOrDefault(r => r.Name == _config.CurrentRole);
        }
        
        public bool CheckControlEnabled(string controlPath)
        {
            if (_config == null) return true;
            
            var role = GetCurrentRole();
            if (role == null) return true;
            
            var allDisabled = role.Permissions.SelectMany(p => p.DisabledControls).ToList();
            
            string currentPath = controlPath;
            while (!string.IsNullOrEmpty(currentPath))
            {
                if (allDisabled.Contains(currentPath))
                {
                    return false;
                }
                currentPath = GetParentPath(currentPath);
            }
            
            return true;
        }
        
        public bool CheckControlVisible(string controlPath)
        {
            if (_config == null) return true;
            
            var role = GetCurrentRole();
            if (role == null) return true;
            
            var allHidden = role.Permissions.SelectMany(p => p.HiddenControls).ToList();
            
            string currentPath = controlPath;
            while (!string.IsNullOrEmpty(currentPath))
            {
                if (allHidden.Contains(currentPath))
                {
                    return false;
                }
                currentPath = GetParentPath(currentPath);
            }
            
            return true;
        }
        
        public void ApplyPermissions(DependencyObject rootElement)
        {
            TraverseVisualTree(rootElement, element =>
            {
                string tag = GetControlTag(element);
                if (!string.IsNullOrEmpty(tag))
                {
                    ApplyControlPermission(element, tag);
                }
            });
        }
        
        public void ScanControls(DependencyObject root)
        {
            var newPaths = new HashSet<string>();
            CollectControlPaths(root, "", newPaths);
            
            var addedPaths = newPaths.Except(_scannedPaths);
            var removedPaths = _scannedPaths.Except(newPaths);
            
            foreach (var path in addedPaths)
            {
                FrameworkElement control = FindControlByPath(root, path);
                if (control != null)
                {
                    RegisterControl(path, control);
                    ApplyControlPermission(control, path);
                }
            }
            
            foreach (var path in removedPaths)
            {
                UnregisterControl(path);
            }
            
            _scannedPaths = newPaths;
        }
        
        public void RequestScan()
        {
            if (_scanDebounceTimer != null)
            {
                _scanDebounceTimer.Stop();
            }
            
            _scanDebounceTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            
            _scanDebounceTimer.Tick += (s, e) =>
            {
                _scanDebounceTimer.Stop();
                foreach (var window in Application.Current.Windows)
                {
                    if (window is Window w)
                    {
                        ScanControls(w);
                    }
                }
            };
            
            _scanDebounceTimer.Start();
        }
        
        public void RegisterControl(string tag, FrameworkElement control)
        {
            if (_controlCache.ContainsKey(tag))
            {
                _controlCache.Remove(tag);
            }
            _controlCache[tag] = new WeakReference<FrameworkElement>(control);
        }
        
        public void UnregisterControl(string tag)
        {
            _controlCache.Remove(tag);
        }
        
        public void ApplyControlPermission(FrameworkElement control, string tag)
        {
            if (!control.IsLoaded)
            {
                RoutedEventHandler loadedHandler = null;
                loadedHandler = (s, e) =>
                {
                    control.Loaded -= loadedHandler;
                    ApplyControlPermissionInternal(control, tag);
                };
                control.Loaded += loadedHandler;
                return;
            }
            
            ApplyControlPermissionInternal(control, tag);
        }
        
        private void ApplyControlPermissionInternal(FrameworkElement control, string tag)
        {
            control.IsEnabled = CheckControlEnabled(tag);
            control.Visibility = CheckControlVisible(tag) ? Visibility.Visible : Visibility.Collapsed;
        }
        
        private void ApplyPermissionsToAllRegisteredControls()
        {
            var toRemove = new List<string>();
            
            foreach (var kvp in _controlCache)
            {
                if (kvp.Value.TryGetTarget(out FrameworkElement control))
                {
                    ApplyControlPermission(control, kvp.Key);
                }
                else
                {
                    toRemove.Add(kvp.Key);
                }
            }
            
            foreach (var key in toRemove)
            {
                _controlCache.Remove(key);
            }
        }
        
        private void CollectControlPaths(DependencyObject parent, string parentPath, HashSet<string> paths)
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                
                if (child is FrameworkElement fe)
                {
                    string tag = GetControlTag(fe);
                    
                    if (!string.IsNullOrEmpty(tag))
                    {
                        string path = string.IsNullOrEmpty(parentPath) ? tag : $"{parentPath}.{tag}";
                        paths.Add(path);
                        CollectControlPaths(child, path, paths);
                    }
                    else
                    {
                        CollectControlPaths(child, parentPath, paths);
                    }
                }
                else
                {
                    CollectControlPaths(child, parentPath, paths);
                }
            }
        }
        
        private string GetControlTag(FrameworkElement element)
        {
            switch (ScanMode)
            {
                case ScanMode.Explicit:
                    return Behaviors.PermissionBehavior.GetPermissionTag(element);
                
                case ScanMode.Auto:
                    return !string.IsNullOrEmpty(element.Name) ? element.Name : null;
                
                case ScanMode.Hybrid:
                default:
                    string tag = Behaviors.PermissionBehavior.GetPermissionTag(element);
                    return !string.IsNullOrEmpty(tag) ? tag : (string.IsNullOrEmpty(element.Name) ? null : element.Name);
            }
        }
        
        private FrameworkElement FindControlByPath(DependencyObject root, string path)
        {
            var parts = path.Split('.');
            DependencyObject current = root;
            
            foreach (string part in parts)
            {
                bool found = false;
                
                for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(current); i++)
                {
                    var child = System.Windows.Media.VisualTreeHelper.GetChild(current, i);
                    
                    if (child is FrameworkElement fe)
                    {
                        string tag = GetControlTag(fe);
                        if (tag == part)
                        {
                            current = child;
                            found = true;
                            break;
                        }
                    }
                }
                
                if (!found)
                {
                    return null;
                }
            }
            
            return current as FrameworkElement;
        }
        
        private string GetParentPath(string path)
        {
            int lastDotIndex = path.LastIndexOf('.');
            return lastDotIndex > 0 ? path.Substring(0, lastDotIndex) : null;
        }
        
        private void TraverseVisualTree(DependencyObject parent, Action<FrameworkElement> action)
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is FrameworkElement element)
                {
                    action(element);
                }
                TraverseVisualTree(child, action);
            }
        }
        
        private void SetupFileWatcher()
        {
            if (_fileWatcher != null)
            {
                _fileWatcher.Dispose();
            }
            
            _fileWatcher = new FileSystemWatcher
            {
                Path = Path.GetDirectoryName(_configFilePath),
                Filter = Path.GetFileName(_configFilePath),
                NotifyFilter = NotifyFilters.LastWrite
            };
            
            _fileWatcher.Changed += delegate (object s, FileSystemEventArgs e)
            {
                Application.Current.Dispatcher.BeginInvoke(new System.Action(ReloadConfiguration));
            };
            
            _fileWatcher.EnableRaisingEvents = true;
        }
        
        private void RaiseRoleChanged()
        {
            RoleChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}