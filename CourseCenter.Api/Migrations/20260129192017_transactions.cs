using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CourseCenter.Api.Migrations
{
    /// <inheritdoc />
    public partial class transactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InteractionType",
                table: "LeadNotes",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InteractionType",
                table: "LeadNotes");
        }
    }
}
