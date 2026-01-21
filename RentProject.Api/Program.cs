using Microsoft.Extensions.Options;
using RentProject.Api.Clients;
using RentProject.Api.Options;
using RentProject.Service;
using RentProject.Repository;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JobNoApiOptions>(
    builder.Configuration.GetSection("JobNoApi"));

builder.Services.AddHttpClient<IJobNoApiClient, ProcertJobNoApiClient>((sp, http) =>
{
    var opt = sp.GetRequiredService<IOptions<JobNoApiOptions>>().Value;

    // BaseUrl 必須是完整網址，例如 https://procert.sgs.net/
    http.BaseAddress = new Uri(opt.BaseUrl, UriKind.Absolute);

    // 避免 timeout=0 或負數
    var t = opt.TimeoutSeconds <= 0 ? 10 : opt.TimeoutSeconds;
    http.Timeout = TimeSpan.FromSeconds(t);
});

var cs = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new Exception("找不到 ConnectionStrings:DefaultConnection");

// Repository / Service（先只註冊 JobNo 這條線
// Scoped（每次 request 一份）是 WebAPI 常用做法
builder.Services.AddSingleton(new DapperJobNoRepository(cs));
builder.Services.AddScoped<JobNoService>();


// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
