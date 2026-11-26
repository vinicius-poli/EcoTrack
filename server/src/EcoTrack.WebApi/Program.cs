using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using EcoTrack.Application;
using EcoTrack.Infrastructure;
using EcoTrack.Infrastructure.Persistence;
using EcoTrack.Domain.Auth;
using EcoTrack.WebApi.Services;
using EcoTrack.Infrastructure.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;


var builder = WebApplication.CreateBuilder(args);

// Registro dos serviços das camadas
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication(builder.Configuration, builder.Logging);


// Configuração de Autenticação e JWT
builder.Services.AddScoped<IEmissorTokens, JwtTokenGenerator>();
builder.Services
    .AddOptions<JwtOptions>()
    .BindConfiguration(JwtOptions.SectionName)
    .Validate(x => !string.IsNullOrWhiteSpace(x.Issuer), "Jwt:Issuer é obrigatório.")
    .Validate(x => !string.IsNullOrWhiteSpace(x.Audience), "Jwt:Audience é obrigatório.")
    .Validate(x => Encoding.UTF8.GetByteCount(x.SigningKey) >= 32, "Jwt:SigningKey deve ter ao menos 32 bytes.")
    .Validate(x => x.ExpiresInMinutes is > 0 and <= 35791394, "Jwt:ExpiresInMinutes inválido.")
    .ValidateOnStart();

var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            NameClaimType = JwtRegisteredClaimNames.Sub,
            RoleClaimType = "role",
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        options.MapInboundClaims = false;
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var jti = context.Principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
                if (string.IsNullOrWhiteSpace(jti))
                {
                    context.Fail("Token sem identificador.");
                    return;
                }

                var revocation = context.HttpContext.RequestServices.GetRequiredService<IRevogacaoToken>();

                if (await revocation.EstaRevogadoAsync(jti, context.HttpContext.RequestAborted))
                {
                    context.Fail("Token revogado.");
                }
            }
        };
    });

builder.Services
    .AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());

builder.Services.AddControllers(options =>
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserContext, UserContext>();

// OpenAPI nativo do .NET com esquema Bearer para o botão Authorize no Swagger UI
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Name = "Authorization",
            Description = "Cole apenas o token JWT (o prefixo 'Bearer ' é adicionado automaticamente)."
        };

        document.Security ??= [];
        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        });

        return Task.CompletedTask;
    });
});

var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddPolicy("Client", policy =>
{
    if (origins.Length > 0)
    {
        policy.WithOrigins(origins);
    }

    policy.AllowAnyHeader().AllowAnyMethod();
}));

var app = builder.Build();

app.MapOpenApi().AllowAnonymous();
app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "EcoTrack API v1"));

// Aplicação automática de migrations no ambiente de desenvolvimento
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<EcoTrackDbContext>();

    if (db.Database.IsSqlServer())
    {
        db.Database.Migrate();
    }

    var mapper = scope.ServiceProvider.GetRequiredService<AutoMapper.IMapper>();
    mapper.ConfigurationProvider.AssertConfigurationIsValid();
    app.Logger.LogInformation("AutoMapper validado com sucesso.");
}

app.UseExceptionHandler(error => error.Run(async context =>
{
    context.Response.StatusCode = 500;
    context.Response.ContentType = "application/problem+json";
    await context.Response.WriteAsJsonAsync(
        new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Status = 500,
            Title = "Erro interno do servidor"
        });
}));

app.UseStatusCodePages();
app.UseHttpsRedirection();
app.UseCors("Client");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();