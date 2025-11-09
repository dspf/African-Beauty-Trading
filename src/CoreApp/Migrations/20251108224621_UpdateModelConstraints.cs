using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AfricanBeautyTrading.Core.Migrations
{
    /// <inheritdoc />
    public partial class UpdateModelConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatMessages_AspNetUsers_SenderId",
                table: "ChatMessages");

            migrationBuilder.DropForeignKey(
                name: "FK_ChatRooms_AspNetUsers_AdminId",
                table: "ChatRooms");

            migrationBuilder.DropForeignKey(
                name: "FK_ChatRooms_AspNetUsers_CustomerId",
                table: "ChatRooms");

            migrationBuilder.AddColumn<string>(
                name: "AdminId1",
                table: "ChatRooms",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CustomerId1",
                table: "ChatRooms",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "SenderName",
                table: "ChatMessages",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Message",
                table: "ChatMessages",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "SenderId1",
                table: "ChatMessages",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_ChatRooms_AdminId1",
                table: "ChatRooms",
                column: "AdminId1");

            migrationBuilder.CreateIndex(
                name: "IX_ChatRooms_CustomerId1",
                table: "ChatRooms",
                column: "CustomerId1");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_SenderId1",
                table: "ChatMessages",
                column: "SenderId1");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMessages_AspNetUsers_SenderId",
                table: "ChatMessages",
                column: "SenderId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMessages_AspNetUsers_SenderId1",
                table: "ChatMessages",
                column: "SenderId1",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatRooms_AspNetUsers_AdminId",
                table: "ChatRooms",
                column: "AdminId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatRooms_AspNetUsers_AdminId1",
                table: "ChatRooms",
                column: "AdminId1",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatRooms_AspNetUsers_CustomerId",
                table: "ChatRooms",
                column: "CustomerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatRooms_AspNetUsers_CustomerId1",
                table: "ChatRooms",
                column: "CustomerId1",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatMessages_AspNetUsers_SenderId",
                table: "ChatMessages");

            migrationBuilder.DropForeignKey(
                name: "FK_ChatMessages_AspNetUsers_SenderId1",
                table: "ChatMessages");

            migrationBuilder.DropForeignKey(
                name: "FK_ChatRooms_AspNetUsers_AdminId",
                table: "ChatRooms");

            migrationBuilder.DropForeignKey(
                name: "FK_ChatRooms_AspNetUsers_AdminId1",
                table: "ChatRooms");

            migrationBuilder.DropForeignKey(
                name: "FK_ChatRooms_AspNetUsers_CustomerId",
                table: "ChatRooms");

            migrationBuilder.DropForeignKey(
                name: "FK_ChatRooms_AspNetUsers_CustomerId1",
                table: "ChatRooms");

            migrationBuilder.DropIndex(
                name: "IX_ChatRooms_AdminId1",
                table: "ChatRooms");

            migrationBuilder.DropIndex(
                name: "IX_ChatRooms_CustomerId1",
                table: "ChatRooms");

            migrationBuilder.DropIndex(
                name: "IX_ChatMessages_SenderId1",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "AdminId1",
                table: "ChatRooms");

            migrationBuilder.DropColumn(
                name: "CustomerId1",
                table: "ChatRooms");

            migrationBuilder.DropColumn(
                name: "SenderId1",
                table: "ChatMessages");

            migrationBuilder.AlterColumn<string>(
                name: "SenderName",
                table: "ChatMessages",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "Message",
                table: "ChatMessages",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMessages_AspNetUsers_SenderId",
                table: "ChatMessages",
                column: "SenderId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatRooms_AspNetUsers_AdminId",
                table: "ChatRooms",
                column: "AdminId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatRooms_AspNetUsers_CustomerId",
                table: "ChatRooms",
                column: "CustomerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
