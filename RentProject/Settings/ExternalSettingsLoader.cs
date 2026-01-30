using System;
using System.Configuration;
using System.IO;
using System.Text;
using System.Text.Json;

namespace RentProject.Settings
{
    // settings.json 的最外層結構
    public sealed class AppSettingsRoot
    {
        public int ConfigVersion { get; set; } = 0;

        public RentApiSettings RentApi { get; set; } = new();

        public AppSettingsSeed Seed { get; set; } = new();
    }

    // RentApi 設定
    public sealed class RentApiSettings
    {
        public string BaseUrl { get; set; } = "https://localhost:7063/";
        public int TimeoutSeconds { get; set; } = 10;
    }

    public sealed class AppSettingsSeed
    {
        public string RentApiBaseUrl { get; set; } = "https://localhost:7063/";
        public int RentApiTimeoutSeconds { get; set; } = 10;
    }

    public static class ExternalSettingsLoader
    {
        // 每次改設定結構或想觸發升級，就把版本 +1
        private const int CurrentConfigVersion = 2;

        // PropertyNameCaseInsensitive = true：JSON 欄位大小寫不同也能讀
        // WriteIndented = true：輸出 JSON 會漂亮縮排（方便人讀）
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

            // 1) 從 App.config 讀「新版預設值」
            var defaults = ReadDefaultsFromAppConfig();

            // 2) 如果 settings.json 不存在：建立資料夾 + 建檔
            if (!File.Exists(path))
            {
                Directory.CreateDirectory(dir);

                var created = CreateNew(defaults);
                SaveAtomic(path, created);
                return created;
            }

            // 3) settings.json 存在：讀檔
            try
            {
                var json = File.ReadAllText(path, Encoding.UTF8);
                var data = JsonSerializer.Deserialize<AppSettingsRoot>(json, _jsonOpt);

                if (data == null)
                {
                    // 讀到 null：當成壞檔，直接重建（也可改成先備份）
                    var rebuilt = CreateNew(defaults);
                    SaveAtomic(path, rebuilt);
                    return rebuilt;
                }

                // 3-1) 確保物件不為 null（兼容舊檔）
                data.RentApi ??= new RentApiSettings();
                data.Seed ??= new AppSettingsSeed();

                // 3-2) 如果舊檔沒有 ConfigVersion（預設 0），一樣會走升級
                var upgraded = UpgradeIfNeeded(data, defaults);

                // 若有升級或補齊，回寫
                if (upgraded.changed)
                {
                    SaveAtomic(path, upgraded.data);
                }

                return upgraded.data;
            }
            catch
            {
                // 檔案壞掉/格式錯：用 App.config 預設重建（可再加備份壞檔）
                var rebuilt = CreateNew(defaults);
                Directory.CreateDirectory(dir);
                SaveAtomic(path, rebuilt);
                return rebuilt;
            }
        }

        // ====== 讀取 App.config 預設值 ======
        // ====== 讀取 App.config 預設值 ======
        private static RentApiSettings ReadDefaultsFromAppConfig()
        {
            var baseUrl =
                ConfigurationManager.AppSettings["RentApi:BaseUrl"]
                ?? "https://localhost:7063/";

            var timeout =
                int.TryParse(ConfigurationManager.AppSettings["RentApi:TimeoutSeconds"], out var t)
                    ? t
                    : 10;

            return new RentApiSettings
            {
                BaseUrl = NormalizeAndValidateBaseUrl(baseUrl, "https://localhost:7063/"),
                TimeoutSeconds = NormalizeTimeoutSeconds(timeout, 10)
            };
        }

        // ====== 新建一份 settings.json（含 Seed + ConfigVersion） ======
        private static AppSettingsRoot CreateNew(RentApiSettings defaults)
        {
            return new AppSettingsRoot
            {
                ConfigVersion = CurrentConfigVersion,
                RentApi = new RentApiSettings
                {
                    BaseUrl = defaults.BaseUrl,
                    TimeoutSeconds = defaults.TimeoutSeconds
                },
                Seed = new AppSettingsSeed
                {
                    RentApiBaseUrl = defaults.BaseUrl,
                    RentApiTimeoutSeconds = defaults.TimeoutSeconds
                }
            };
        }

