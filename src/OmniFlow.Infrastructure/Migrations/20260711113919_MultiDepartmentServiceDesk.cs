using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MultiDepartmentServiceDesk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Departments_AssignedDepartmentId",
                table: "Tickets");

            migrationBuilder.RenameColumn(
                name: "DepartmentId",
                table: "TicketTemplates",
                newName: "ServiceDeskId");

            migrationBuilder.RenameColumn(
                name: "AssignedDepartmentId",
                table: "Tickets",
                newName: "ServiceDeskId");

            migrationBuilder.RenameIndex(
                name: "IX_Tickets_AssignedDepartmentId",
                table: "Tickets",
                newName: "IX_Tickets_ServiceDeskId");

            migrationBuilder.AddColumn<Guid>(
                name: "ServiceDeskId",
                table: "Problems",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ServiceDeskId",
                table: "ChangeRequests",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "AuthorityDelegations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DelegatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    DelegateeId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthorityDelegations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuthorityDelegations_Users_DelegateeId",
                        column: x => x.DelegateeId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AuthorityDelegations_Users_DelegatorId",
                        column: x => x.DelegatorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DirectoryProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Bio = table.Column<string>(type: "text", nullable: true),
                    Skills = table.Column<List<string>>(type: "text[]", nullable: false),
                    ShowPhoneNumber = table.Column<bool>(type: "boolean", nullable: false),
                    ShowPersonalEmail = table.Column<bool>(type: "boolean", nullable: false),
                    ShowDateOfBirth = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DirectoryProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DirectoryProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExpenseClaims",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    ExpenseDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    ReceiptUrl = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ApproverId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExpenseClaims_Users_ApproverId",
                        column: x => x.ApproverId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ExpenseClaims_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServiceDesks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    AdminId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsPublicToAllEmployees = table.Column<bool>(type: "boolean", nullable: false),
                    AllowedDepartmentIds = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceDesks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceDesks_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServiceDesks_Users_AdminId",
                        column: x => x.AdminId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_TicketTemplates_ServiceDeskId",
                table: "TicketTemplates",
                column: "ServiceDeskId");

            migrationBuilder.CreateIndex(
                name: "IX_Problems_ServiceDeskId",
                table: "Problems",
                column: "ServiceDeskId");

            migrationBuilder.CreateIndex(
                name: "IX_ChangeRequests_ServiceDeskId",
                table: "ChangeRequests",
                column: "ServiceDeskId");

            migrationBuilder.CreateIndex(
                name: "IX_AuthorityDelegations_DelegateeId",
                table: "AuthorityDelegations",
                column: "DelegateeId");

            migrationBuilder.CreateIndex(
                name: "IX_AuthorityDelegations_DelegatorId",
                table: "AuthorityDelegations",
                column: "DelegatorId");

            migrationBuilder.CreateIndex(
                name: "IX_DirectoryProfiles_UserId",
                table: "DirectoryProfiles",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseClaims_ApproverId",
                table: "ExpenseClaims",
                column: "ApproverId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseClaims_UserId",
                table: "ExpenseClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceDesks_AdminId",
                table: "ServiceDesks",
                column: "AdminId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceDesks_DepartmentId",
                table: "ServiceDesks",
                column: "DepartmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChangeRequests_ServiceDesks_ServiceDeskId",
                table: "ChangeRequests",
                column: "ServiceDeskId",
                principalTable: "ServiceDesks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Problems_ServiceDesks_ServiceDeskId",
                table: "Problems",
                column: "ServiceDeskId",
                principalTable: "ServiceDesks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_ServiceDesks_ServiceDeskId",
                table: "Tickets",
                column: "ServiceDeskId",
                principalTable: "ServiceDesks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TicketTemplates_ServiceDesks_ServiceDeskId",
                table: "TicketTemplates",
                column: "ServiceDeskId",
                principalTable: "ServiceDesks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChangeRequests_ServiceDesks_ServiceDeskId",
                table: "ChangeRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_Problems_ServiceDesks_ServiceDeskId",
                table: "Problems");

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_ServiceDesks_ServiceDeskId",
                table: "Tickets");

            migrationBuilder.DropForeignKey(
                name: "FK_TicketTemplates_ServiceDesks_ServiceDeskId",
                table: "TicketTemplates");

            migrationBuilder.DropTable(
                name: "AuthorityDelegations");

            migrationBuilder.DropTable(
                name: "DirectoryProfiles");

            migrationBuilder.DropTable(
                name: "ExpenseClaims");

            migrationBuilder.DropTable(
                name: "ServiceDesks");

            migrationBuilder.DropIndex(
                name: "IX_TicketTemplates_ServiceDeskId",
                table: "TicketTemplates");

            migrationBuilder.DropIndex(
                name: "IX_Problems_ServiceDeskId",
                table: "Problems");

            migrationBuilder.DropIndex(
                name: "IX_ChangeRequests_ServiceDeskId",
                table: "ChangeRequests");

            migrationBuilder.DropColumn(
                name: "ServiceDeskId",
                table: "Problems");

            migrationBuilder.DropColumn(
                name: "ServiceDeskId",
                table: "ChangeRequests");

            migrationBuilder.RenameColumn(
                name: "ServiceDeskId",
                table: "TicketTemplates",
                newName: "DepartmentId");

            migrationBuilder.RenameColumn(
                name: "ServiceDeskId",
                table: "Tickets",
                newName: "AssignedDepartmentId");

            migrationBuilder.RenameIndex(
                name: "IX_Tickets_ServiceDeskId",
                table: "Tickets",
                newName: "IX_Tickets_AssignedDepartmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Departments_AssignedDepartmentId",
                table: "Tickets",
                column: "AssignedDepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
