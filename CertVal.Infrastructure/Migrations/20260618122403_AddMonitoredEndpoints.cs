using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CertVal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMonitoredEndpoints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MonitoredEndpoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Host = table.Column<string>(type: "character varying(253)", maxLength: 253, nullable: false),
                    Port = table.Column<int>(type: "integer", nullable: false, defaultValue: 443),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CheckIntervalMinutes = table.Column<int>(type: "integer", nullable: false, defaultValue: 360),
                    LastCheckedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastReachable = table.Column<bool>(type: "boolean", nullable: true),
                    LastGrade = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    LastProtocol = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    LeafNotAfter = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LeafSubject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LeafThumbprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    LastError = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonitoredEndpoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MonitoredEndpoints_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MonitoredEndpoints_Due",
                table: "MonitoredEndpoints",
                columns: new[] { "IsEnabled", "LastCheckedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MonitoredEndpoints_Workspace_Host_Port",
                table: "MonitoredEndpoints",
                columns: new[] { "WorkspaceId", "Host", "Port" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MonitoredEndpoints_WorkspaceId",
                table: "MonitoredEndpoints",
                column: "WorkspaceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MonitoredEndpoints");
        }
    }
}
