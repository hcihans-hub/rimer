using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RimerApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReferenceNo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReferenceNo",
                table: "Tickets",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("UPDATE Tickets SET ReferenceNo = LEFT(CAST(Id AS NVARCHAR(36)), 8)");

            migrationBuilder.CreateIndex(
                name: "UX_Tickets_ReferenceNo",
                table: "Tickets",
                column: "ReferenceNo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Tickets_ReferenceNo",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "ReferenceNo",
                table: "Tickets");
        }
    }
}
