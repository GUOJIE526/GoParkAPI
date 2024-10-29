using GoParkAPI;
using GoParkAPI.Models;
using GoParkAPI.Providers;
using GoParkAPI.Services;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<EasyParkContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("EasyPark"));
});

builder.Services.AddHttpClient();

string PolicyName = "EasyParkCors";
builder.Services.AddCors(options =>
{
    options.AddPolicy(PolicyName, policy =>
    {
        policy.WithOrigins("*").WithMethods("*").WithHeaders("*");
    });
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost", policy =>
    {
        policy.WithOrigins("http://127.0.0.1:5500", "http://localhost:5173")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
//----------------------------------------

// 註冊 JsonProvider 作為 Singleton 服務
builder.Services.AddSingleton<JsonProvider>();

// 註冊 LinePayService 並使用 IHttpClientFactory
builder.Services.AddHttpClient<LinePayService>();

builder.Services.AddScoped<MyPayService>();

//----------------------------------------


builder.Services.AddScoped<pwdHash>();
builder.Services.AddScoped<MailService>();
builder.Services.AddScoped<MonRentalService>();

// 配置 Hangfire，並設置使用 SQL Server 作為儲存
builder.Services.AddHangfire(config =>
{
    config.SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
          .UseSimpleAssemblyNameTypeSerializer()
          .UseDefaultTypeSerializer()
          .UseSqlServerStorage(builder.Configuration.GetConnectionString("EasyPark"), new SqlServerStorageOptions
          {
              CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
              SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
              QueuePollInterval = TimeSpan.Zero,
              UseRecommendedIsolationLevel = true,
              DisableGlobalLocks = true
          });
});

// 啟用 Hangfire 服務
builder.Services.AddHangfireServer();


//VAPID設置
var vapidConfig = new VapidConfig(
  publicKey: "BEOC-kXHgoTOx9oB89JAGbgZxr2w_IXEc_G4_0PACRCJOFtfx4hoT0hxslv1aGGmCSbrzpV-NSexuMjYuCyoMAM",
  privateKey: "MHnqygHOGLthp9ydqXz6r7Lpmpy1ZdlqzkMHFq0tI80", subject: "mailto:hungkaojay@gmail.com");
builder.Services.AddSingleton(vapidConfig); //注入 VapidConfig
builder.Services.AddScoped<PushNotificationService>(); //注入推播

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
// 啟用 Hangfire Dashboard
app.UseHangfireDashboard();
// 在應用啟動時設置 Recurring Job
//RecurringJob.AddOrUpdate<PushNotificationService>("CheckAndSendOverdueReminder", service => service.CheckAndSendOverdueReminder(), "*/2 * * * *"); // 每隔5分鐘執行一次

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors(PolicyName);

//CROS
app.UseCors("AllowLocalhost");
app.MapControllers();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
