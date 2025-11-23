using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CertVal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCertificateSkippingAndVersioning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSkipped",
                table: "Certificates",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "PreviousCertificateId",
                table: "Certificates",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_PreviousCertificateId",
                table: "Certificates",
                column: "PreviousCertificateId");

            migrationBuilder.AddForeignKey(
                name: "FK_Certificates_Certificates_PreviousCertificateId",
                table: "Certificates",
                column: "PreviousCertificateId",
                principalTable: "Certificates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Certificates_Certificates_PreviousCertificateId",
                table: "Certificates");

            migrationBuilder.DropIndex(
                name: "IX_Certificates_PreviousCertificateId",
                table: "Certificates");

            migrationBuilder.DropColumn(
                name: "IsSkipped",
                table: "Certificates");

            migrationBuilder.DropColumn(
                name: "PreviousCertificateId",
                table: "Certificates");
        }
    }
}
