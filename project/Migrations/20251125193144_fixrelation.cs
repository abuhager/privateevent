using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace project.Migrations
{
    /// <inheritdoc />
    public partial class fixrelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Rolls_UserId",
                table: "Rolls",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Rolls_Users_UserId",
                table: "Rolls",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rolls_Users_UserId",
                table: "Rolls");

            migrationBuilder.DropIndex(
                name: "IX_Rolls_UserId",
                table: "Rolls");
        }
    }
}
