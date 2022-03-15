using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtlasTracker.Data.Migrations
{
    public partial class _0005 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invites_Companies_CompanyId",
                table: "Invites");

            migrationBuilder.DropColumn(
                name: "CommentId",
                table: "Invites");

            migrationBuilder.RenameColumn(
                name: "IviteDate",
                table: "Invites",
                newName: "InviteDate");

            migrationBuilder.AlterColumn<int>(
                name: "CompanyId",
                table: "Invites",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Invites_Companies_CompanyId",
                table: "Invites",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invites_Companies_CompanyId",
                table: "Invites");

            migrationBuilder.RenameColumn(
                name: "InviteDate",
                table: "Invites",
                newName: "IviteDate");

            migrationBuilder.AlterColumn<int>(
                name: "CompanyId",
                table: "Invites",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "CommentId",
                table: "Invites",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddForeignKey(
                name: "FK_Invites_Companies_CompanyId",
                table: "Invites",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id");
        }
    }
}
