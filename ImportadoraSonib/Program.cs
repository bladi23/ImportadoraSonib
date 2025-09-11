using System.Text;
using ImportadoraSonib.Data;
using ImportadoraSonib.Infrastructure.Seeds;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Controllers (sin vistas)
builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});

// DB
builder.Services.AddDbContext<ApplicationDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity + Roles + JWT (API)
builder.Services.AddIdentity<IdentityUser, IdentityRole>(opt =>
{
    opt.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// JWT
var jwtKey = builder.Configuration["Jwt:Key"]!;
var jwtIssuer = builder.Configuration["Jwt:Issuer"]!;
var jwtSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = true;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = false,
        IssuerSigningKey = jwtSigningKey,
        ValidateIssuerSigningKey = true,
        ValidateLifetime = true
    };
});

// CORS para Angular en local
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("ng",
        p => p.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

// Session (solo para demostrar lastCategory si quieres)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(o =>
{
    o.Cookie.Name = ".Sonib.Session";
    o.IdleTimeout = TimeSpan.FromMinutes(20);
    o.Cookie.HttpOnly = true;
    o.Cookie.SameSite = SameSiteMode.Lax;
});
builder.Services.AddHttpContextAccessor();

// Servicios propios
builder.Services.AddScoped<ImportadoraSonib.Services.CartService>();

var app = builder.Build();

// Migración + seed
using (var scope = app.Services.CreateScope())
{
    var sp = scope.ServiceProvider;
    var db = sp.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
    await DbSeeder.SeedAsync(db, sp);
}

app.UseSwagger();
app.UseSwaggerUI();

// Middlewares
app.UseHttpsRedirection();
app.UseCors("ng");
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers(); // ← Solo API

app.Run();
