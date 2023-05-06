using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineLibraryManagementSystem.Migrations
{
    public partial class BookAddedToIssuedBookModel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "bad4a07f-4609-4557-95f2-1b19ce317a73");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "dd01766c-b193-47d1-a10f-04fc90b42b93");

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[] { "a0e8bda3-81dc-4d6d-b7cb-c716c7cd7f7a", "1", "Admin", "Admin" });

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[] { "bd3f973d-76eb-4e65-bfdb-191d2726a963", "2", "User", "User" });

            migrationBuilder.CreateIndex(
                name: "IX_IssueBooks_BookId",
                table: "IssueBooks",
                column: "BookId");

            migrationBuilder.AddForeignKey(
                name: "FK_IssueBooks_Books_BookId",
                table: "IssueBooks",
                column: "BookId",
                principalTable: "Books",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IssueBooks_Books_BookId",
                table: "IssueBooks");

            migrationBuilder.DropIndex(
                name: "IX_IssueBooks_BookId",
                table: "IssueBooks");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "a0e8bda3-81dc-4d6d-b7cb-c716c7cd7f7a");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "bd3f973d-76eb-4e65-bfdb-191d2726a963");

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[] { "bad4a07f-4609-4557-95f2-1b19ce317a73", "1", "Admin", "Admin" });

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[] { "dd01766c-b193-47d1-a10f-04fc90b42b93", "2", "User", "User" });
        }
    }
}
