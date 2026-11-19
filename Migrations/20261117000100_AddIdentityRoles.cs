using CinemaProject.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaProject.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261117000100_AddIdentityRoles")]
public partial class AddIdentityRoles : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "UserName", table: "CustomerAccounts", type: "nvarchar(256)", maxLength: 256, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>(name: "NormalizedUserName", table: "CustomerAccounts", type: "nvarchar(256)", maxLength: 256, nullable: true);
        migrationBuilder.AddColumn<bool>(name: "EmailConfirmed", table: "CustomerAccounts", type: "bit", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<string>(name: "PhoneNumber", table: "CustomerAccounts", type: "nvarchar(max)", nullable: true);
        migrationBuilder.AddColumn<bool>(name: "PhoneNumberConfirmed", table: "CustomerAccounts", type: "bit", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<bool>(name: "TwoFactorEnabled", table: "CustomerAccounts", type: "bit", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<DateTimeOffset>(name: "LockoutEnd", table: "CustomerAccounts", type: "datetimeoffset", nullable: true);
        migrationBuilder.AddColumn<bool>(name: "LockoutEnabled", table: "CustomerAccounts", type: "bit", nullable: false, defaultValue: true);
        migrationBuilder.AddColumn<int>(name: "AccessFailedCount", table: "CustomerAccounts", type: "int", nullable: false, defaultValue: 0);
        migrationBuilder.AddColumn<string>(name: "SecurityStamp", table: "CustomerAccounts", type: "nvarchar(max)", nullable: true);
        migrationBuilder.AddColumn<string>(name: "ConcurrencyStamp", table: "CustomerAccounts", type: "nvarchar(max)", nullable: true);

        migrationBuilder.Sql("UPDATE [CustomerAccounts] SET [UserName] = [Email], [NormalizedUserName] = [NormalizedEmail], [SecurityStamp] = CONVERT(nvarchar(36), NEWID()), [ConcurrencyStamp] = CONVERT(nvarchar(36), NEWID()) WHERE [Email] IS NOT NULL");

        migrationBuilder.CreateTable(
            name: "AspNetRoles",
            columns: table => new
            {
                Id = table.Column<int>(type: "int").Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_AspNetRoles", x => x.Id));

        migrationBuilder.CreateTable(
            name: "AspNetUserClaims",
            columns: table => new
            {
                Id = table.Column<int>(type: "int").Annotation("SqlServer:Identity", "1, 1"),
                UserId = table.Column<int>(type: "int", nullable: false),
                ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                table.ForeignKey("FK_AspNetUserClaims_CustomerAccounts_UserId", x => x.UserId, "CustomerAccounts", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "AspNetUserLogins",
            columns: table => new
            {
                LoginProvider = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                ProviderKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                UserId = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                table.ForeignKey("FK_AspNetUserLogins_CustomerAccounts_UserId", x => x.UserId, "CustomerAccounts", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "AspNetUserRoles",
            columns: table => new
            {
                UserId = table.Column<int>(type: "int", nullable: false),
                RoleId = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                table.ForeignKey("FK_AspNetUserRoles_CustomerAccounts_UserId", x => x.UserId, "CustomerAccounts", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_AspNetUserRoles_AspNetRoles_RoleId", x => x.RoleId, "AspNetRoles", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "AspNetUserTokens",
            columns: table => new
            {
                UserId = table.Column<int>(type: "int", nullable: false),
                LoginProvider = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                table.ForeignKey("FK_AspNetUserTokens_CustomerAccounts_UserId", x => x.UserId, "CustomerAccounts", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "AspNetRoleClaims",
            columns: table => new
            {
                Id = table.Column<int>(type: "int").Annotation("SqlServer:Identity", "1, 1"),
                RoleId = table.Column<int>(type: "int", nullable: false),
                ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                table.ForeignKey("FK_AspNetRoleClaims_AspNetRoles_RoleId", x => x.RoleId, "AspNetRoles", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(name: "IX_CustomerAccounts_NormalizedUserName", table: "CustomerAccounts", column: "NormalizedUserName", unique: true, filter: "[NormalizedUserName] IS NOT NULL");
        migrationBuilder.CreateIndex(name: "IX_AspNetRoles_NormalizedName", table: "AspNetRoles", column: "NormalizedName", unique: true, filter: "[NormalizedName] IS NOT NULL");
        migrationBuilder.CreateIndex(name: "IX_AspNetRoleClaims_RoleId", table: "AspNetRoleClaims", column: "RoleId");
        migrationBuilder.CreateIndex(name: "IX_AspNetUserClaims_UserId", table: "AspNetUserClaims", column: "UserId");
        migrationBuilder.CreateIndex(name: "IX_AspNetUserLogins_UserId", table: "AspNetUserLogins", column: "UserId");
        migrationBuilder.CreateIndex(name: "IX_AspNetUserRoles_RoleId", table: "AspNetUserRoles", column: "RoleId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AspNetRoleClaims");
        migrationBuilder.DropTable(name: "AspNetUserClaims");
        migrationBuilder.DropTable(name: "AspNetUserLogins");
        migrationBuilder.DropTable(name: "AspNetUserRoles");
        migrationBuilder.DropTable(name: "AspNetUserTokens");
        migrationBuilder.DropTable(name: "AspNetRoles");
        migrationBuilder.DropIndex(name: "IX_CustomerAccounts_NormalizedUserName", table: "CustomerAccounts");
        migrationBuilder.DropColumn(name: "UserName", table: "CustomerAccounts");
        migrationBuilder.DropColumn(name: "NormalizedUserName", table: "CustomerAccounts");
        migrationBuilder.DropColumn(name: "EmailConfirmed", table: "CustomerAccounts");
        migrationBuilder.DropColumn(name: "PhoneNumber", table: "CustomerAccounts");
        migrationBuilder.DropColumn(name: "PhoneNumberConfirmed", table: "CustomerAccounts");
        migrationBuilder.DropColumn(name: "TwoFactorEnabled", table: "CustomerAccounts");
        migrationBuilder.DropColumn(name: "LockoutEnd", table: "CustomerAccounts");
        migrationBuilder.DropColumn(name: "LockoutEnabled", table: "CustomerAccounts");
        migrationBuilder.DropColumn(name: "AccessFailedCount", table: "CustomerAccounts");
        migrationBuilder.DropColumn(name: "SecurityStamp", table: "CustomerAccounts");
        migrationBuilder.DropColumn(name: "ConcurrencyStamp", table: "CustomerAccounts");
    }

    protected override void BuildTargetModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "10.0.14");
    }
}
