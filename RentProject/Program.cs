using DevExpress.XtraEditors;
using Microsoft.Extensions.DependencyInjection;
using RentProject.Clients;
using RentProject.Settings;
using RentProject.UI.Http;
using System;
using System.Threading.Tasks;
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

            // ====== 全域例外攔截 ======
            // 告訴 WinForms：請把 UI 執行緒的未處理例外交給 Application.ThreadException，不要讓 CLR 預設處理直接中止。
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            // 會抓到：UI 執行緒 上沒有被處理的例外（例如按鈕 Click 事件裡丟出的例外，或某些控制項事件）。
            Application.ThreadException += (s, e) =>
            {
                XtraMessageBox.Show(e.Exception.ToString(), "UI ThreadException");
            };

            // 會抓到：非 UI 執行緒（背景執行緒）最後仍然沒人處理的例外。
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                XtraMessageBox.Show(ex?.ToString() ?? e.ExceptionObject?.ToString() ?? "(null)", "UnhandledException");
            };

            // 會抓到：Task 的例外沒被 await / Wait 觀察到，之後被 GC 回收時冒出來的「未觀察例外」。
            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                XtraMessageBox.Show(e.Exception.ToString(), "UnobservedTaskException");
                e.SetObserved();
            };

            // 1. 建 DI 容器
            var services = new ServiceCollection();

            // 先把『模組的製造方法』登記在工廠的系統裡，工廠才知道要去哪裡拿模組、怎麼造模組
            services.AddTransient<CorrelationIdHandler>();

            // 2. 註冊 API Client
            // 讀外部設定檔（不存在就用 App.config 預設值建立一份 settings.json）
            var ext = ExternalSettingsLoader.LoadOrCreateFromAppConfig();

            // 取出 RentApi 設定
            var BaseUrl = string.IsNullOrWhiteSpace(ext.RentApi.BaseUrl)
                ? "https://localhost:7063/"
                : ext.RentApi.BaseUrl;

            var timeout = ext.RentApi.TimeoutSeconds <= 0 ? 10 : ext.RentApi.TimeoutSeconds;

            // 3. IJobNoApiClient（給 JobNoService 用）
            services.AddHttpClient<IJobNoApiClient, RentProjectApiJobNoClient>(http =>
            {
                http.BaseAddress = new Uri(BaseUrl, UriKind.Absolute);
                http.Timeout = TimeSpan.FromSeconds(timeout);
            })
            .AddHttpMessageHandler<CorrelationIdHandler>();

            services.AddHttpClient<IRentTimeApiClient, RentProjectApiRentTimeClient>(http =>
            {
                http.BaseAddress = new Uri(BaseUrl, UriKind.Absolute);
                http.Timeout = TimeSpan.FromSeconds(timeout);
            })
            .AddHttpMessageHandler<CorrelationIdHandler>();

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
