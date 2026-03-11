using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DairySystem.Api.Migrations
{
    public partial class Accountseed : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Accounts",
                columns: new[] { "Id", "Code", "CreatedAt", "IsActive", "Name", "ParentId", "Type" },
                values: new object[,]
                {
                    { 1, "1000", new DateTime(2026, 4, 16, 9, 52, 22, 439, DateTimeKind.Utc).AddTicks(3222), true, "الأصول", null, 1 },
                    { 7, "2000", new DateTime(2026, 4, 16, 9, 52, 22, 439, DateTimeKind.Utc).AddTicks(3267), true, "الخصوم", null, 2 },
                    { 10, "3000", new DateTime(2026, 4, 16, 9, 52, 22, 439, DateTimeKind.Utc).AddTicks(3283), true, "حقوق الملكية", null, 3 },
                    { 13, "4000", new DateTime(2026, 4, 16, 9, 52, 22, 439, DateTimeKind.Utc).AddTicks(3301), true, "الإيرادات", null, 4 },
                    { 15, "5000", new DateTime(2026, 4, 16, 9, 52, 22, 439, DateTimeKind.Utc).AddTicks(3306), true, "المصروفات", null, 5 }
                });

            migrationBuilder.InsertData(
                table: "Accounts",
                columns: new[] { "Id", "Code", "CreatedAt", "IsActive", "Name", "ParentId", "Type" },
                values: new object[,]
                {
                    { 2, "1100", new DateTime(2026, 4, 16, 9, 52, 22, 439, DateTimeKind.Utc).AddTicks(3231), true, "الأصول المتداولة", 1, 1 },
                    { 8, "2100", new DateTime(2026, 4, 16, 9, 52, 22, 439, DateTimeKind.Utc).AddTicks(3269), true, "الخصوم المتداولة", 7, 2 },
                    { 11, "3001", new DateTime(2026, 4, 16, 9, 52, 22, 439, DateTimeKind.Utc).AddTicks(3291), true, "رأس المال", 10, 3 },
                    { 12, "3002", new DateTime(2026, 4, 16, 9, 52, 22, 439, DateTimeKind.Utc).AddTicks(3294), true, "الأرباح المحتجزة", 10, 3 },
                    { 14, "4001", new DateTime(2026, 4, 16, 9, 52, 22, 439, DateTimeKind.Utc).AddTicks(3303), true, "إيرادات المبيعات", 13, 4 },
                    { 16, "5001", new DateTime(2026, 4, 16, 9, 52, 22, 439, DateTimeKind.Utc).AddTicks(3308), true, "تكلفة البضاعة المباعة", 15, 5 },
                    { 17, "5002", new DateTime(2026, 4, 16, 9, 52, 22, 439, DateTimeKind.Utc).AddTicks(3313), true, "مصروفات تشغيلية", 15, 5 }
                });

            migrationBuilder.InsertData(
                table: "Accounts",
                columns: new[] { "Id", "Code", "CreatedAt", "IsActive", "Name", "ParentId", "Type" },
                values: new object[,]
                {
                    { 3, "1101", new DateTime(2026, 4, 16, 9, 52, 22, 439, DateTimeKind.Utc).AddTicks(3236), true, "الصندوق", 2, 1 },
                    { 4, "1102", new DateTime(2026, 4, 16, 9, 52, 22, 439, DateTimeKind.Utc).AddTicks(3239), true, "البنك", 2, 1 },
                    { 5, "1103", new DateTime(2026, 4, 16, 9, 52, 22, 439, DateTimeKind.Utc).AddTicks(3245), true, "ذمم العملاء", 2, 1 },
                    { 6, "1104", new DateTime(2026, 4, 16, 9, 52, 22, 439, DateTimeKind.Utc).AddTicks(3254), true, "المخزون", 2, 1 },
                    { 9, "2101", new DateTime(2026, 4, 16, 9, 52, 22, 439, DateTimeKind.Utc).AddTicks(3278), true, "ذمم الموردين", 8, 2 }
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Accounts",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Accounts",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Accounts",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Accounts",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Accounts",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Accounts",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Accounts",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "Accounts",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "Accounts",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "Accounts",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "Accounts",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Accounts",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Accounts",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Accounts",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "Accounts",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "Accounts",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Accounts",
                keyColumn: "Id",
                keyValue: 7);
        }
    }
}
