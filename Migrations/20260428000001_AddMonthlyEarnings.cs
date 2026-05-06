using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrudDemo.Migrations
{
    /// <inheritdoc />
    public partial class AddMonthlyEarnings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MonthlyEarnings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Month = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    LessonsCompleted = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    TotalLessonsForMonth = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    EarnedAmount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    CalculatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthlyEarnings", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            // Index unique : un seul enregistrement par utilisateur / mois / année
            migrationBuilder.CreateIndex(
                name: "UX_MonthlyEarnings_UserId_Year_Month",
                table: "MonthlyEarnings",
                columns: new[] { "UserId", "Year", "Month" },
                unique: true);

            // Index pour accéder rapidement à l'historique annuel d'un utilisateur
            migrationBuilder.CreateIndex(
                name: "IX_MonthlyEarnings_UserId_Year",
                table: "MonthlyEarnings",
                columns: new[] { "UserId", "Year" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MonthlyEarnings");
        }
    }
}
