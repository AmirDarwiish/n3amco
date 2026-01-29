using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CourseCenter.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAssessmentTests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AssessmentQuestion_AssessmentTest_TestId",
                table: "AssessmentQuestion");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AssessmentTest",
                table: "AssessmentTest");

            migrationBuilder.RenameTable(
                name: "AssessmentTest",
                newName: "AssessmentTests");

            migrationBuilder.AddColumn<string>(
                name: "Level",
                table: "ArchivedStudents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ParentPhoneNumber",
                table: "ArchivedStudents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RelativeName",
                table: "ArchivedStudents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_AssessmentTests",
                table: "AssessmentTests",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AssessmentQuestion_AssessmentTests_TestId",
                table: "AssessmentQuestion",
                column: "TestId",
                principalTable: "AssessmentTests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AssessmentQuestion_AssessmentTests_TestId",
                table: "AssessmentQuestion");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AssessmentTests",
                table: "AssessmentTests");

            migrationBuilder.DropColumn(
                name: "Level",
                table: "ArchivedStudents");

            migrationBuilder.DropColumn(
                name: "ParentPhoneNumber",
                table: "ArchivedStudents");

            migrationBuilder.DropColumn(
                name: "RelativeName",
                table: "ArchivedStudents");

            migrationBuilder.RenameTable(
                name: "AssessmentTests",
                newName: "AssessmentTest");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AssessmentTest",
                table: "AssessmentTest",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AssessmentQuestion_AssessmentTest_TestId",
                table: "AssessmentQuestion",
                column: "TestId",
                principalTable: "AssessmentTest",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
