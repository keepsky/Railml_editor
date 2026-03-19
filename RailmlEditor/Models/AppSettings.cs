using System;
using System.IO;
using System.Text.Json;

namespace RailmlEditor.Models
{
    /// <summary>
    /// 에디터 전체에서 공통으로 쓰는 환경설정(테마 색상, 선로 스냅 거리 등)을 모아둔 클래스입니다.
    /// 이 정보는 `appsettings.json` 이라는 파일에 저장되었다가, 앱이 켜질 때 불러와집니다.
    /// </summary>
    public class AppSettings
    {
        public string Theme { get; set; } = "DarkTheme";
        public double NodeMappingTolerance { get; set; } = 5.0;

        private static readonly string SettingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

        private static AppSettings? _instance;
        public static AppSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Load();
                }
                return _instance;
            }
        }

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    string json = File.ReadAllText(SettingsFilePath);
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex}");
            }
            return new AppSettings();
        }

        public void Save()
        {
            try
            {
                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex}");
            }
        }
    }
}
