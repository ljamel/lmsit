using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrudDemo.Migrations
{
    /// <inheritdoc />
    public partial class AddSelectedOptionIdToUserQuizResult : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SelectedOptionId",
                table: "UserQuizResults",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_UserQuizResults_SelectedOptionId",
                table: "UserQuizResults",
                column: "SelectedOptionId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserQuizResults_QuizOptions_SelectedOptionId",
                table: "UserQuizResults",
                column: "SelectedOptionId",
                principalTable: "QuizOptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserQuizResults_QuizOptions_SelectedOptionId",
                table: "UserQuizResults");

            migrationBuilder.DropIndex(
                name: "IX_UserQuizResults_SelectedOptionId",
                table: "UserQuizResults");

            migrationBuilder.DropColumn(
                name: "SelectedOptionId",
                table: "UserQuizResults");
        }
    }
}
