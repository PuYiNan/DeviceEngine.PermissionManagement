using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace DeviceEngine.PermissionManagement.Editors
{
    public class NameDescriptionDialog : Window
    {
        public string InputName { get; set; }
        public string InputDescription { get; set; }

        public NameDescriptionDialog(string title, string nameLabel, string descriptionLabel)
        {
            Title = title;
            Width = 350;
            Height = 210;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var lblName = new Label { Content = nameLabel, Margin = new Thickness(8, 8, 8, 0) };
            Grid.SetRow(lblName, 0);

            var txtName = new TextBox { Margin = new Thickness(8, 0, 8, 4) };
            txtName.SetBinding(TextBox.TextProperty, new Binding("InputName") { Mode = BindingMode.TwoWay, Source = this, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
            Grid.SetRow(txtName, 1);

            var lblDesc = new Label { Content = descriptionLabel, Margin = new Thickness(8, 4, 8, 0) };
            Grid.SetRow(lblDesc, 2);

            var txtDesc = new TextBox { Margin = new Thickness(8, 0, 8, 4) };
            txtDesc.SetBinding(TextBox.TextProperty, new Binding("InputDescription") { Mode = BindingMode.TwoWay, Source = this, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
            Grid.SetRow(txtDesc, 3);

            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(8, 8, 8, 8) };
            var okBtn = new Button { Content = "确定", Width = 60, Margin = new Thickness(2), IsDefault = true };
            okBtn.Click += (s, ev) =>
            {
                if (string.IsNullOrWhiteSpace(InputName)) return;
                DialogResult = true;
                Close();
            };
            var cancelBtn = new Button { Content = "取消", Width = 60, Margin = new Thickness(2), IsCancel = true };
            cancelBtn.Click += (s, ev) => { DialogResult = false; Close(); };
            buttonPanel.Children.Add(okBtn);
            buttonPanel.Children.Add(cancelBtn);
            Grid.SetRow(buttonPanel, 4);

            grid.Children.Add(lblName);
            grid.Children.Add(txtName);
            grid.Children.Add(lblDesc);
            grid.Children.Add(txtDesc);
            grid.Children.Add(buttonPanel);
            Content = grid;
        }
    }
}
