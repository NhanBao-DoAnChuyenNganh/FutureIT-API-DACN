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
            // Tất cả đã tồn tại trong database, chỉ cần đánh dấu migration đã chạy
            // Kiểm tra và tạo bảng ChuyenNganh nếu chưa có
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ChuyenNganh')
                BEGIN
                    CREATE TABLE [ChuyenNganh] (
                        [MaChuyenNganh] int NOT NULL IDENTITY,
                        [TenChuyenNganh] nvarchar(max) NOT NULL,
                        CONSTRAINT [PK_ChuyenNganh] PRIMARY KEY ([MaChuyenNganh])
                    );
                END
            ");

            // Kiểm tra và thêm cột nếu chưa có
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AspNetUsers') AND name = 'ChuyenNganhMaChuyenNganh')
                BEGIN
                    ALTER TABLE [AspNetUsers] ADD [ChuyenNganhMaChuyenNganh] int NULL;
                END
            ");

            // Kiểm tra và tạo index nếu chưa có
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AspNetUsers_ChuyenNganhMaChuyenNganh' AND object_id = OBJECT_ID('AspNetUsers'))
                BEGIN
                    CREATE INDEX [IX_AspNetUsers_ChuyenNganhMaChuyenNganh] ON [AspNetUsers] ([ChuyenNganhMaChuyenNganh]);
                END
            ");

            // Kiểm tra và tạo foreign key nếu chưa có
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_AspNetUsers_ChuyenNganh_ChuyenNganhMaChuyenNganh')
                BEGIN
                    ALTER TABLE [AspNetUsers] ADD CONSTRAINT [FK_AspNetUsers_ChuyenNganh_ChuyenNganhMaChuyenNganh] 
                    FOREIGN KEY ([ChuyenNganhMaChuyenNganh]) REFERENCES [ChuyenNganh] ([MaChuyenNganh]);
                END
            ");
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
