using Microsoft.EntityFrameworkCore;
using PagamentosApp.API.Contexts;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddRouting(map => map.LowercaseUrls = true);
builder.Services.AddEndpointsApiExplorer(); //Swagger
builder.Services.AddSwaggerGen(); //Swagger

//Injeção de dependência da classe DataContext
builder.Services.AddDbContext<DataContext>
    (options => options.UseSqlServer
        (builder.Configuration.GetConnectionString("BDPagamentos")));

//Configuração para geração dos LOGS
Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                    .Enrich.FromLogContext()
                    .WriteTo.Console()
                    .WriteTo.Seq("http://localhost:5341")
                    .CreateLogger();

builder.Host.UseSerilog();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseSwagger(); //Swagger
app.UseSwaggerUI(); //Swagger
app.MapScalarApiReference(s => s.WithTheme(ScalarTheme.BluePlanet)); //Scalar

app.UseAuthorization();
app.MapControllers();
app.Run();