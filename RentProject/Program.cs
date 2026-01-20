using Microsoft.Extensions.DependencyInjection;
using RentProject.Repository;
using RentProject.Service;
using System;
using System.Configuration;
using System.Linq;
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

            // 23 先檢查 ConnectionStrings 這個集合本身在不在
            var connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

            services.AddSingleton<string>(connectionString);

            // 4.註冊 Repositories（Dapper 連 DB）
            services.AddSingleton<DapperRentTimeRepository>(sp => new DapperRentTimeRepository(connectionString));
            services.AddSingleton<DapperProjectRepository>(sp => new DapperProjectRepository(connectionString));
            services.AddSingleton<DapperJobNoRepository>(sp => new DapperJobNoRepository(connectionString));
            // 5.註冊 Services（商業邏輯層）
            // 當有人需要 IJobNoApiClient 時，請給他 FakeJobNoApiClient 的實例
            services.AddSingleton<RentTimeService>();
            services.AddSingleton<ProjectService>();
            services.AddSingleton<JobNoService>();

            // 6. 註冊 API Client（先用 Fake，確保可編譯可跑）
            services.AddSingleton<IJobNoApiClient, FakeJobNoApiClient>();

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
