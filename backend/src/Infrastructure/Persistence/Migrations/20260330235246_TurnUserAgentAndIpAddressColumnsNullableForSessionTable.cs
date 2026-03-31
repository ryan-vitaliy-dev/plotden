using System.Net;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TurnUserAgentAndIpAddressColumnsNullableForSessionTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "UserAgent",
                schema: "core",
                table: "sessions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            // migrationBuilder.AlterColumn<IPAddress>(
            //     name: "IpAddress",
            //     schema: "core",
            //     table: "sessions",
            //     type: "inet",
            //     nullable: true,
            //     oldClrType: typeof(string),
            //     oldType: "text");
            migrationBuilder.Sql(
                @"ALTER TABLE core.sessions
                ALTER COLUMN ""IpAddress"" TYPE inet USING ""IpAddress""::inet,
                ALTER COLUMN ""IpAddress"" DROP NOT NULL;"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "UserAgent",
                schema: "core",
                table: "sessions",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            // migrationBuilder.AlterColumn<string>(
            //     name: "IpAddress",
            //     schema: "core",
            //     table: "sessions",
            //     type: "text",
            //     nullable: false,
            //     defaultValue: "",
            //     oldClrType: typeof(IPAddress),
            //     oldType: "inet",
            //     oldNullable: true);
            migrationBuilder.Sql(
                @"ALTER TABLE core.sessions
                ALTER COLUMN ""IpAddress"" TYPE text USING ""IpAddress""::text,
                ALTER COLUMN ""IpAddress"" SET NOT NULL,
                ALTER COLUMN ""IpAddress"" SET DEFAULT '';"
            );
        }
    }
}
