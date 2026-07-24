using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DepartmentHeadForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Departments_Employees_HeadId1",
                table: "Departments");

            migrationBuilder.DropIndex(
                name: "IX_Departments_HeadId1",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "HeadId1",
                table: "Departments");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_HeadId",
                table: "Departments",
                column: "HeadId");

            migrationBuilder.AddForeignKey(
                name: "FK_Departments_Employees_HeadId",
                table: "Departments",
                column: "HeadId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Departments_Employees_HeadId",
                table: "Departments");

            migrationBuilder.DropIndex(
                name: "IX_Departments_HeadId",
                table: "Departments");

            migrationBuilder.AddColumn<Guid>(
                name: "HeadId1",
                table: "Departments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Departments_HeadId1",
                table: "Departments",
                column: "HeadId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Departments_Employees_HeadId1",
                table: "Departments",
                column: "HeadId1",
                principalTable: "Employees",
                principalColumn: "Id");
        }
    }
}
