using System.Configuration;
using System.Data;
using System.Windows;

namespace RailmlEditor;

    /// <summary>
    /// 프로그램이 가장 먼저 실행될 때 시작되는 진입점(Entry Point)입니다.
    /// 앱이 켜질 때 설정 파일(ConfigService)을 읽어오고, 저장된 테마(다크/라이트)를 적용하는 역할을 합니다.
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Services.ConfigService.Load();
            
            // Apply loaded theme (if different from default Light)
            if (Services.ConfigService.Current.Theme == "Dark")
            {
                ChangeTheme("Dark");
            }
        }

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
            
            // Persist
            if (Services.ConfigService.Current.Theme != theme)
            {
                Services.ConfigService.Current.Theme = theme;
                Services.ConfigService.Save();
            }
        }
    }

