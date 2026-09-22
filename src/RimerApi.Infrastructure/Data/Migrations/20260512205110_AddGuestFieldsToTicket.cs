using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RimerApi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGuestFieldsToTicket : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ContainsSensitiveData",
                table: "Tickets",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuestAddress",
                table: "Tickets",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuestIdentityNumber",
                table: "Tickets",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuestSurname",
                table: "Tickets",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuestTitle",
                table: "Tickets",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HidePersonalInfo",
                table: "Tickets",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContainsSensitiveData",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "GuestAddress",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "GuestIdentityNumber",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "GuestSurname",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "GuestTitle",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "HidePersonalInfo",
                table: "Tickets");
        }
    }
}
