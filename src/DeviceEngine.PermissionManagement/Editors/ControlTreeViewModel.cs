using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace DeviceEngine.PermissionManagement.Editors
{
    public class ControlTreeItem : INotifyPropertyChanged
    {
        private bool _isSelected;
        private bool _isExpanded = true;
        private bool _isChecked;

        public string Name { get; set; }
        public string FullPath { get; set; }
        public FrameworkElement Control { get; set; }
        public ControlTreeItem Parent { get; set; }
        public List<ControlTreeItem> Children { get; set; } = new List<ControlTreeItem>();

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                _isExpanded = value;
                OnPropertyChanged(nameof(IsExpanded));
            }
        }

        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (_isChecked == value) return;

                _isChecked = value;
                OnPropertyChanged(nameof(IsChecked));

                foreach (var child in Children)
                {
                    child.IsChecked = value;
                }

                if (Parent != null)
                {
                    Parent.UpdateCheckedState();
                }
            }
        }

        private void UpdateCheckedState()
        {
            if (Children.Count == 0) return;

            bool allChecked = Children.All(c => c.IsChecked);
            bool noneChecked = Children.All(c => !c.IsChecked);

            if (allChecked && !_isChecked)
            {
                _isChecked = true;
                OnPropertyChanged(nameof(IsChecked));
                if (Parent != null) Parent.UpdateCheckedState();
            }
            else if (noneChecked && _isChecked)
            {
                _isChecked = false;
                OnPropertyChanged(nameof(IsChecked));
                if (Parent != null) Parent.UpdateCheckedState();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ControlTreeViewModel
    {
        public List<ControlTreeItem> RootItems { get; set; } = new List<ControlTreeItem>();

        public void ScanControls(Window window)
        {
            RootItems.Clear();
            var rootItem = new ControlTreeItem
            {
                Name = !string.IsNullOrEmpty(window.Name) ? window.Name : "Window",
                FullPath = "",
                Control = window
            };
            RootItems.Add(rootItem);
            ScanVisualTree(window, rootItem);
        }

        private void ScanVisualTree(DependencyObject parent, ControlTreeItem parentItem)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                
                if (child is FrameworkElement fe)
                {
                    string tag = Behaviors.PermissionBehavior.GetPermissionTag(fe);
                    string name = !string.IsNullOrEmpty(tag) ? tag : fe.Name;
                    
                    if (!string.IsNullOrEmpty(name))
                    {
                        var item = new ControlTreeItem
                        {
                            Name = name,
                            FullPath = string.IsNullOrEmpty(parentItem.FullPath) ? name : $"{parentItem.FullPath}.{name}",
                            Control = fe,
                            Parent = parentItem
                        };
                        parentItem.Children.Add(item);
                        ScanVisualTree(child, item);
                    }
                    else
                    {
                        ScanVisualTree(child, parentItem);
                    }
                }
                else
                {
                    ScanVisualTree(child, parentItem);
                }
            }
        }

        public List<string> GetSelectedPaths()
        {
            var paths = new List<string>();
            CollectSelectedPaths(RootItems, paths);
            return paths;
        }

        private void CollectSelectedPaths(List<ControlTreeItem> items, List<string> paths)
        {
            foreach (var item in items)
            {
                if (item.IsChecked)
                {
                    paths.Add(item.FullPath);
                }
                CollectSelectedPaths(item.Children, paths);
            }
        }

        public void SetSelectedPaths(List<string> paths)
        {
            SetSelectedPaths(RootItems, paths);
        }

        private void SetSelectedPaths(List<ControlTreeItem> items, List<string> paths)
        {
            foreach (var item in items)
            {
                item.IsChecked = paths.Contains(item.FullPath);
                SetSelectedPaths(item.Children, paths);
            }
        }

        public void ClearSelection()
        {
            ClearSelection(RootItems);
        }

        private void ClearSelection(List<ControlTreeItem> items)
        {
            foreach (var item in items)
            {
                item.IsChecked = false;
                ClearSelection(item.Children);
            }
        }
    }
}