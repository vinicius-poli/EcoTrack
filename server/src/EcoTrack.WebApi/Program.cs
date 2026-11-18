using EcoTrack.Application;
using EcoTrack.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Adiciona os serviços das camadas ao container DI
builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();

// Opcional: Configuração de CORS para o front-end
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseHttpsRedirection();

app.UseCors("CorsPolicy");

app.MapControllers();

app.Run();