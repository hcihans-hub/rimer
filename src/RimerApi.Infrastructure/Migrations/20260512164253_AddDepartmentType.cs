using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RimerApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDepartmentType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Departments",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Other");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Type",
                table: "Departments");
        }
    }
}
