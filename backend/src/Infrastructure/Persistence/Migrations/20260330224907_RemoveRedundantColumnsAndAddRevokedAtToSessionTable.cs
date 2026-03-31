using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRedundantColumnsAndAddRevokedAtToSessionTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsExpired",
                schema: "core",
                table: "sessions");

            migrationBuilder.DropColumn(
                name: "HasVerifiedEmail",
                schema: "core",
                table: "accounts");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RevokedAt",
                schema: "core",
                table: "sessions",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RevokedAt",
                schema: "core",
                table: "sessions");

            migrationBuilder.AddColumn<bool>(
                name: "IsExpired",
                schema: "core",
                table: "sessions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasVerifiedEmail",
                schema: "core",
                table: "accounts",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
