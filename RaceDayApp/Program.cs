using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using RaceDayApp.Constants;
using RaceDayApp.Data;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<RaceDayDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var jwtKey = builder.Configuration["Jwt:Key"] ?? "8WCrSqPgwkXAQUR84L+ebAymV8fAzZlTRscWaqm/zaATPVPRH+f2Yw==";
var keyBytes = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "RaceDayApi",
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "RaceDayClient",
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes)
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = ctx => { Console.WriteLine("AUTH FAILED: " + ctx.Exception.Message); return Task.CompletedTask; },
        OnChallenge = ctx => { Console.WriteLine("CHALLENGE: " + ctx.Error + " / " + ctx.ErrorDescription); return Task.CompletedTask; },
        OnForbidden = ctx => {
            Console.WriteLine("FORBIDDEN (role mismatch). Roles: " +
            string.Join(",", ctx.HttpContext.User.FindAll(System.Security.Claims.ClaimTypes.Role).Select(c => c.Value))); return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("OrganiserOnly", policy => policy.RequireRole("ORGANISER"));
    options.AddPolicy("ParticipantOnly", policy => policy.RequireRole("PARTICIPANT"));
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "RaceDay API", Version = "v1" });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();