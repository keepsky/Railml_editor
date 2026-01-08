using System.Configuration;
using System.Data;
using System.Windows;

namespace RailmlEditor;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public void ChangeTheme(string theme)
    {
        string themeFile = theme == "Light" ? "Themes/LightTheme.xaml" : "Themes/DarkTheme.xaml";
        var uri = new Uri(themeFile, UriKind.Relative);

        ResourceDictionary newTheme = new ResourceDictionary() { Source = uri };

        Resources.MergedDictionaries.Clear();
        Resources.MergedDictionaries.Add(newTheme);

        if (App.Current.MainWindow is MainWindow mainWindow)
        {
            mainWindow.SetTheme(theme);
        }
    }
}

