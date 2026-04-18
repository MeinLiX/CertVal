using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CertVal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAutoDeleteAndUniqueIssuerSerial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AutoDeleteExpiredCertificates",
                table: "Workspaces",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_Workspace_Issuer_Serial",
                table: "Certificates",
                columns: new[] { "WorkspaceId", "Issuer", "SerialNumber" },
                unique: true,
                filter: "\"SerialNumber\" IS NOT NULL AND \"IsBundle\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Certificates_Workspace_Issuer_Serial",
                table: "Certificates");

            migrationBuilder.DropColumn(
                name: "AutoDeleteExpiredCertificates",
                table: "Workspaces");
        }
    }
}
