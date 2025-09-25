using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CertVal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRecipientAggregationModeToNotificationRule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RecipientAggregationMode",
                table: "NotificationRules",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RecipientAggregationMode",
                table: "NotificationRules");
        }
    }
}
