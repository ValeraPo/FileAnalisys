using FileAnalysis.API;
using FileAnalysis.API.Configuration;
using FileAnalysis.BLL.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.RegisterSwaggerGen(); // Settings swagger
builder.Services.AddAutoMapper(typeof(AutoMapperApiBll)); //Registration automappers
builder.Services.AddScoped<IProcessService, ProcessService>(); // Registration service
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
