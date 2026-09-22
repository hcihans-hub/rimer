using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RimerApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTicketEnumToInt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Manually convert string values to digit strings so SQL can cast them
            migrationBuilder.Sql("UPDATE Tickets SET Status = '0' WHERE Status = 'Open'");
            migrationBuilder.Sql("UPDATE Tickets SET Status = '1' WHERE Status = 'InProgress'");
            migrationBuilder.Sql("UPDATE Tickets SET Status = '2' WHERE Status = 'Closed'");

            migrationBuilder.Sql("UPDATE Tickets SET Category = '0' WHERE Category = 'Complaint'");
            migrationBuilder.Sql("UPDATE Tickets SET Category = '1' WHERE Category = 'Suggestion'");
            migrationBuilder.Sql("UPDATE Tickets SET Category = '2' WHERE Category = 'Request'");
            migrationBuilder.Sql("UPDATE Tickets SET Category = '3' WHERE Category = 'Thanks'");
            migrationBuilder.Sql("UPDATE Tickets SET Category = '4' WHERE Category = 'Question'");
            migrationBuilder.Sql("UPDATE Tickets SET Category = '5' WHERE Category = 'InfoRequest'");

            // 2. Perform the actual column type change
            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Tickets",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<int>(
                name: "Category",
                table: "Tickets",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Tickets",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "Tickets",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");
        }
    }
}
