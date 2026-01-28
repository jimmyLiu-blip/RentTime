using Microsoft.Extensions.Options;
using RentProject.Api;
using RentProject.Api.Clients;
using RentProject.Api.Options;
using RentProject.Repository;
using RentProject.Service;

var builder = WebApplication.CreateBuilder(args);

// ===== JobNo 外部 API 設定 =====
builder.Services.Configure<JobNoApiOptions>(
    builder.Configuration.GetSection("JobNoApi"));

builder.Services.AddHttpClient<IExternalJobNoClient, ProcertJobNoApiClient>((sp, http) =>
{
    var opt = sp.GetRequiredService<IOptions<JobNoApiOptions>>().Value;

    // BaseUrl 必須是完整網址，例如 https://procert.sgs.net/
    http.BaseAddress = new Uri(opt.BaseUrl, UriKind.Absolute);

    // 避免 timeout=0 或負數
    var t = opt.TimeoutSeconds <= 0 ? 10 : opt.TimeoutSeconds;
    http.Timeout = TimeSpan.FromSeconds(t);
});

// ===== DB 連線字串 =====
var cs = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new Exception("找不到 ConnectionStrings:DefaultConnection");

// Repository 
// Scoped（每次 request 一份）是 WebAPI 常用做法
builder.Services.AddScoped<DapperRentTimeRepository>(_ => new DapperRentTimeRepository(cs));
builder.Services.AddScoped<DapperJobNoRepository>(_ => new DapperJobNoRepository(cs));
builder.Services.AddScoped<DapperHealthRepository>(_ => new DapperHealthRepository(cs));

// services 
builder.Services.AddScoped<RentTimeService>();
builder.Services.AddScoped<JobNoService>();

// ===== MVC / Swagger =====
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 放在 MapControllers 前面
app.UseMiddleware<ApiExceptionMiddleware>();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
