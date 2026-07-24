using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase2MultiDeskAndConfidentiality : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ColorHex",
                table: "ServiceDesks",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DepartmentType",
                table: "ServiceDesks",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IconName",
                table: "ServiceDesks",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsConfidential",
                table: "ServiceDesks",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "DepartmentVisibilities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ServiceDeskKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IsVisible = table.Column<bool>(type: "boolean", nullable: false),
                    CanCreateTickets = table.Column<bool>(type: "boolean", nullable: false),
                    IsConfidential = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepartmentVisibilities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OnboardingChecklists",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    TaskName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    AssignedDepartment = table.Column<int>(type: "integer", nullable: false),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnboardingChecklists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OnboardingChecklists_Users_CompletedByUserId",
                        column: x => x.CompletedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OnboardingChecklists_Users_EmployeeUserId",
                        column: x => x.EmployeeUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentVisibilities_TenantId_RoleName_ServiceDeskKey",
                table: "DepartmentVisibilities",
                columns: new[] { "TenantId", "RoleName", "ServiceDeskKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingChecklists_CompletedByUserId",
                table: "OnboardingChecklists",
                column: "CompletedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingChecklists_EmployeeUserId",
                table: "OnboardingChecklists",
                column: "EmployeeUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingChecklists_TenantId_EmployeeUserId_Type",
                table: "OnboardingChecklists",
                columns: new[] { "TenantId", "EmployeeUserId", "Type" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DepartmentVisibilities");

            migrationBuilder.DropTable(
                name: "OnboardingChecklists");

            migrationBuilder.DropColumn(
                name: "ColorHex",
                table: "ServiceDesks");

            migrationBuilder.DropColumn(
                name: "DepartmentType",
                table: "ServiceDesks");

            migrationBuilder.DropColumn(
                name: "IconName",
                table: "ServiceDesks");

            migrationBuilder.DropColumn(
                name: "IsConfidential",
                table: "ServiceDesks");
        }
    }
}
