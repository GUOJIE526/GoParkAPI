using GoParkAPI;
using GoParkAPI.Controllers;
using GoParkAPI.Models;
using GoParkAPI.Providers;
using GoParkAPI.Services;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<EasyParkContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("EasyPark"));
});

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = builder.Configuration.GetConnectionString("RedisConnection");
    return ConnectionMultiplexer.Connect(configuration);
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
builder.Services.AddSignalR();

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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.MapHub<ReservationHub>("/reservationHub"); // 設置 SignalR Hub 路徑
app.UseCors(PolicyName);
//CROS
app.UseCors("AllowLocalhost");
app.MapControllers();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
