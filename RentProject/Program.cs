using Microsoft.Extensions.DependencyInjection;
using RentProject.Repository;
using RentProject.Service;
using System;
using System.Configuration;
using System.Windows.Forms;
using RentProject.Settings;

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

            // 2 先檢查 ConnectionStrings 這個集合本身在不在
            var connectionString = 
                ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

            services.AddSingleton<string>(connectionString);

            // 3.註冊 Repositories（Dapper 連 DB）
            services.AddSingleton<DapperRentTimeRepository>(sp => new DapperRentTimeRepository(connectionString));
            services.AddSingleton<DapperProjectRepository>(sp => new DapperProjectRepository(connectionString));
            services.AddSingleton<DapperJobNoRepository>(sp => new DapperJobNoRepository(connectionString));
            
            // 4.註冊 Services（商業邏輯層）
            // 當有人需要 IJobNoApiClient 時，請給他 FakeJobNoApiClient 的實例
            services.AddSingleton<RentTimeService>();
            services.AddSingleton<ProjectService>();
            services.AddSingleton<JobNoService>();

            // 5. 註冊 API Client
            // 讀外部設定檔（不存在就用 App.config 預設值建立一份 settings.json）
            var ext = ExternalSettingsLoader.LoadOrCreateFromAppConfig();

            // 取出 RentApi 設定
            var rentApiBaseUrl = string.IsNullOrWhiteSpace(ext.RentApi.BaseUrl)
                ? "https://localhost:7063/"
                : ext.RentApi.BaseUrl;

            var rentApiTimeoutSeconds = ext.RentApi.TimeoutSeconds <= 0
                ? 10
                : ext.RentApi.TimeoutSeconds;

            // 保底防呆
            if (string.IsNullOrWhiteSpace(rentApiBaseUrl))
                rentApiBaseUrl = "https://localhost:7063/";

            if (rentApiTimeoutSeconds <= 0)
                rentApiTimeoutSeconds = 10;

            // 6. IJobNoApiClient（給 JobNoService 用）
            services.AddHttpClient<IJobNoApiClient, RentProject.Clients.RentProjectApiJobNoClient>(http =>
            {
                http.BaseAddress = new Uri(rentApiBaseUrl, UriKind.Absolute);
                http.Timeout = TimeSpan.FromSeconds(rentApiTimeoutSeconds);
            });

            // 7. 註冊 Form（UI）
            services.AddSingleton<Form1>();

            // 8. Build ServiceProvider
            var sp = services.BuildServiceProvider();

            // 9. 用 DI 建出 Form1（不要 new）
            // 為什麼叫 GetRequiredService？ 因為它的語氣是「一定要拿到」：
            var mainForm = sp.GetRequiredService<Form1>();
            Application.Run(mainForm);
        }
    }
}
