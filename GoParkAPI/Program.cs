using GoParkAPI;
using GoParkAPI.Models;
using GoParkAPI.Providers;
using GoParkAPI.Services;
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

//----------------------------------------


builder.Services.AddScoped<Hash>();
builder.Services.AddScoped<MailService>();
builder.Services.AddScoped<MonRentalService>();

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
app.UseCors(PolicyName);

//CROS
app.UseCors("AllowLocalhost");
app.MapControllers();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
