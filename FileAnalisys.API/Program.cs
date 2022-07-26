using FileAnalisys.API.Infrastructure;
using FileAnalisys.BLL.Requests;
using FileAnalysis.API.Configuration;
using FileAnalysis.BLL.Services;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAutoMapper(typeof(AutoMapperApiBll)); //Registration automappers
builder.Services.AddScoped<IScannerService, ScannerService>(); // Registration service
builder.Services.AddScoped<IWebRequestCreate, MyWebRequest>(); // Injecting WebRequest
builder.Services.AddScoped<IRestClient, MyRestClient>(); //Injecting RestSharp
builder.Services.AddMemoryCache(); // Injecting MemoryCache

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthorization();
app.UseMiddleware<ErrorExceptionMiddleware>();

app.MapControllers();

app.Run();
