using Microsoft.Extensions.DependencyInjection;
using RentProject.Clients;
using RentProject.Settings;
using System;
using System.Threading;
using System.Windows.Forms;

namespace RentProject
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 1. 建 DI 容器
            var services = new ServiceCollection();

            // 2. 註冊 API Client
            // 讀外部設定檔（不存在就用 App.config 預設值建立一份 settings.json）
            var ext = ExternalSettingsLoader.LoadOrCreateFromAppConfig();

            // 取出 RentApi 設定
            var BaseUrl = string.IsNullOrWhiteSpace(ext.RentApi.BaseUrl)
                ? "https://localhost:7063/"
                : ext.RentApi.BaseUrl;

            var timeout = ext.RentApi.TimeoutSeconds <= 0 ? 10 : ext.RentApi.TimeoutSeconds;

            // 3. IJobNoApiClient（給 JobNoService 用）
            services.AddHttpClient<IJobNoApiClient, RentProject.Clients.RentProjectApiJobNoClient>(http =>
            {
                http.BaseAddress = new Uri(BaseUrl, UriKind.Absolute);
                http.Timeout = TimeSpan.FromSeconds(timeout);
            });

            services.AddHttpClient<IRentTimeApiClient, RentProjectApiRentTimeClient>(http =>
            {
                http.BaseAddress = new Uri(BaseUrl, UriKind.Absolute);
                http.Timeout = TimeSpan.FromSeconds(timeout);
            });

            // 4. 註冊 Form（UI）
            services.AddSingleton<Form1>();     // 整個程式生命週期只會建立 1 個 Form1 實例
            services.AddTransient<Project>();   // 每次從 DI 取得 Project 都會 new 一個新的實例

            // 5. Build ServiceProvider
            var sp = services.BuildServiceProvider();

            // 6. 用 DI 建出 Form1（不要 new）
            // 為什麼叫 GetRequiredService？ 因為它的語氣是「一定要拿到」：
            var mainForm = sp.GetRequiredService<Form1>();
            Application.Run(mainForm);
        }
    }
}
