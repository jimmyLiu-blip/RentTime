using System;
using System.Configuration;
using System.IO;
using System.Text.Json;

namespace RentProject.Settings
{
    // settings.json 的最外層結構
    public sealed class AppSettingsRoot
    {
        public RentApiSettings RentApi { get; set; } = new();
    }

    // RentApi 設定
    public sealed class RentApiSettings
    {
        public string BaseUrl { get; set; } = "https://localhost:7063/";
        public int TimeoutSeconds { get; set; } = 10;
    }

    public static class ExternalSettingsLoader
    {
        private static readonly JsonSerializerOptions _jsonOpt = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        // 外部設定檔完整路徑：C:\Users\<user>\AppData\Local\RentProject\settings.json
        public static string GetSettingsPath()
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RentProject");

            return Path.Combine(dir, "settings.json");
        }

        // 讀取 settings.json；若沒有就用 App.config 的預設值建立一份
        public static AppSettingsRoot LoadOrCreateFromAppConfig()
        {
            var path = GetSettingsPath();
            var dir = Path.GetDirectoryName(path)!;

            // 1) 先從 App.config 拿「預設值」（外部檔不存在時會用）
            var defaultBaseUrl =
                ConfigurationManager.AppSettings["RentApi:BaseUrl"]
                ?? "https://localhost:7063/";

            var defaultTimeout =
                int.TryParse(ConfigurationManager.AppSettings["RentApi:TimeoutSeconds"], out var t)
                    ? t
                    : 10;

            // 2) 如果 settings.json 不存在：建立資料夾 + 建檔
            if (!File.Exists(path))
            {
                Directory.CreateDirectory(dir);

                var init = new AppSettingsRoot
                {
                    RentApi = new RentApiSettings
                    {
                        BaseUrl = defaultBaseUrl,
                        TimeoutSeconds = defaultTimeout
                    }
                };

                var json = JsonSerializer.Serialize(init, _jsonOpt);
                File.WriteAllText(path, json);

                return init;
            }

            // 3) settings.json 存在：讀檔
            try
            {
                var json = File.ReadAllText(path);
                var data = JsonSerializer.Deserialize<AppSettingsRoot>(json, _jsonOpt);

                // 讀到 null 就回預設，避免程式掛掉
                return data ?? new AppSettingsRoot
                {
                    RentApi = new RentApiSettings
                    {
                        BaseUrl = defaultBaseUrl,
                        TimeoutSeconds = defaultTimeout
                    }
                };
            }
            catch
            {
                // 檔案壞掉/格式錯：回 App.config 預設值，避免 UI 起不來
                return new AppSettingsRoot
                {
                    RentApi = new RentApiSettings
                    {
                        BaseUrl = defaultBaseUrl,
                        TimeoutSeconds = defaultTimeout
                    }
                };
            }
        }
    }
}
