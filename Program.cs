using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using pw2_clase5.Data;
using System.Security.Claims;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Authentication
var keycloakSettings = builder.Configuration.GetSection("Keycloak");
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.Authority = keycloakSettings["Authority"];
    options.Audience = keycloakSettings["Audience"];
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new()
    {
        ValidateAudience = false,
        ValidateIssuer = true,
        ValidateLifetime = true,
        NameClaimType = "preferred_username",
        RoleClaimType = "realm_access"
    };

    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = context =>
        {
            var principal = context.Principal;
            var realmAccess = principal?.FindFirst("realm_access");

            if (realmAccess != null)
            {
                var roles = JsonDocument
                    .Parse(realmAccess.Value)
                    .RootElement
                    .GetProperty("roles")
                    .EnumerateArray()
                    .Select(r => r.GetString())
                    .ToList();

                var claimsIdentity = principal?.Identity as ClaimsIdentity;
                foreach (var role in roles)
                {
#pragma warning disable CS8604 // Possible null reference argument.
                    claimsIdentity?.AddClaim(new Claim(ClaimTypes.Role, role));
#pragma warning restore CS8604 // Possible null reference argument.
                }
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdminOnly", policy =>
        policy.RequireAssertion(context =>
            context.User.FindAll(ClaimTypes.Role)
                .Select(r => r.Value)
                .Contains("ADMIN")))
    .AddPolicy("AdminOrUser", policy =>
        policy.RequireAssertion(context =>
            context.User.FindAll(ClaimTypes.Role)
                .Select(r => r.Value)
                .Any(role => role == "ADMIN" || role == "USER")))
    .AddPolicy("UserOnly", policy =>
        policy.RequireAssertion(context =>
            context.User.FindAll(ClaimTypes.Role)
                .Select(r => r.Value)
                .Contains("USER")));

builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
