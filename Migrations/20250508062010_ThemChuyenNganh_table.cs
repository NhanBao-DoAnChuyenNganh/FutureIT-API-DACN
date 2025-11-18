using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoAnCoSo_Web.Migrations
{
    /// <inheritdoc />
    public partial class ThemChuyenNganh_table : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ChuyenNganhMaChuyenNganh",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ChuyenNganh",
                columns: table => new
                {
                    MaChuyenNganh = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenChuyenNganh = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChuyenNganh", x => x.MaChuyenNganh);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_ChuyenNganhMaChuyenNganh",
                table: "AspNetUsers",
                column: "ChuyenNganhMaChuyenNganh");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_ChuyenNganh_ChuyenNganhMaChuyenNganh",
                table: "AspNetUsers",
                column: "ChuyenNganhMaChuyenNganh",
                principalTable: "ChuyenNganh",
                principalColumn: "MaChuyenNganh");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_ChuyenNganh_ChuyenNganhMaChuyenNganh",
                table: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "ChuyenNganh");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_ChuyenNganhMaChuyenNganh",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ChuyenNganhMaChuyenNganh",
                table: "AspNetUsers");
        }
    }
}
