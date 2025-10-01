using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sigma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantIdToWebhookEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WebhookEvents_Platform_EventId",
                table: "WebhookEvents");

            migrationBuilder.RenameColumn(
                name: "UpdatedAtUtc",
                table: "WebhookEvents",
                newName: "updated_at_utc");

            migrationBuilder.RenameColumn(
                name: "UpdatedAtUtc",
                table: "OutboxMessages",
                newName: "updated_at_utc");

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "WebhookEvents",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_WebhookEvents_Platform_EventId_TenantId",
                table: "WebhookEvents",
                columns: new[] { "Platform", "EventId", "TenantId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WebhookEvents_Platform_EventId_TenantId",
                table: "WebhookEvents");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "WebhookEvents");

            migrationBuilder.RenameColumn(
                name: "updated_at_utc",
                table: "WebhookEvents",
                newName: "UpdatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "updated_at_utc",
                table: "OutboxMessages",
                newName: "UpdatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookEvents_Platform_EventId",
                table: "WebhookEvents",
                columns: new[] { "Platform", "EventId" },
                unique: true);
        }
    }
}
