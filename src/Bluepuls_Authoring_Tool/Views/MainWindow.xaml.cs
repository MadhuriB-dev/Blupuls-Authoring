using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Bluepuls_Authoring_Tool.Models;
using Bluepuls_Authoring_Tool.ViewModels;

namespace Bluepuls_Authoring_Tool.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (DataContext is MainViewModel vm && e.NewValue is DlcTreeNode selectedNode)
            {
                vm.OnTreeNodeSelected(selectedNode);
            }
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }

    public class NodeTypeVisibilityConverter : IValueConverter
    {
        public string TargetType { get; set; } = string.Empty;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string nodeType && string.Equals(nodeType, TargetType, StringComparison.OrdinalIgnoreCase))
            {
                return Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
