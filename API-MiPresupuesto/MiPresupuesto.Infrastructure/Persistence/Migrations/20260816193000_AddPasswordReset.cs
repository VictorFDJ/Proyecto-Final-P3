using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MiPresupuesto.Infrastructure.Persistence;

#nullable disable

namespace MiPresupuesto.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260816193000_AddPasswordReset")]
public partial class AddPasswordReset : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "PasswordResetTokenExpiresAtUtc",
            table: "Users",
            type: "datetime2",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "PasswordResetTokenHash",
            table: "Users",
            type: "nvarchar(64)",
            maxLength: 64,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "PasswordResetTokenExpiresAtUtc",
            table: "Users");

        migrationBuilder.DropColumn(
            name: "PasswordResetTokenHash",
            table: "Users");
    }
}
