using DoAnCoSo_Web.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore;

namespace DoAnCoSo_Web.Data
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<User> User { get; set; }
        public DbSet<KhoaHoc> KhoaHoc { get; set; }
        public DbSet<HinhAnhKhoaHoc> HinhAnhKhoaHoc { get; set; }
        public DbSet<TinTucTuyenDung> TinTucTuyenDung { get; set; }
        public DbSet<HoaDon> HoaDon { get; set; }
        public DbSet<PhongHoc> PhongHoc { get; set; }
        public DbSet<LopHoc> LopHoc { get; set; }
        public DbSet<ChiTietGiangDay> ChiTietGiangDay { get; set; }
        public DbSet<ChiTietHocTap> ChiTietHocTap { get; set; }
        public DbSet<DanhGia> DanhGia { get; set; }
        public DbSet<DanhSachQuanTam> DanhSachQuanTam { get; set; }
        public DbSet<PhieuDangKyKhoaHoc> PhieuDangKyKhoaHoc { get; set; }
        public DbSet<ToCaoDanhGia> ToCaoDanhGia { get; set; }
        public DbSet<ChuyenNganh> ChuyenNganh {  get; set; }
        public DbSet<DiemDanh> DiemDanh { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            //Khóa chính Phiếu đăng ký Môn Học
            modelBuilder.Entity<PhieuDangKyKhoaHoc>()
                .HasKey(c => new { c.UserId, c.MaKhoaHoc,c.MaHoaDon });

            // Thiết lập quan hệ cho PhieuDangKyKhoaHoc
            modelBuilder.Entity<PhieuDangKyKhoaHoc>()
                .HasOne(p => p.User)
                .WithMany(u => u.PhieuDangKyKhoaHoc)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PhieuDangKyKhoaHoc>()
                .HasOne(p => p.KhoaHoc)
                .WithMany(k => k.PhieuDangKyKhoaHoc)
                .HasForeignKey(p => p.MaKhoaHoc)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PhieuDangKyKhoaHoc>()
               .HasOne(p => p.HoaDon)
               .WithMany(u => u.PhieuDangKyKhoaHoc)
               .HasForeignKey(p => p.MaHoaDon)
               .OnDelete(DeleteBehavior.Restrict);
            //Khóa chính chi tiết học tập
            modelBuilder.Entity<ChiTietHocTap>()
    .HasKey(c => new { c.UserId, c.MaLopHoc });

            modelBuilder.Entity<ChiTietHocTap>()
                .HasOne(c => c.User)
                .WithMany(u => u.ChiTietHocTap)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ChiTietHocTap>()
                .HasOne(c => c.LopHoc)
                .WithMany(l => l.ChiTietHocTap)
                .HasForeignKey(c => c.MaLopHoc)
                .OnDelete(DeleteBehavior.Cascade);

            //Khóa chính chi tiết giảng dạy
            modelBuilder.Entity<ChiTietGiangDay>()
        .HasKey(c => new { c.UserId, c.MaLopHoc });

            modelBuilder.Entity<ChiTietGiangDay>()
                .HasOne(c => c.User)
                .WithMany(u => u.ChiTietGiangDay)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ChiTietGiangDay>()
                .HasOne(c => c.LopHoc)
                .WithMany(l => l.ChiTietGiangDay)
                .HasForeignKey(c => c.MaLopHoc)
                .OnDelete(DeleteBehavior.Cascade);

            // Khóa chính ToCaoDanhGia
            modelBuilder.Entity<ToCaoDanhGia>()
            .HasKey(tc => tc.Id);  // Dùng ID làm khóa chính

            modelBuilder.Entity<ToCaoDanhGia>()
                .HasOne(tc => tc.User)
                .WithMany(u => u.ToCaoDanhGia)
                .HasForeignKey(tc => tc.UserId)
                .OnDelete(DeleteBehavior.Restrict); // Tránh cascade vòng lặp

            modelBuilder.Entity<ToCaoDanhGia>()
                .HasOne(tc => tc.DanhGia)
                .WithMany(dg => dg.ToCaoDanhGia)
                .HasForeignKey(tc => tc.MaDanhGia)
                .OnDelete(DeleteBehavior.Restrict); // Tránh cascade vòng lặp

            modelBuilder.Entity<ToCaoDanhGia>()
                .HasIndex(tc => new { tc.MaDanhGia, tc.UserId })
                .IsUnique(); // Chỉ mục cho MaDanhGia và UserId

            // Khóa chính DanhSachQuanTam
            modelBuilder.Entity<DanhSachQuanTam>()
                .HasKey(qt => new { qt.UserId, qt.MaKhoaHoc });

            modelBuilder.Entity<DanhSachQuanTam>()
                .HasOne(qt => qt.User)
                .WithMany(u => u.DanhSachQuanTam)
                .HasForeignKey(qt => qt.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DanhSachQuanTam>()
                .HasOne(qt => qt.KhoaHoc)
                .WithMany(kh => kh.DanhSachQuanTam)
                .HasForeignKey(qt => qt.MaKhoaHoc)
                .OnDelete(DeleteBehavior.Cascade);


        }

    }
}
