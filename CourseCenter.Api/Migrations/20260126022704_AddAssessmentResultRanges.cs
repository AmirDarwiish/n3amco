using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CourseCenter.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAssessmentResultRanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssessmentAnswerRanges");

            migrationBuilder.AddColumn<string>(
                name: "ResultLabel",
                table: "AssessmentAttempts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "AssessmentResultRanges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TestId = table.Column<int>(type: "int", nullable: false),
                    FromScore = table.Column<int>(type: "int", nullable: false),
                    ToScore = table.Column<int>(type: "int", nullable: false),
                    ResultLabel = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentResultRanges", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssessmentResultRanges");

            migrationBuilder.DropColumn(
                name: "ResultLabel",
                table: "AssessmentAttempts");

            migrationBuilder.CreateTable(
                name: "AssessmentAnswerRanges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AnswerId = table.Column<int>(type: "int", nullable: false),
                    FromValue = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<int>(type: "int", nullable: false),
                    ToValue = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentAnswerRanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssessmentAnswerRanges_AssessmentAnswers_AnswerId",
                        column: x => x.AnswerId,
                        principalTable: "AssessmentAnswers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentAnswerRanges_AnswerId",
                table: "AssessmentAnswerRanges",
                column: "AnswerId");
        }
    }
}
