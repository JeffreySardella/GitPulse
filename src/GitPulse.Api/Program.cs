using System.Text;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using GitPulse.Api.Data;
using GitPulse.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Polly;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDbContext<GitPulseDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var jwtSecret = builder.Configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT secret not configured");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "gitpulse";

builder.Services.AddScoped<ITokenService>(sp =>
    new TokenService(sp.GetRequiredService<GitPulseDbContext>(), jwtSecret, jwtIssuer));

builder.Services.Configure<GitHubOptions>(builder.Configuration.GetSection("GitHub"));
builder.Services.AddHttpClient<IGitHubAuthService, GitHubAuthService>(client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd("GitPulse");
}).AddPolicyHandler(_ => Polly.Extensions.Http.HttpPolicyExtensions
    .HandleTransientHttpError()
    .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtIssuer,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddHttpClient<IGitHubDataService, GitHubDataService>()
    .AddPolicyHandler(Polly.Extensions.Http.HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(r => r.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<ISecretStore, InMemorySecretStore>();
}
else
{
    var keyVaultUri = builder.Configuration["KeyVault:Uri"]
        ?? throw new InvalidOperationException("KeyVault URI not configured");
    builder.Services.AddSingleton<ISecretStore>(
        new KeyVaultSecretStore(new SecretClient(new Uri(keyVaultUri), new DefaultAzureCredential())));
}

builder.Services.AddScoped<ISnapshotService, SnapshotService>();
builder.Services.AddScoped<ISyncService, SyncService>();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
