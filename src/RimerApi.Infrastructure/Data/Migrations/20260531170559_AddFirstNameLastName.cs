using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RimerApi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFirstNameLastName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "Users",
                type: "nvarchar(75)",
                maxLength: 75,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "Users",
                type: "nvarchar(75)",
                maxLength: 75,
                nullable: true);

            // Split FullName into FirstName and LastName in SQL
            // Everything before the last space is FirstName, everything after is LastName
            migrationBuilder.Sql(@"
                UPDATE Users
                SET 
                    FirstName = CASE 
                        WHEN CHARINDEX(' ', TRIM(FullName)) > 0 
                        THEN SUBSTRING(TRIM(FullName), 1, LEN(TRIM(FullName)) - CHARINDEX(' ', REVERSE(TRIM(FullName))))
                        ELSE TRIM(FullName)
                    END,
                    LastName = CASE 
                        WHEN CHARINDEX(' ', TRIM(FullName)) > 0 
                        THEN SUBSTRING(TRIM(FullName), LEN(TRIM(FullName)) - CHARINDEX(' ', REVERSE(TRIM(FullName))) + 2, LEN(TRIM(FullName)))
                        ELSE ''
                    END
                WHERE FullName IS NOT NULL AND FullName <> ''
            ");

            // Update any remaining nulls to empty string
            migrationBuilder.Sql("UPDATE Users SET FirstName = '' WHERE FirstName IS NULL");
            migrationBuilder.Sql("UPDATE Users SET LastName = '' WHERE LastName IS NULL");

            migrationBuilder.AlterColumn<string>(
                name: "FirstName",
                table: "Users",
                type: "nvarchar(75)",
                maxLength: 75,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(75)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LastName",
                table: "Users",
                type: "nvarchar(75)",
                maxLength: 75,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(75)",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "FullName",
                table: "Users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FullName",
                table: "Users",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            // Reconstruct FullName
            migrationBuilder.Sql("UPDATE Users SET FullName = TRIM(FirstName + ' ' + LastName)");

            migrationBuilder.AlterColumn<string>(
                name: "FullName",
                table: "Users",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "Users");
        }
    }
}
