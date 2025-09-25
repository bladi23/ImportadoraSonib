using System.Text;
using ImportadoraSonib.Data;
using ImportadoraSonib.Infrastructure.Seeds;
using ImportadoraSonib.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

// ───────────────── Swagger + JWT (Bearer) ─────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ImportadoraSonib API",
        Version = "v1",
        Description = "API de catálogo, carrito, órdenes y administración."
    });

    var scheme = new OpenApiSecurityScheme
    {
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Description = "Pega tu token JWT (con o sin el prefijo 'Bearer ').",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };
    c.AddSecurityDefinition("Bearer", scheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement { { scheme, Array.Empty<string>() } });
});

// ───────────────── Controllers ─────────────────
builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.ReferenceHandler =
        System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});

// ───────────────── DB ─────────────────
builder.Services.AddDbContext<ApplicationDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ───────────────── Identity ─────────────────
builder.Services.AddIdentity<IdentityUser, IdentityRole>(opt =>
{
    opt.SignIn.RequireConfirmedAccount = false;
    opt.User.RequireUniqueEmail = true;
    opt.Password.RequiredLength = 8;
    opt.Lockout.MaxFailedAccessAttempts = 5;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// ───────────────── JWT ─────────────────
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
    throw new InvalidOperationException("Falta Jwt:Key en appsettings.");

var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "ImportadoraSonib";
var jwtSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = false,
        IssuerSigningKey = jwtSigningKey,
        ValidateIssuerSigningKey = true,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30),
        NameClaimType = System.Security.Claims.ClaimTypes.NameIdentifier,
        RoleClaimType = System.Security.Claims.ClaimTypes.Role
    };
});

// ───────────────── CORS (Angular dev) ─────────────────
// Incluye http y https por si usas 'ng serve --ssl'
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("ng", p => p
        .WithOrigins("http://localhost:4200", "https://localhost:4200")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

// ───────────────── Autorización (políticas opcionales) ─────────────────
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
    options.AddPolicy("CustomerOnly", p => p.RequireRole("Customer"));
});

// ───────────────── Session (para carrito por sesión) ─────────────────
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(o =>
{
    o.Cookie.Name = ".Sonib.Session";
    o.IdleTimeout = TimeSpan.FromMinutes(20);
    o.Cookie.HttpOnly = true;
    o.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.None; 
    o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});


builder.Services.AddHttpContextAccessor();

// ───────────────── Cache + Estampilla de catálogo ─────────────────
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<CatalogCacheStamp>();

// ───────────────── Servicios propios ─────────────────
builder.Services.AddScoped<CartService>();
builder.Services.AddSingleton<WhatsappLinkService>();

var app = builder.Build();

// ───────────────── Migración + Seed (roles, admin, datos mínimos) ─────────────────
using (var scope = app.Services.CreateScope())
{
    var sp = scope.ServiceProvider;
    var db = sp.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
    await DbSeeder.SeedAsync(db, sp, app.Configuration);
}

// ───────────────── Archivos estáticos (uploads) ─────────────────
var www = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
var up = Path.Combine(www, "uploads", "products");
Directory.CreateDirectory(up);

// ───────────────── Encabezados reenviados (si hay proxy/containers) ─────────────────
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        var path = ctx.File?.PhysicalPath;
        if (path != null && path.Contains(Path.Combine("wwwroot", "uploads", "products")))
        {
            var headers = ctx.Context.Response.GetTypedHeaders();
            headers.CacheControl = new Microsoft.Net.Http.Headers.CacheControlHeaderValue
            {
                Public = true,
                MaxAge = TimeSpan.FromDays(30)
            };
            headers.CacheControl.Extensions.Add(new NameValueHeaderValue("immutable"));
        }
    }
});
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("ng");
app.UseSession();

//app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

// (Opcional) redirige raíz a Swagger
app.MapGet("/", () => Results.Redirect("/swagger"));

app.MapControllers();

app.Run();
