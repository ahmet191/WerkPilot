using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EmployeeProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DocumentType",
                table: "EmployeeDocuments");

            migrationBuilder.DropColumn(
                name: "FileUrl",
                table: "EmployeeDocuments");

            migrationBuilder.RenameColumn(
                name: "ExpiryDate",
                table: "EmployeeDocuments",
                newName: "ExpirationDate");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "EmployeeDocuments",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "EmployeeDocuments",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "EmployeeProfileId",
                table: "EmployeeDocuments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "EmployeeDocuments",
                type: "character varying(260)",
                maxLength: 260,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "FileSizeBytes",
                table: "EmployeeDocuments",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "FileStorageKey",
                table: "EmployeeDocuments",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsConfidential",
                table: "EmployeeDocuments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "EmployeeDocuments",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UploadedAt",
                table: "EmployeeDocuments",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "EmployeeProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Country = table.Column<int>(type: "integer", nullable: false),
                    BirthDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Gender = table.Column<string>(type: "text", nullable: true),
                    BloodType = table.Column<string>(type: "text", nullable: true),
                    EmergencyContactName = table.Column<string>(type: "text", nullable: true),
                    EmergencyContactPhone = table.Column<string>(type: "text", nullable: true),
                    AddressLine = table.Column<string>(type: "text", nullable: true),
                    City = table.Column<string>(type: "text", nullable: true),
                    PostalCode = table.Column<string>(type: "text", nullable: true),
                    HireDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TrialPeriodEndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EmploymentType = table.Column<int>(type: "integer", nullable: false),
                    WorkingHoursPerWeek = table.Column<decimal>(type: "numeric", nullable: false),
                    ManagerId = table.Column<Guid>(type: "uuid", nullable: true),
                    GrossSalary = table.Column<decimal>(type: "numeric", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    BankName = table.Column<string>(type: "text", nullable: false),
                    Iban = table.Column<string>(type: "text", nullable: false),
                    BicSwift = table.Column<string>(type: "text", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    GermanyDetails = table.Column<string>(type: "jsonb", nullable: false),
                    TurkeyDetails = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeProfiles_Users_ManagerId",
                        column: x => x.ManagerId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EmployeeProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDocuments_EmployeeProfileId",
                table: "EmployeeDocuments",
                column: "EmployeeProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDocuments_TenantId_UserId",
                table: "EmployeeDocuments",
                columns: new[] { "TenantId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeProfiles_ManagerId",
                table: "EmployeeProfiles",
                column: "ManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeProfiles_TenantId_UserId",
                table: "EmployeeProfiles",
                columns: new[] { "TenantId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeProfiles_UserId",
                table: "EmployeeProfiles",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeDocuments_EmployeeProfiles_EmployeeProfileId",
                table: "EmployeeDocuments",
                column: "EmployeeProfileId",
                principalTable: "EmployeeProfiles",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeDocuments_EmployeeProfiles_EmployeeProfileId",
                table: "EmployeeDocuments");

            migrationBuilder.DropTable(
                name: "EmployeeProfiles");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeDocuments_EmployeeProfileId",
                table: "EmployeeDocuments");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeDocuments_TenantId_UserId",
                table: "EmployeeDocuments");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "EmployeeDocuments");

            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "EmployeeDocuments");

            migrationBuilder.DropColumn(
                name: "EmployeeProfileId",
                table: "EmployeeDocuments");

            migrationBuilder.DropColumn(
                name: "FileName",
                table: "EmployeeDocuments");

            migrationBuilder.DropColumn(
                name: "FileSizeBytes",
                table: "EmployeeDocuments");

            migrationBuilder.DropColumn(
                name: "FileStorageKey",
                table: "EmployeeDocuments");

            migrationBuilder.DropColumn(
                name: "IsConfidential",
                table: "EmployeeDocuments");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "EmployeeDocuments");

            migrationBuilder.DropColumn(
                name: "UploadedAt",
                table: "EmployeeDocuments");

            migrationBuilder.RenameColumn(
                name: "ExpirationDate",
                table: "EmployeeDocuments",
                newName: "ExpiryDate");

            migrationBuilder.AddColumn<string>(
                name: "DocumentType",
                table: "EmployeeDocuments",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FileUrl",
                table: "EmployeeDocuments",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");
        }
    }
}
