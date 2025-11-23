using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CertVal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCertificateDeleteBehavior : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Certificates_Certificates_ParentCertificateId",
                table: "Certificates");

            migrationBuilder.DropForeignKey(
                name: "FK_Certificates_Certificates_PreviousCertificateId",
                table: "Certificates");

            migrationBuilder.AddForeignKey(
                name: "FK_Certificates_Certificates_ParentCertificateId",
                table: "Certificates",
                column: "ParentCertificateId",
                principalTable: "Certificates",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Certificates_Certificates_PreviousCertificateId",
                table: "Certificates",
                column: "PreviousCertificateId",
                principalTable: "Certificates",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Certificates_Certificates_ParentCertificateId",
                table: "Certificates");

            migrationBuilder.DropForeignKey(
                name: "FK_Certificates_Certificates_PreviousCertificateId",
                table: "Certificates");

            migrationBuilder.AddForeignKey(
                name: "FK_Certificates_Certificates_ParentCertificateId",
                table: "Certificates",
                column: "ParentCertificateId",
                principalTable: "Certificates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Certificates_Certificates_PreviousCertificateId",
                table: "Certificates",
                column: "PreviousCertificateId",
                principalTable: "Certificates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
