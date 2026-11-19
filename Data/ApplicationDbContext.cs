
using CinemaProject.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CinemaProject.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Cinema> Cinemas { get; set; }
        public DbSet<Actor> Actors { get; set; }
        public DbSet<Movie> Movies { get; set; }
        public DbSet<MovieSubImage> MovieSubImages { get; set; }
        public DbSet<CustomerAccount> CustomerAccounts { get; set; }
        public DbSet<CustomerAccount> Users => Set<CustomerAccount>();
        public DbSet<IdentityRole<int>> Roles => Set<IdentityRole<int>>();
        public DbSet<IdentityUserRole<int>> UserRoles => Set<IdentityUserRole<int>>();
        public DbSet<IdentityUserClaim<int>> UserClaims => Set<IdentityUserClaim<int>>();
        public DbSet<IdentityRoleClaim<int>> RoleClaims => Set<IdentityRoleClaim<int>>();
        public DbSet<IdentityUserLogin<int>> UserLogins => Set<IdentityUserLogin<int>>();
        public DbSet<IdentityUserToken<int>> UserTokens => Set<IdentityUserToken<int>>();
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
        public DbSet<TicketOrder> TicketOrders { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CustomerAccount>().ToTable("CustomerAccounts");
            modelBuilder.Entity<CustomerAccount>().Property(user => user.Name).HasMaxLength(120).IsRequired();
            modelBuilder.Entity<CustomerAccount>().Property(user => user.ProfileImage).HasMaxLength(400);
            modelBuilder.Entity<CustomerAccount>().Property(user => user.UserName).HasMaxLength(256);
            modelBuilder.Entity<CustomerAccount>().Property(user => user.UserName).IsRequired();
            modelBuilder.Entity<CustomerAccount>().Property(user => user.NormalizedUserName).HasMaxLength(256);
            modelBuilder.Entity<CustomerAccount>().Property(user => user.Email).HasMaxLength(256).IsRequired();
            modelBuilder.Entity<CustomerAccount>().Property(user => user.NormalizedEmail).HasMaxLength(256).IsRequired();
            modelBuilder.Entity<CustomerAccount>().Property(user => user.PasswordHash).HasMaxLength(512).IsRequired();
            modelBuilder.Entity<CustomerAccount>().Property(user => user.ConcurrencyStamp).IsConcurrencyToken();
            modelBuilder.Entity<CustomerAccount>().HasIndex(user => user.NormalizedUserName).IsUnique().HasFilter("[NormalizedUserName] IS NOT NULL");
            modelBuilder.Entity<CustomerAccount>().HasIndex(user => user.NormalizedEmail).IsUnique();
            modelBuilder.Entity<IdentityRole<int>>().ToTable("AspNetRoles");
            modelBuilder.Entity<IdentityRole<int>>().Property(role => role.Name).HasMaxLength(256);
            modelBuilder.Entity<IdentityRole<int>>().Property(role => role.NormalizedName).HasMaxLength(256);
            modelBuilder.Entity<IdentityRole<int>>().Property(role => role.ConcurrencyStamp).IsConcurrencyToken();
            modelBuilder.Entity<IdentityRole<int>>().HasIndex(role => role.NormalizedName).IsUnique().HasFilter("[NormalizedName] IS NOT NULL");
            modelBuilder.Entity<IdentityUserRole<int>>().ToTable("AspNetUserRoles").HasKey(link => new { link.UserId, link.RoleId });
            modelBuilder.Entity<IdentityUserRole<int>>().HasOne<IdentityRole<int>>().WithMany().HasForeignKey(link => link.RoleId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<IdentityUserRole<int>>().HasOne<CustomerAccount>().WithMany().HasForeignKey(link => link.UserId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<IdentityUserClaim<int>>().ToTable("AspNetUserClaims");
            modelBuilder.Entity<IdentityUserClaim<int>>().HasOne<CustomerAccount>().WithMany().HasForeignKey(claim => claim.UserId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<IdentityRoleClaim<int>>().ToTable("AspNetRoleClaims");
            modelBuilder.Entity<IdentityRoleClaim<int>>().HasOne<IdentityRole<int>>().WithMany().HasForeignKey(claim => claim.RoleId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<IdentityUserLogin<int>>().ToTable("AspNetUserLogins").HasKey(login => new { login.LoginProvider, login.ProviderKey });
            modelBuilder.Entity<IdentityUserLogin<int>>().Property(login => login.LoginProvider).HasMaxLength(128);
            modelBuilder.Entity<IdentityUserLogin<int>>().Property(login => login.ProviderKey).HasMaxLength(128);
            modelBuilder.Entity<IdentityUserLogin<int>>().HasOne<CustomerAccount>().WithMany().HasForeignKey(login => login.UserId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<IdentityUserToken<int>>().ToTable("AspNetUserTokens").HasKey(token => new { token.UserId, token.LoginProvider, token.Name });
            modelBuilder.Entity<IdentityUserToken<int>>().Property(token => token.LoginProvider).HasMaxLength(128);
            modelBuilder.Entity<IdentityUserToken<int>>().Property(token => token.Name).HasMaxLength(128);
            modelBuilder.Entity<IdentityUserToken<int>>().HasOne<CustomerAccount>().WithMany().HasForeignKey(token => token.UserId).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Movie>()
                .HasOne(m => m.Category)
                .WithMany(c => c.Movies)
                .HasForeignKey(m => m.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Movie>()
                .HasOne(m => m.Cinema)
                .WithMany(c => c.Movies)
                .HasForeignKey(m => m.CinemaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MovieSubImage>()
                .HasOne(i => i.Movie)
                .WithMany(m => m.SubImages)
                .HasForeignKey(i => i.MovieId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Movie>()
                .HasMany(m => m.Actors)
                .WithMany(a => a.Movies);

            modelBuilder.Entity<PasswordResetToken>()
                .HasIndex(token => token.TokenHash)
                .IsUnique();

            modelBuilder.Entity<PasswordResetToken>()
                .HasOne(token => token.CustomerAccount)
                .WithMany()
                .HasForeignKey(token => token.CustomerAccountId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TicketOrder>()
                .HasOne(order => order.CustomerAccount)
                .WithMany(account => account.TicketOrders)
                .HasForeignKey(order => order.CustomerAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TicketOrder>()
                .HasOne(order => order.Movie)
                .WithMany(movie => movie.TicketOrders)
                .HasForeignKey(order => order.MovieId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TicketOrder>()
                .HasIndex(order => order.StripeCheckoutSessionId)
                .IsUnique();

            modelBuilder.Entity<TicketOrder>()
                .HasIndex(order => new { order.CustomerAccountId, order.CreatedUtc });
        }
    }
}
