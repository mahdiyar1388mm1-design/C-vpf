using NexusControlCenter.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
namespace NexusControlCenter.Views;
public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent(); DataContext = viewModel;
    }
    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed) { if (e.ClickCount == 2) ToggleMaximize(); else DragMove(); }
    }
    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void Maximize_Click(object sender, RoutedEventArgs e) => ToggleMaximize();
    private void ToggleMaximize() => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    private void Close_Click(object sender, RoutedEventArgs e) => Close();
    private void FileGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    { if (DataContext is MainViewModel vm) vm.OpenFileCommand.Execute(null); }
}
