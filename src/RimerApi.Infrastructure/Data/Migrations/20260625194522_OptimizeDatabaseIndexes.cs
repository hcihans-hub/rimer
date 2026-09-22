using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RimerApi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class OptimizeDatabaseIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tickets_AssignedDepartmentId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_AssignedToId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_Category",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_CreatorId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_IsExternal",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_Status",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_Status_Category_Department",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_TicketReplies_TicketId",
                table: "TicketReplies");

            migrationBuilder.DropIndex(
                name: "IX_TicketHistories_TicketId",
                table: "TicketHistories");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId_IsRead",
                table: "Notifications");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_AssignedDept_CreatedAt",
                table: "Tickets",
                columns: new[] { "AssignedDepartmentId", "CreatedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_AssignedDept_Status",
                table: "Tickets",
                columns: new[] { "AssignedDepartmentId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_AssignedToId_Status",
                table: "Tickets",
                columns: new[] { "AssignedToId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_Category_CreatedAt",
                table: "Tickets",
                columns: new[] { "Category", "CreatedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_CreatorId_CreatedAt",
                table: "Tickets",
                columns: new[] { "CreatorId", "CreatedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_DepartmentId_Status",
                table: "Tickets",
                columns: new[] { "DepartmentId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_Status_CreatedAt",
                table: "Tickets",
                columns: new[] { "Status", "CreatedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_TicketReplies_TicketId_CreatedAt",
                table: "TicketReplies",
                columns: new[] { "TicketId", "CreatedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_TicketReminders_IsDismissed_ReminderAt",
                table: "TicketReminders",
                columns: new[] { "IsDismissed", "ReminderAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TicketHistories_TicketId_CreatedAt",
                table: "TicketHistories",
                columns: new[] { "TicketId", "CreatedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead_CreatedAt",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead", "CreatedAt" },
                descending: new[] { false, false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tickets_AssignedDept_CreatedAt",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_AssignedDept_Status",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_AssignedToId_Status",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_Category_CreatedAt",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_CreatorId_CreatedAt",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_DepartmentId_Status",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_Status_CreatedAt",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_TicketReplies_TicketId_CreatedAt",
                table: "TicketReplies");

            migrationBuilder.DropIndex(
                name: "IX_TicketReminders_IsDismissed_ReminderAt",
                table: "TicketReminders");

            migrationBuilder.DropIndex(
                name: "IX_TicketHistories_TicketId_CreatedAt",
                table: "TicketHistories");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId_IsRead_CreatedAt",
                table: "Notifications");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_AssignedDepartmentId",
                table: "Tickets",
                column: "AssignedDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_AssignedToId",
                table: "Tickets",
                column: "AssignedToId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_Category",
                table: "Tickets",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_CreatorId",
                table: "Tickets",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_IsExternal",
                table: "Tickets",
                column: "IsExternal");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_Status",
                table: "Tickets",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_Status_Category_Department",
                table: "Tickets",
                columns: new[] { "Status", "Category", "DepartmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_TicketReplies_TicketId",
                table: "TicketReplies",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketHistories_TicketId",
                table: "TicketHistories",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead" });
        }
    }
}
