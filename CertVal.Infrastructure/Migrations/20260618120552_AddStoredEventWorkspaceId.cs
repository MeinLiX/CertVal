using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CertVal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStoredEventWorkspaceId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "WorkspaceId",
                table: "StoredEvents",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoredEvents_Workspace_OccurredAt",
                table: "StoredEvents",
                columns: new[] { "WorkspaceId", "OccurredAt" },
                filter: "\"WorkspaceId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StoredEvents_Workspace_OccurredAt",
                table: "StoredEvents");

            migrationBuilder.DropColumn(
                name: "WorkspaceId",
                table: "StoredEvents");
        }
    }
}
