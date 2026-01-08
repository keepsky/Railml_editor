using System;
using System.Windows;
using System.Windows.Controls;

namespace RailmlEditor.Views
{
    public partial class SettingsView : Window
    {
        private bool _isInitialized = false;

        public SettingsView()
        {
            InitializeComponent();
            LoadCurrentTheme();
            _isInitialized = true;
        }

        private void LoadCurrentTheme()
        {
            var app = (App)Application.Current;
            if (app.Resources.MergedDictionaries.Count > 0)
            {
                var dict = app.Resources.MergedDictionaries[0];
                string source = dict.Source?.OriginalString ?? "";

                if (source.Contains("DarkTheme"))
                    ThemeCombo.SelectedIndex = 1; // Dark
                else
                    ThemeCombo.SelectedIndex = 0; // Light (Default)
            }
            else
            {
                 ThemeCombo.SelectedIndex = 1; // Default to Dark if unsure
            }
        }

        private void ThemeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;

            if (ThemeCombo.SelectedItem is ComboBoxItem item && item.Tag is string themeTag)
            {
                var app = (App)Application.Current;
                app.ChangeTheme(themeTag);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