        // ====== 升級規則：使用者沒改過才跟著新版預設更新 ======
        private static (AppSettingsRoot data, bool changed) UpgradeIfNeeded(AppSettingsRoot data, RentApiSettings defaults)
        {
            bool changed = false;

            // 先正規化（避免因為少/多斜線判斷失準）
            data.RentApi.BaseUrl = NormalizeBaseUrl(data.RentApi.BaseUrl);
            data.Seed.RentApiBaseUrl = NormalizeBaseUrl(data.Seed.RentApiBaseUrl);

            // BaseUrl 驗證：不合法就回 defaults（但不會把「合法的使用者自訂」蓋掉）
            data.RentApi.BaseUrl = NormalizeAndValidateBaseUrl(data.RentApi.BaseUrl, defaults.BaseUrl);
            data.Seed.RentApiBaseUrl = NormalizeAndValidateBaseUrl(data.Seed.RentApiBaseUrl, defaults.BaseUrl);

            // Timeout 最小值保底：< 1 就回 defaults（或 10）
            data.RentApi.TimeoutSeconds = NormalizeTimeoutSeconds(data.RentApi.TimeoutSeconds, defaults.TimeoutSeconds);
            data.Seed.RentApiTimeoutSeconds = NormalizeTimeoutSeconds(data.Seed.RentApiTimeoutSeconds, defaults.TimeoutSeconds);

            // 沒有 Seed 的舊檔：補 Seed（當下用「現況」當 seed，避免突然覆蓋使用者值）
            if (string.IsNullOrWhiteSpace(data.Seed.RentApiBaseUrl))
            {
                data.Seed.RentApiBaseUrl = data.RentApi.BaseUrl;
                changed = true;
            }
            if (data.Seed.RentApiTimeoutSeconds <= 0)
            {
                data.Seed.RentApiTimeoutSeconds = data.RentApi.TimeoutSeconds;
                changed = true;
            }

            // 只有版本較舊才做升級
            if (data.ConfigVersion < CurrentConfigVersion)
            {
                // --- BaseUrl：使用者沒改過(=仍等於 Seed) 才更新成新版預設 ---
                if (NormalizeBaseUrl(data.RentApi.BaseUrl) == NormalizeBaseUrl(data.Seed.RentApiBaseUrl))
                {
                    data.RentApi.BaseUrl = defaults.BaseUrl;
                    changed = true;
                }

                // --- Timeout：同樣規則 ---
                if (data.RentApi.TimeoutSeconds == data.Seed.RentApiTimeoutSeconds)
                {
                    data.RentApi.TimeoutSeconds = defaults.TimeoutSeconds;
                    changed = true;
                }

                // 更新 Seed 成「新版預設」，並把版本升到最新
                data.Seed.RentApiBaseUrl = defaults.BaseUrl;
                data.Seed.RentApiTimeoutSeconds = defaults.TimeoutSeconds;
                data.ConfigVersion = CurrentConfigVersion;

                changed = true;
            }
            else
            {
                // 版本已最新，但仍可補強：確保 BaseUrl 格式正確（可選）
                var normalized = NormalizeBaseUrl(data.RentApi.BaseUrl);
                if (normalized != data.RentApi.BaseUrl)
                {
                    data.RentApi.BaseUrl = normalized;
                    changed = true;
                }
            }

            return (data, changed);
        }

        // ====== 避免使用者少打 / ：統一 BaseUrl 格式 ======
        private static string NormalizeBaseUrl(string? url)
        {
            var s = (url ?? "").Trim();
            if (string.IsNullOrWhiteSpace(s)) return "https://localhost:7063/";

            // 確保最後有 /
            if (!s.EndsWith("/")) s += "/";

            return s;
        }

        // ====== BaseUrl 驗證（最小版）======
        // 規則：
        // 1) 先 Normalize（Trim + 補 / + 空白回預設）
        // 2) 再用 Uri.TryCreate 驗證：必須是 Absolute + http/https
        // 不合法就回 fallback（也會 Normalize）
        private static string NormalizeAndValidateBaseUrl(string? url, string fallback)
        {
            var candidate = NormalizeBaseUrl(url);
            if (IsValidHttpUrl(candidate)) return candidate;

            // fallback 也做一次 Normalize，確保有 /
            var fb = NormalizeBaseUrl(fallback);
            return IsValidHttpUrl(fb) ? fb : "https://localhost:7063/";
        }

        private static bool IsValidHttpUrl(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
            return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
        }

        // ====== Timeout 合法範圍（最小版）======
        // 只做最基本：小於 1 就回 fallback（或預設 10）
        private static int NormalizeTimeoutSeconds(int value, int fallback)
        {
            if (value >= 1) return value;
            return fallback >= 1 ? fallback : 10;
        }


        // ====== 安全回寫：先寫 temp，再 replace（避免寫一半壞檔） ======
        private static void SaveAtomic(string path, AppSettingsRoot data)
        {
            // !告訴編譯器「我確定這裡不會是 null」，不要警告我
            var dir = Path.GetDirectoryName(path)!;
            // 如果資料夾不存在 → 建立 / 如果已存在 → 不會報錯，什麼都不做
            Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(data, _jsonOpt);

            var tmp = path + ".tmp";
            File.WriteAllText(tmp, json, Encoding.UTF8);

            // Windows 下 Replace 最穩；若不存在就 Move
            // 用 tmp 取代 settings.json / 把原本的 settings.json 另存成 settings.json.bak（備份）
            if (File.Exists(path))
            {
                File.Replace(tmp, path, path + ".bak", ignoreMetadataErrors: true);
            }
            else
            {
                // 式檔不存在（第一次建立）/ 把 tmp 直接改名成正式檔。
                File.Move(tmp, path);
            }
        }
    }
}
