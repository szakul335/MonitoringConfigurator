using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MonitoringConfigurator.Data;
using MonitoringConfigurator.Services;

var builder = WebApplication.CreateBuilder(args);

// Lokalizacja PL
var supportedCultures = new[] { new CultureInfo("pl-PL") };
builder.Services.Configure<RequestLocalizationOptions>(options => {
    options.DefaultRequestCulture = new RequestCulture("pl-PL");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});
CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("pl-PL");
CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("pl-PL");


builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection") ??
        "Server=(localdb)\\mssqllocaldb;Database=MonitoringConfiguratorDb;Trusted_Connection=True;MultipleActiveResultSets=true"));

builder.Services
    .AddDefaultIdentity<IdentityUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequiredLength = 6;
    })
    .AddRoles<IdentityRole>()                  
    .AddEntityFrameworkStores<AppDbContext>();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddScoped<IConfiguratorService, ConfiguratorService>();

var app = builder.Build();


using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
{
    await DbSeeder.SeedAsync(services);
}
catch (Exception ex)
{
   
    using var scope2 = app.Services.CreateScope();
    var ctx2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
    await ctx2.Database.MigrateAsync();
}
}


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}



using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

    if (!await roleManager.RoleExistsAsync("Admin"))
        await roleManager.CreateAsync(new IdentityRole("Admin"));
    if (!await roleManager.RoleExistsAsync("Operator"))
        await roleManager.CreateAsync(new IdentityRole("Operator"));

    
    var adminUser = await userManager.FindByEmailAsync("admin@demo.pl");
    if (adminUser != null && !await userManager.IsInRoleAsync(adminUser, "Admin"))
        await userManager.AddToRoleAsync(adminUser, "Admin");

    var operatorUser = await userManager.FindByEmailAsync("operator@demo.pl");
    if (operatorUser != null && !await userManager.IsInRoleAsync(operatorUser, "Operator"))
        await userManager.AddToRoleAsync(operatorUser, "Operator");
}

var locOptions = app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value;
app.UseRequestLocalization(locOptions);

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
