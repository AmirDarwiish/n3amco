using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CourseCenter.Api.Migrations
{
    /// <inheritdoc />
    public partial class mentions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LeadMentions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LeadNoteId = table.Column<int>(type: "int", nullable: false),
                    MentionedUserId = table.Column<int>(type: "int", nullable: false),
                    MentionedByUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeadMentions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeadMentions_LeadNotes_LeadNoteId",
                        column: x => x.LeadNoteId,
                        principalTable: "LeadNotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LeadMentions_LeadNoteId",
                table: "LeadMentions",
                column: "LeadNoteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LeadMentions");
        }
    }
}
