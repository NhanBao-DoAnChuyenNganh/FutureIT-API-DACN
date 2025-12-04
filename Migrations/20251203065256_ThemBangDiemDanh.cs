using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoAnCoSo_Web.Migrations
{
    /// <inheritdoc />
    public partial class ThemBangDiemDanh : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NgaySN",
                table: "AspNetUsers");

            migrationBuilder.CreateTable(
                name: "DiemDanh",
                columns: table => new
                {
                    MaDiemDanh = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaLopHoc = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    NgayDiemDanh = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CoMat = table.Column<bool>(type: "bit", nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiemDanh", x => x.MaDiemDanh);
                    table.ForeignKey(
                        name: "FK_DiemDanh_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DiemDanh_LopHoc_MaLopHoc",
                        column: x => x.MaLopHoc,
                        principalTable: "LopHoc",
                        principalColumn: "MaLopHoc",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DiemDanh_MaLopHoc",
                table: "DiemDanh",
                column: "MaLopHoc");

            migrationBuilder.CreateIndex(
                name: "IX_DiemDanh_UserId",
                table: "DiemDanh",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DiemDanh");

            migrationBuilder.AddColumn<DateTime>(
                name: "NgaySN",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}
