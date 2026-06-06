using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailUpdateRequestTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_tokens_TokenId",
                schema: "core",
                table: "tokens");

            migrationBuilder.DropIndex(
                name: "IX_sessions_SessionId",
                schema: "core",
                table: "sessions");

            migrationBuilder.DropIndex(
                name: "IX_accounts_AccountId",
                schema: "core",
                table: "accounts");

            migrationBuilder.CreateTable(
                name: "email_update_requests",
                schema: "core",
                columns: table => new
                {
                    EmailUpdateRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    OldEmail = table.Column<string>(type: "text", nullable: false),
                    NewEmail = table.Column<string>(type: "text", nullable: false),
                    RequestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_update_requests", x => x.EmailUpdateRequestId);
                    table.ForeignKey(
                        name: "FK_email_update_requests_accounts_AccountId",
                        column: x => x.AccountId,
                        principalSchema: "core",
                        principalTable: "accounts",
                        principalColumn: "AccountId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_email_update_requests_AccountId",
                schema: "core",
                table: "email_update_requests",
                column: "AccountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "email_update_requests",
                schema: "core");

            migrationBuilder.CreateIndex(
                name: "IX_tokens_TokenId",
                schema: "core",
                table: "tokens",
                column: "TokenId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sessions_SessionId",
                schema: "core",
                table: "sessions",
                column: "SessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_accounts_AccountId",
                schema: "core",
                table: "accounts",
                column: "AccountId",
                unique: true);
        }
    }
}
