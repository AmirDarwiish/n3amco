using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CourseCenter.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddLeadArchiving : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                table: "Leads",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ArchivedByUserId",
                table: "Leads",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "Leads",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "ArchivedByUserId",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "Leads");
        }
    }
}
