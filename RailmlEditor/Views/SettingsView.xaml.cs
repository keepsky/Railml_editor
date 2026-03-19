using System;
using System.Windows;
using System.Windows.Controls;

namespace RailmlEditor.Views
{
    /// <summary>
    /// 상단 메뉴의 "설정(Settings)..."을 눌렀을 때 나타나는 팝업 창입니다.
    /// 테마(다크/라이트 모드)를 바꾸거나, 선로가 서로 달라붙는(스냅) 민감도 조절 등 환경설정을 담당합니다.
    /// </summary>
    public partial class SettingsView : Window
    {
        private bool _isInitialized = false;

        public SettingsView()
        {
            InitializeComponent();
            LoadCurrentTheme();
            LoadTolerance();
            _isInitialized = true;
        }

        private void LoadTolerance()
        {
            ToleranceSlider.Value = Models.AppSettings.Instance.NodeMappingTolerance;
            ToleranceValueText.Text = ToleranceSlider.Value.ToString("0.0");
        }

        private void ToleranceSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isInitialized) return;
            ToleranceValueText.Text = e.NewValue.ToString("0.0");
            Models.AppSettings.Instance.NodeMappingTolerance = e.NewValue;
            Models.AppSettings.Instance.Save();
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
