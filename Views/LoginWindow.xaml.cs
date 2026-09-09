using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace InventarioAppDesktop.Views;

public partial class LoginWindow : Window
{
    private readonly ViewModels.LoginViewModel _vm = new();

    public LoginWindow()
    {
        InitializeComponent();
        DataContext = _vm;
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void Maximize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Normal ? WindowState.Maximized : WindowState.Normal;
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        TxtPassword.Password = string.Empty;
        Close();
    }

    private async void BtnLogin_Click(object sender, RoutedEventArgs e)
    {
        await RealizarLoginAsync();
    }

    private async Task<bool> RealizarLoginAsync()
    {
        var ok = await _vm.LoginAsync(TxtPassword.Password);
        if (ok)
        {
            if (Data.Session.UsuarioActual?.DebeCambiarPassword == true)
            {
                var change = new CambiarPasswordWindow(Data.Session.UsuarioActual.Id)
                {
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };
                if (change.ShowDialog() != true)
                {
                    Data.Session.Clear();
                    TxtPassword.Password = string.Empty;
                    return false;
                }
            }

            var main = new MainWindow();
            main.Show();
            Close();
            return true;
        }

        TxtPassword.Password = string.Empty;
        return false;
    }
}

public class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => !string.IsNullOrEmpty(value as string) ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class InverseBooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && !b ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class IntToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is int i && i == 0 ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
