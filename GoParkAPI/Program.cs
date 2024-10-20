using GoParkAPI;
using GoParkAPI.Models;
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


builder.Services.AddScoped<Hash>();

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
app.UseCors();
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
