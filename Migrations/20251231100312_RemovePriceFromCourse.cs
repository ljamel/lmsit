using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrudDemo.Migrations
{
    /// <inheritdoc />
    public partial class RemovePriceFromCourse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Price",
                table: "Courses");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "Courses",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
