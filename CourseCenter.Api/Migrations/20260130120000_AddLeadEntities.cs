using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CourseCenter.Api.Migrations
{
    public partial class AddLeadEntities : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LeadStages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    IsFinal = table.Column<bool>(type: "bit", nullable: false),
                    IsWon = table.Column<bool>(type: "bit", nullable: false),
                    IsLost = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeadStages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LeadStageHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LeadId = table.Column<int>(type: "int", nullable: false),
                    FromStageId = table.Column<int>(type: "int", nullable: true),
                    ToStageId = table.Column<int>(type: "int", nullable: false),
                    ChangedByUserId = table.Column<int>(type: "int", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeadStageHistory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LeadCalls",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LeadId = table.Column<int>(type: "int", nullable: false),
                    DurationInMinutes = table.Column<int>(type: "int", nullable: true),
                    CallResult = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeadCalls", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LeadMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LeadId = table.Column<int>(type: "int", nullable: false),
                    Channel = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MessagePreview = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Direction = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeadMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LeadTasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LeadId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeadTasks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LeadTags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Color = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeadTags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LeadTagLinks",
                columns: table => new
                {
                    LeadId = table.Column<int>(type: "int", nullable: false),
                    TagId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeadTagLinks", x => new { x.LeadId, x.TagId });
                });

            migrationBuilder.CreateIndex(
                name: "IX_LeadCalls_LeadId",
                table: "LeadCalls",
                column: "LeadId");

            migrationBuilder.CreateIndex(
                name: "IX_LeadCalls_CreatedByUserId",
                table: "LeadCalls",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_LeadMessages_LeadId",
                table: "LeadMessages",
                column: "LeadId");

            migrationBuilder.CreateIndex(
                name: "IX_LeadMessages_CreatedByUserId",
                table: "LeadMessages",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_LeadStageHistory_LeadId",
                table: "LeadStageHistory",
                column: "LeadId");

            migrationBuilder.CreateIndex(
                name: "IX_LeadStageHistory_FromStageId",
                table: "LeadStageHistory",
                column: "FromStageId");

            migrationBuilder.CreateIndex(
                name: "IX_LeadStageHistory_ToStageId",
                table: "LeadStageHistory",
                column: "ToStageId");

            migrationBuilder.CreateIndex(
                name: "IX_LeadStageHistory_ChangedByUserId",
                table: "LeadStageHistory",
                column: "ChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_LeadTasks_LeadId",
                table: "LeadTasks",
                column: "LeadId");

            migrationBuilder.CreateIndex(
                name: "IX_LeadTasks_CreatedByUserId",
                table: "LeadTasks",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_LeadTagLinks_TagId",
                table: "LeadTagLinks",
                column: "TagId");

            // Add foreign keys separately to avoid long schema locks on large parent tables
            migrationBuilder.AddForeignKey(
                name: "FK_LeadStageHistory_Leads_LeadId",
                table: "LeadStageHistory",
                column: "LeadId",
                principalTable: "Leads",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LeadStageHistory_LeadStages_FromStageId",
                table: "LeadStageHistory",
                column: "FromStageId",
                principalTable: "LeadStages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LeadStageHistory_LeadStages_ToStageId",
                table: "LeadStageHistory",
                column: "ToStageId",
                principalTable: "LeadStages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LeadStageHistory_Users_ChangedByUserId",
                table: "LeadStageHistory",
                column: "ChangedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LeadCalls_Leads_LeadId",
                table: "LeadCalls",
                column: "LeadId",
                principalTable: "Leads",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LeadCalls_Users_CreatedByUserId",
                table: "LeadCalls",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LeadMessages_Leads_LeadId",
                table: "LeadMessages",
                column: "LeadId",
                principalTable: "Leads",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LeadMessages_Users_CreatedByUserId",
                table: "LeadMessages",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LeadTasks_Leads_LeadId",
                table: "LeadTasks",
                column: "LeadId",
                principalTable: "Leads",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LeadTasks_Users_CreatedByUserId",
                table: "LeadTasks",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LeadTagLinks_Leads_LeadId",
                table: "LeadTagLinks",
                column: "LeadId",
                principalTable: "Leads",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LeadTagLinks_LeadTags_TagId",
                table: "LeadTagLinks",
                column: "TagId",
                principalTable: "LeadTags",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "LeadTagLinks");
            migrationBuilder.DropTable(name: "LeadTags");
            migrationBuilder.DropTable(name: "LeadTasks");
            migrationBuilder.DropTable(name: "LeadMessages");
            migrationBuilder.DropTable(name: "LeadCalls");
            migrationBuilder.DropTable(name: "LeadStageHistory");
            migrationBuilder.DropTable(name: "LeadStages");
        }
    }
}
