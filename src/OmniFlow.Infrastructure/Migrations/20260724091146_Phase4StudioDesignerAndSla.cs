using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase4StudioDesignerAndSla : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomDepartments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    DepartmentType = table.Column<string>(type: "text", nullable: false),
                    IconName = table.Column<string>(type: "text", nullable: false),
                    ColorHex = table.Column<string>(type: "text", nullable: false),
                    ParentDepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_CustomDepartments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomDepartments_CustomDepartments_ParentDepartmentId",
                        column: x => x.ParentDepartmentId,
                        principalTable: "CustomDepartments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SlaPolicyRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    ResponseTimeMinutes = table.Column<int>(type: "integer", nullable: false),
                    ResolutionTimeHours = table.Column<int>(type: "integer", nullable: false),
                    EscalateAfterPercent = table.Column<int>(type: "integer", nullable: false),
                    EscalateToUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    NotifyManagerOnBreach = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_SlaPolicyRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlaPolicyRules_Users_EscalateToUserId",
                        column: x => x.EscalateToUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TicketFormSchemas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceDeskId = table.Column<Guid>(type: "uuid", nullable: false),
                    FormTitle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FormDescription = table.Column<string>(type: "text", nullable: false),
                    FieldsJson = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_TicketFormSchemas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketFormSchemas_ServiceDesks_ServiceDeskId",
                        column: x => x.ServiceDeskId,
                        principalTable: "ServiceDesks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomDepartments_ParentDepartmentId",
                table: "CustomDepartments",
                column: "ParentDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomDepartments_TenantId_Code",
                table: "CustomDepartments",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SlaPolicyRules_EscalateToUserId",
                table: "SlaPolicyRules",
                column: "EscalateToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SlaPolicyRules_TenantId_Priority",
                table: "SlaPolicyRules",
                columns: new[] { "TenantId", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_TicketFormSchemas_ServiceDeskId",
                table: "TicketFormSchemas",
                column: "ServiceDeskId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketFormSchemas_TenantId_ServiceDeskId",
                table: "TicketFormSchemas",
                columns: new[] { "TenantId", "ServiceDeskId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomDepartments");

            migrationBuilder.DropTable(
                name: "SlaPolicyRules");

            migrationBuilder.DropTable(
                name: "TicketFormSchemas");
        }
    }
}
