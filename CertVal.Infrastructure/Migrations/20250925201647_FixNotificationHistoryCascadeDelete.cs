using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CertVal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixNotificationHistoryCascadeDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NotificationHistory_Certificates_CertificateId",
                table: "NotificationHistory");

            migrationBuilder.AddForeignKey(
                name: "FK_NotificationHistory_Certificates_CertificateId",
                table: "NotificationHistory",
                column: "CertificateId",
                principalTable: "Certificates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NotificationHistory_Certificates_CertificateId",
                table: "NotificationHistory");

            migrationBuilder.AddForeignKey(
                name: "FK_NotificationHistory_Certificates_CertificateId",
                table: "NotificationHistory",
                column: "CertificateId",
                principalTable: "Certificates",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
