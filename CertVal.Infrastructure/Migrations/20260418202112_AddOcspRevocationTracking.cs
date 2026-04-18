using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CertVal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOcspRevocationTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "OcspLastCheckedAt",
                table: "Certificates",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OcspLastError",
                table: "Certificates",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OcspResponderUrl",
                table: "Certificates",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OcspRevocationReason",
                table: "Certificates",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OcspRevokedAt",
                table: "Certificates",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OcspStatus",
                table: "Certificates",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_OcspLastCheckedAt",
                table: "Certificates",
                columns: new[] { "OcspLastCheckedAt", "OcspStatus" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Certificates_OcspLastCheckedAt",
                table: "Certificates");

            migrationBuilder.DropColumn(
                name: "OcspLastCheckedAt",
                table: "Certificates");

            migrationBuilder.DropColumn(
                name: "OcspLastError",
                table: "Certificates");

            migrationBuilder.DropColumn(
                name: "OcspResponderUrl",
                table: "Certificates");

            migrationBuilder.DropColumn(
                name: "OcspRevocationReason",
                table: "Certificates");

            migrationBuilder.DropColumn(
                name: "OcspRevokedAt",
                table: "Certificates");

            migrationBuilder.DropColumn(
                name: "OcspStatus",
                table: "Certificates");
        }
    }
}
