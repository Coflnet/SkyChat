using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkyChat.Migrations
{
    public partial class moderationRetention : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Rule",
                table: "Mute",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                """
                UPDATE `Mute`
                SET `Rule` = CASE
                    WHEN TRIM(COALESCE(`Reason`, '')) LIKE '1 %' THEN 1
                    WHEN TRIM(COALESCE(`Reason`, '')) LIKE '2 %' THEN 2
                    ELSE 0
                END
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Rule",
                table: "Mute");
        }
    }
}
