using ImportadoraSonib.Data;
using ImportadoraSonib.Infrastructure.Seeds;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();

// DB (SQL Server)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity + Roles (necesario para que el seeder cree el rol Admin)
builder.Services
    .AddDefaultIdentity<IdentityUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        // Opcional: políticas de password
        // options.Password.RequireNonAlphanumeric = false;
    })
    .AddRoles<IdentityRole>()                           // ← IMPORTANTE
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Sesión
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(o =>
{
    o.Cookie.Name = ".Sonib.Session";
    o.IdleTimeout = TimeSpan.FromMinutes(20);
    o.Cookie.HttpOnly = true;
    o.Cookie.SameSite = SameSiteMode.Lax;
});

// Para leer sesión en vistas con @inject IHttpContextAccessor
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Migración + seed al arrancar
using (var scope = app.Services.CreateScope())
{
    var sp = scope.ServiceProvider;
    var db = sp.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
    await DbSeeder.SeedAsync(db, sp); // pasa ServiceProvider para crear rol/usuario
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

// Rutas: área Admin y sitio público
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Catalog}/{action=Index}/{id?}");

app.MapRazorPages(); // Requerido si usas la UI de Identity
app.Run();
