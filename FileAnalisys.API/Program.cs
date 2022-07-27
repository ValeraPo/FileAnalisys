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
builder.Services.AddScoped<IWebRequestCreate, MyWebRequest>(); // Registration WebRequest
builder.Services.AddScoped<IRestClient, MyRestClient>(); //Registration RestSharp
builder.Services.AddMemoryCache(); // Registration MemoryCache

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseMiddleware<ErrorExceptionMiddleware>();

app.MapControllers();

app.Run();
