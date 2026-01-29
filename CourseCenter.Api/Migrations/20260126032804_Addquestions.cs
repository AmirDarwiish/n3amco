using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CourseCenter.Api.Migrations
{
    /// <inheritdoc />
    public partial class Addquestions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_AssessmentAnswers_QuestionId",
                table: "AssessmentAnswers",
                column: "QuestionId");

            migrationBuilder.AddForeignKey(
                name: "FK_AssessmentAnswers_AssessmentQuestions_QuestionId",
                table: "AssessmentAnswers",
                column: "QuestionId",
                principalTable: "AssessmentQuestions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AssessmentAnswers_AssessmentQuestions_QuestionId",
                table: "AssessmentAnswers");

            migrationBuilder.DropIndex(
                name: "IX_AssessmentAnswers_QuestionId",
                table: "AssessmentAnswers");
        }
    }
}
