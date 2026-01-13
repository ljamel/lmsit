using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrudDemo.Migrations
{
    /// <inheritdoc />
    public partial class AddMattermostFieldsToSubscription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MattermostUserId",
                table: "Subscriptions",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "MattermostCreatedAt",
                table: "Subscriptions",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MattermostUserId",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "MattermostCreatedAt",
                table: "Subscriptions");
        }
    }
}
