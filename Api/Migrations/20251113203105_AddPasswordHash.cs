using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
   /// <inheritdoc />
   public partial class AddPasswordHash : Migration
   {
      /// <inheritdoc />
      protected override void Up(MigrationBuilder migrationBuilder)
      {
         // migrationBuilder.DropColumn(
         //     name: "Role",
         //     table: "Users");

         migrationBuilder.AddColumn<string>(
             name: "PasswordHash",
             table: "Users",
             type: "text",
             nullable: false,
             defaultValue: "");
      }

      /// <inheritdoc />
      protected override void Down(MigrationBuilder migrationBuilder)
      {
         migrationBuilder.DropColumn(
             name: "PasswordHash",
             table: "Users");

         migrationBuilder.AddColumn<int>(
             name: "Role",
             table: "Users",
             type: "integer",
             nullable: false,
             defaultValue: 0);
      }
   }
}
