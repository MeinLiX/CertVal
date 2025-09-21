using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CertVal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StoredEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EventData = table.Column<string>(type: "text", nullable: false),
                    AggregateType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AggregateId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StoredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    Metadata = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoredEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsEmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    EmailConfirmationToken = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    EmailConfirmedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PasswordResetToken = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    PasswordResetTokenExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TimeZone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true, defaultValue: "UTC"),
                    Language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true, defaultValue: "en"),
                    EmailNotificationsEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApiTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    TokenPrefix = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Scope = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastUsedIpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApiTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Workspaces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaxCertificates = table.Column<int>(type: "integer", nullable: false, defaultValue: 1000),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    AllowMemberInvites = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workspaces", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Workspaces_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Certificates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Issuer = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SerialNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Thumbprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    NotBefore = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NotAfter = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FilePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FileFormat = table.Column<int>(type: "integer", nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    ParentCertificateId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsBundle = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Certificates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Certificates_Certificates_ParentCertificateId",
                        column: x => x.ParentCertificateId,
                        principalTable: "Certificates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Certificates_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NotificationRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    DaysBeforeExpiry = table.Column<int>(type: "integer", nullable: false),
                    Frequency = table.Column<int>(type: "integer", nullable: false),
                    ChannelType = table.Column<int>(type: "integer", nullable: false),
                    ChannelConfig = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false, defaultValue: "{}"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationRules_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkspaceMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    InvitedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    InvitedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    InvitationToken = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    InvitationTokenExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkspaceMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkspaceMembers_Users_InvitedByUserId",
                        column: x => x.InvitedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkspaceMembers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkspaceMembers_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NotificationHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NotificationRuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    CertificateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ChannelType = table.Column<int>(type: "integer", nullable: false),
                    Recipient = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    ScheduledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeliveredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    MaxRetries = table.Column<int>(type: "integer", nullable: false, defaultValue: 3),
                    ExternalId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ResponseData = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationHistory_Certificates_CertificateId",
                        column: x => x.CertificateId,
                        principalTable: "Certificates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_NotificationHistory_NotificationRules_NotificationRuleId",
                        column: x => x.NotificationRuleId,
                        principalTable: "NotificationRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApiTokens_ExpiresAt",
                table: "ApiTokens",
                column: "ExpiresAt",
                filter: "\"ExpiresAt\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ApiTokens_TokenHash",
                table: "ApiTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApiTokens_User_Active",
                table: "ApiTokens",
                columns: new[] { "UserId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ApiTokens_User_Active_Expires",
                table: "ApiTokens",
                columns: new[] { "UserId", "IsActive", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ApiTokens_UserId",
                table: "ApiTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_NotAfter",
                table: "Certificates",
                column: "NotAfter");

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_ParentId",
                table: "Certificates",
                column: "ParentCertificateId",
                filter: "\"ParentCertificateId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_Subject_Issuer",
                table: "Certificates",
                columns: new[] { "Subject", "Issuer" });

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_Thumbprint",
                table: "Certificates",
                column: "Thumbprint");

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_Workspace_Bundle",
                table: "Certificates",
                columns: new[] { "WorkspaceId", "IsBundle" });

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_Workspace_Expiry",
                table: "Certificates",
                columns: new[] { "WorkspaceId", "NotAfter" });

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_Workspace_Expiry_Status",
                table: "Certificates",
                columns: new[] { "WorkspaceId", "NotAfter", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_Workspace_Status",
                table: "Certificates",
                columns: new[] { "WorkspaceId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_WorkspaceId",
                table: "Certificates",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationHistory_CertificateId",
                table: "NotificationHistory",
                column: "CertificateId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationHistory_Processing",
                table: "NotificationHistory",
                columns: new[] { "Status", "ScheduledAt", "RetryCount" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationHistory_RuleId",
                table: "NotificationHistory",
                column: "NotificationRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationHistory_ScheduledAt",
                table: "NotificationHistory",
                column: "ScheduledAt");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationHistory_Status_Scheduled",
                table: "NotificationHistory",
                columns: new[] { "Status", "ScheduledAt" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationRules_Workspace_Enabled",
                table: "NotificationRules",
                columns: new[] { "WorkspaceId", "IsEnabled" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationRules_WorkspaceId",
                table: "NotificationRules",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_StoredEvents_Aggregate",
                table: "StoredEvents",
                columns: new[] { "AggregateType", "AggregateId" });

            migrationBuilder.CreateIndex(
                name: "IX_StoredEvents_Aggregate_OccurredAt",
                table: "StoredEvents",
                columns: new[] { "AggregateType", "AggregateId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StoredEvents_CorrelationId",
                table: "StoredEvents",
                column: "CorrelationId",
                filter: "\"CorrelationId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StoredEvents_EventId",
                table: "StoredEvents",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoredEvents_EventType",
                table: "StoredEvents",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_StoredEvents_Id",
                table: "StoredEvents",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoredEvents_OccurredAt",
                table: "StoredEvents",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_StoredEvents_StoredAt",
                table: "StoredEvents",
                column: "StoredAt");

            migrationBuilder.CreateIndex(
                name: "IX_StoredEvents_Type_OccurredAt",
                table: "StoredEvents",
                columns: new[] { "EventType", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StoredEvents_User_OccurredAt",
                table: "StoredEvents",
                columns: new[] { "UserId", "OccurredAt" },
                filter: "\"UserId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StoredEvents_UserId",
                table: "StoredEvents",
                column: "UserId",
                filter: "\"UserId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_EmailConfirmationToken",
                table: "Users",
                column: "EmailConfirmationToken",
                filter: "\"EmailConfirmationToken\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Users_PasswordResetToken",
                table: "Users",
                column: "PasswordResetToken",
                filter: "\"PasswordResetToken\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Status",
                table: "Users",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_WorkspaceMembers_InvitationToken",
                table: "WorkspaceMembers",
                column: "InvitationToken",
                filter: "\"InvitationToken\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_WorkspaceMembers_InvitedBy",
                table: "WorkspaceMembers",
                column: "InvitedByUserId",
                filter: "\"InvitedByUserId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_WorkspaceMembers_UserId",
                table: "WorkspaceMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkspaceMembers_Workspace_User",
                table: "WorkspaceMembers",
                columns: new[] { "WorkspaceId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkspaceMembers_WorkspaceId",
                table: "WorkspaceMembers",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_Workspaces_IsPublic",
                table: "Workspaces",
                column: "IsPublic");

            migrationBuilder.CreateIndex(
                name: "IX_Workspaces_Owner_Name",
                table: "Workspaces",
                columns: new[] { "OwnerId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Workspaces_OwnerId",
                table: "Workspaces",
                column: "OwnerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApiTokens");

            migrationBuilder.DropTable(
                name: "NotificationHistory");

            migrationBuilder.DropTable(
                name: "StoredEvents");

            migrationBuilder.DropTable(
                name: "WorkspaceMembers");

            migrationBuilder.DropTable(
                name: "Certificates");

            migrationBuilder.DropTable(
                name: "NotificationRules");

            migrationBuilder.DropTable(
                name: "Workspaces");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
