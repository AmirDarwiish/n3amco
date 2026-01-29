using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CourseCenter.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAssessmentQuestions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AssessmentAnswer_AssessmentQuestion_QuestionId",
                table: "AssessmentAnswer");

            migrationBuilder.DropForeignKey(
                name: "FK_AssessmentQuestion_AssessmentTests_TestId",
                table: "AssessmentQuestion");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AssessmentQuestion",
                table: "AssessmentQuestion");

            migrationBuilder.RenameTable(
                name: "AssessmentQuestion",
                newName: "AssessmentQuestions");

            migrationBuilder.RenameIndex(
                name: "IX_AssessmentQuestion_TestId",
                table: "AssessmentQuestions",
                newName: "IX_AssessmentQuestions_TestId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AssessmentQuestions",
                table: "AssessmentQuestions",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AssessmentAnswer_AssessmentQuestions_QuestionId",
                table: "AssessmentAnswer",
                column: "QuestionId",
                principalTable: "AssessmentQuestions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AssessmentQuestions_AssessmentTests_TestId",
                table: "AssessmentQuestions",
                column: "TestId",
                principalTable: "AssessmentTests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AssessmentAnswer_AssessmentQuestions_QuestionId",
                table: "AssessmentAnswer");

            migrationBuilder.DropForeignKey(
                name: "FK_AssessmentQuestions_AssessmentTests_TestId",
                table: "AssessmentQuestions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AssessmentQuestions",
                table: "AssessmentQuestions");

            migrationBuilder.RenameTable(
                name: "AssessmentQuestions",
                newName: "AssessmentQuestion");

            migrationBuilder.RenameIndex(
                name: "IX_AssessmentQuestions_TestId",
                table: "AssessmentQuestion",
                newName: "IX_AssessmentQuestion_TestId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AssessmentQuestion",
                table: "AssessmentQuestion",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AssessmentAnswer_AssessmentQuestion_QuestionId",
                table: "AssessmentAnswer",
                column: "QuestionId",
                principalTable: "AssessmentQuestion",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AssessmentQuestion_AssessmentTests_TestId",
                table: "AssessmentQuestion",
                column: "TestId",
                principalTable: "AssessmentTests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
