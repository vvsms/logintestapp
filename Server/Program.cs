// Server/Program.cs
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
// Your namespaces
using Server.Data;              // AppDbContext
using Server.Hubs;              // MenuHub
using Server.Interfaces;
using Server.Models;            // ApplicationUser
using Server.Services;          // ITokenService, TokenService, IMenuService, MenuService
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;
var env = builder.Environment;

// -----------------------------
// 1) EF Core + Identity
// -----------------------------
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    // Use your preferred provider. Example: SQL Server
    options.UseSqlServer(config.GetConnectionString("DefaultConnection"));
    // For SQLite (alternative):
    // options.UseSqlite(config.GetConnectionString("DefaultConnection"));
});

// Minimal Identity setup (no UI; JWT-based auth)
builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        // Password options – adjust for production policy
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = false;
        // Lockout etc. are optional
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

// -----------------------------
// 2) CORS (Blazor WASM front-end)
// -----------------------------
const string CorsPolicyName = "Frontend";
var frontendOrigins = (config["Cors:Origins"] ?? "https://localhost:5002")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
    {
        policy.WithOrigins(frontendOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // needed for cookies (refresh token)
    });
});

// -----------------------------
// 3) JWT Authentication
// -----------------------------
var issuer = config["Jwt:Issuer"] ?? "your-issuer";
var audience = config["Jwt:Audience"] ?? "your-audience";
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"] ?? "dev-secret-change-me"));

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        // Standard API bearer config
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,

            ValidateAudience = true,
            ValidAudience = audience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30) // keep small for tight expiry windows
        };

        // Enable JWT auth for SignalR (access_token via query string)
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // When the request is for our SignalR hub, read token from the query string
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/menu"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    // Example policies
    options.AddPolicy("ManageMenus", p =>
    {
        // Either role Admin or a specific permission claim
        p.RequireAssertion(ctx =>
            ctx.User.IsInRole("Admin") ||
            ctx.User.HasClaim("permission", "menus.manage"));
    });

    options.AddPolicy("ViewReports", p =>
    {
        p.RequireClaim("permission", "reports.view");
    });
});

// -----------------------------
// 4) MVC Controllers + ProblemDetails
// -----------------------------
builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        // Keep enums as strings etc. if you prefer
        // o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        o.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = ctx =>
    {
        ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier;
        if (env.IsDevelopment())
        {
            // add extra debug info during development if you like
        }
    };
});

// -----------------------------
// 5) SignalR
// -----------------------------
builder.Services.AddSignalR();

// -----------------------------
// 6) App Services (DI)
// -----------------------------
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IMenuService, MenuService>();

// -----------------------------
// 7) Swagger (useful in dev)
// -----------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "API", Version = "v1" });

    // Add JWT bearer to Swagger UI
    var jwtScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT Bearer token **_only_**"
    };
    c.AddSecurityDefinition("Bearer", jwtScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            jwtScheme, Array.Empty<string>()
        }
    });
});

// -----------------------------
// 8) Forwarded Headers (if behind reverse proxy / Kestrel + Nginx etc.)
// -----------------------------
builder.Services.Configure<ForwardedHeadersOptions>(opts =>
{
    opts.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // opts.KnownProxies.Add(IPAddress.Parse("xxx.xxx.xxx.xxx"));
});

// -----------------------------
// 9) Build
// -----------------------------
var app = builder.Build();

// -----------------------------
// 10) Middleware Pipeline
// -----------------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    // HSTS recommended in production (requires HTTPS)
    app.UseHsts();
}

// Global exception → RFC 7807 ProblemDetails (maps to /error by default)
app.UseExceptionHandler(_ => { }); // AddProblemDetails() plugs in automatic responses

app.UseHttpsRedirection();

app.UseForwardedHeaders();

app.UseCors(CorsPolicyName);

app.UseAuthentication();
app.UseAuthorization();

// Map controllers and SignalR hubs
app.MapControllers();
app.MapHub<MenuHub>("/hubs/menu")
   // If your hub requires auth to connect, add:
   .RequireAuthorization();

// (Optional) health checks
// app.MapHealthChecks("/health");

// -----------------------------
// 11) Run
// -----------------------------
app.Run();
