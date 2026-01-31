using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CourseCenter.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddLostReasonToLeads : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LostReason",
                table: "Leads",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LostReason",
                table: "Leads");
        }
    }
}
