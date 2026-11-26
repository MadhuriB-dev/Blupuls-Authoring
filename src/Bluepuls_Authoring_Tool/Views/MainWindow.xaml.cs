using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Bluepuls_Authoring_Tool.Models;
using Bluepuls_Authoring_Tool.ViewModels;

namespace Bluepuls_Authoring_Tool.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Closed += MainWindow_Closed;
        }

        private void MainWindow_Closed(object? sender, EventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.CleanupTempWorkspaces();
            }
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

        private void EditableComboCell_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGridCell { IsEditing: false, IsReadOnly: false, Column: DataGridTemplateColumn } cell)
            {
                if (!cell.IsFocused)
                    cell.Focus();

                var grid = FindVisualParent<DataGrid>(cell);
                if (grid != null)
                {
                    if (grid.SelectionUnit != DataGridSelectionUnit.FullRow)
                    {
                        if (!cell.IsSelected) cell.IsSelected = true;
                    }
                    else
                    {
                        var row = FindVisualParent<DataGridRow>(cell);
                        if (row != null && !row.IsSelected)
                            row.IsSelected = true;
                    }
                    grid.BeginEdit(e);
                }
            }
        }

        private static T? FindVisualParent<T>(DependencyObject? child) where T : DependencyObject
        {
            while (child != null)
            {
                if (child is T target) return target;
                child = VisualTreeHelper.GetParent(child);
            }
            return null;
        }
    }

    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool b && b ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
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

    public class InverseBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b && b)
                return Visibility.Collapsed;
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
