using CinemaProject.Data;
using CinemaProject.Models;
using CinemaProject.Repositories;
using CinemaProject.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace TaskCinema;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddControllersWithViews();

        var connectionString = builder.Configuration.GetConnectionString("CinemaProject")
            ?? throw new InvalidOperationException("Connection string 'CinemaProject' was not found.");

        builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
        builder.Services.AddScoped<IUserStore<CustomerAccount>, IdentityUserStore>();
        builder.Services.AddScoped<IRoleStore<IdentityRole<int>>, IdentityRoleStore>();
        builder.Services.AddIdentity<CustomerAccount, IdentityRole<int>>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Lockout.AllowedForNewUsers = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        })
        .AddDefaultTokenProviders()
        .AddClaimsPrincipalFactory<CinemaUserClaimsPrincipalFactory>();

        builder.Services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = "CineVerse.Auth";
            options.LoginPath = "/Customer/Account/Login";
            options.AccessDeniedPath = "/Customer/Account/AccessDenied";
            options.ExpireTimeSpan = TimeSpan.FromDays(14);
            options.SlidingExpiration = true;
        });
        builder.Services.Configure<SecurityStampValidatorOptions>(options =>
            options.ValidationInterval = TimeSpan.FromMinutes(2));

        builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
        builder.Services.AddScoped<IPublicMovieCatalog, PublicMovieCatalog>();
        builder.Services.AddScoped<IAdminMovieService, AdminMovieService>();
        builder.Services.AddScoped<ICategoryService, CategoryService>();
        builder.Services.AddScoped<IMovieImageStorage, LocalMovieImageStorage>();
        builder.Services.AddScoped<IAccountService, AccountService>();
        builder.Services.AddScoped<IProfileImageStorage, LocalProfileImageStorage>();
        builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
        builder.Services.Configure<SmtpEmailOptions>(builder.Configuration.GetSection("Email:Smtp"));
        builder.Services.AddHttpClient<IStripeCheckoutService, StripeCheckoutService>();

        var app = builder.Build();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.MapStaticAssets();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();

        await using (var scope = app.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            if (app.Environment.IsDevelopment())
            {
                await dbContext.Database.MigrateAsync();
            }
            await IdentitySeeder.SeedAsync(
                scope.ServiceProvider,
                builder.Configuration,
                syncBootstrapPasswords: app.Environment.IsDevelopment());
        }

        app.MapControllers();
        app.MapControllerRoute(
            name: "default",
            pattern: "{area=Customer}/{controller=Home}/{action=Index}/{id?}")
            .WithStaticAssets();

        await app.RunAsync();
    }
}
