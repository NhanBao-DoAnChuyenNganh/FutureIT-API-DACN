using DoAnCoSo_Web.Data;
using DoAnCoSo_Web.Models;
using DoAnCoSo_Web.Models.AppSettings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace DoAnCoSo_Web_TestAPI.Areas.Student.Controllers
{
    [Area("Student")]
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class StudentHomeApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly AppSettings _appSettings;

        public StudentHomeApiController(ApplicationDbContext db, IOptions<AppSettings> options)
        {
            _db = db;
            _appSettings = options.Value;
        }

        private static double LamTron(double x)
        {
            var nguyen = Math.Floor(x);
            var le = x - nguyen;
            if (le < 0.25) return nguyen;
            if (le < 0.75) return nguyen + 0.5;
            return nguyen + 1.0;
        }

        /// <summary>
        /// BaseQuery trả IQueryable KhoaHoc, có Include hình ảnh
        /// </summary>
        private IQueryable<KhoaHoc> BaseQuery()
        {
            return _db.KhoaHoc.Include(kh => kh.HinhAnhKhoaHoc).OrderBy(kh => kh.TenKhoaHoc);
        }

        /// <summary>
        /// Get tất cả khóa học
        /// </summary>
        [AllowAnonymous]
        [HttpGet]
        public IActionResult GetKhoaHoc()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Load data cần thiết trước để tránh EF cố gắng translate instance method
            var listDaQuanTam = _db.DanhSachQuanTam.Where(ds => ds.UserId == userId)
                                                   .Select(ds => ds.MaKhoaHoc)
                                                   .ToList();

            var allDanhSachQuanTam = _db.DanhSachQuanTam.ToList();
            var allDanhGia = _db.DanhGia.ToList();
            var allHinhAnh = _db.HinhAnhKhoaHoc.ToList();

            var list = BaseQuery()
                .AsEnumerable()
                .Select(kh =>
                {
                    var daYeuThich = listDaQuanTam.Contains(kh.MaKhoaHoc);
                    var tongLuotQuanTam = allDanhSachQuanTam.Count(q => q.MaKhoaHoc == kh.MaKhoaHoc);
                    var tongLuotBinhLuan = allDanhGia.Count(d => d.KhoaHocMaKhoaHoc == kh.MaKhoaHoc);
                    var soSaoTrungBinh = LamTron(
                        allDanhGia.Where(d => d.KhoaHocMaKhoaHoc == kh.MaKhoaHoc)
                                   .Select(d => d.SoSaoDanhGia)
                                   .DefaultIfEmpty(0)
                                   .Average()
                    );
                    var hinhAnh = allHinhAnh
                        .Where(h => h.KhoaHocMaKhoaHoc == kh.MaKhoaHoc && h.LaAnhDaiDien)
                        .Select(h => $"{_appSettings.BaseUrl}/api/Image/Get?id={h.MaHinh}")
                        .FirstOrDefault();

                    return new
                    {
                        kh.MaKhoaHoc,
                        kh.TenKhoaHoc,
                        kh.NgayHoc,
                        kh.HocPhi,
                        DaYeuThich = daYeuThich,
                        TongLuotQuanTam = tongLuotQuanTam,
                        TongLuotBinhLuan = tongLuotBinhLuan,
                        SoSaoTrungBinh = soSaoTrungBinh,
                        HinhAnh = hinhAnh
                    };
                }).ToList();

            return Ok(list);
        }

        /// <summary>
        /// Tìm kiếm theo tên và lọc theo loại (optional)
        /// </summary>
        [AllowAnonymous]
        [HttpGet]
        public IActionResult SearchOrFilter(string? ten = null, string? tenLoai = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var listDaQuanTam = _db.DanhSachQuanTam.Where(ds => ds.UserId == userId)
                                                   .Select(ds => ds.MaKhoaHoc)
                                                   .ToList();

            var allDanhSachQuanTam = _db.DanhSachQuanTam.ToList();
            var allDanhGia = _db.DanhGia.ToList();
            var allHinhAnh = _db.HinhAnhKhoaHoc.ToList();

            var query = BaseQuery();

            if (!string.IsNullOrEmpty(ten))
                query = query.Where(kh => kh.TenKhoaHoc.ToLower().Contains(ten.ToLower()));

            if (!string.IsNullOrEmpty(tenLoai))
                query = query.Where(kh => kh.TenKhoaHoc.ToLower().Contains(tenLoai.ToLower()));


            var list = query
                .AsEnumerable()
                .Select(kh =>
                {
                    var daYeuThich = listDaQuanTam.Contains(kh.MaKhoaHoc);
                    var tongLuotQuanTam = allDanhSachQuanTam.Count(q => q.MaKhoaHoc == kh.MaKhoaHoc);
                    var tongLuotBinhLuan = allDanhGia.Count(d => d.KhoaHocMaKhoaHoc == kh.MaKhoaHoc);
                    var soSaoTrungBinh = LamTron(
                        allDanhGia.Where(d => d.KhoaHocMaKhoaHoc == kh.MaKhoaHoc)
                                   .Select(d => d.SoSaoDanhGia)
                                   .DefaultIfEmpty(0)
                                   .Average()
                    );
                    var hinhAnh = allHinhAnh
                        .Where(h => h.KhoaHocMaKhoaHoc == kh.MaKhoaHoc && h.LaAnhDaiDien)
                        .Select(h => $"{_appSettings.BaseUrl}/api/Image/Get?id={h.MaHinh}")
                        .FirstOrDefault();

                    return new
                    {
                        kh.MaKhoaHoc,
                        kh.TenKhoaHoc,
                        kh.NgayHoc,
                        kh.HocPhi,
                        DaYeuThich = daYeuThich,
                        TongLuotQuanTam = tongLuotQuanTam,
                        TongLuotBinhLuan = tongLuotBinhLuan,
                        SoSaoTrungBinh = soSaoTrungBinh,
                        HinhAnh = hinhAnh
                    };
                }).ToList();

            return Ok(list);
        }

        /// <summary>
        /// Lọc theo giá
        /// </summary>
        [AllowAnonymous]
        [HttpGet]
        public IActionResult LocTheoGia(int luaChon = 0)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var listDaQuanTam = _db.DanhSachQuanTam.Where(ds => ds.UserId == userId)
                                                   .Select(ds => ds.MaKhoaHoc)
                                                   .ToList();

            var allDanhSachQuanTam = _db.DanhSachQuanTam.ToList();
            var allDanhGia = _db.DanhGia.ToList();
            var allHinhAnh = _db.HinhAnhKhoaHoc.ToList();

            var query = BaseQuery();

            switch (luaChon)
            {
                case 1: query = query.OrderByDescending(kh => kh.HocPhi); break;
                case 2: query = query.OrderBy(kh => kh.HocPhi); break;
                case 3: query = query.Where(kh => kh.HocPhi < 5000000).OrderBy(kh => kh.HocPhi); break;
                case 4: query = query.Where(kh => kh.HocPhi >= 5000000 && kh.HocPhi < 7000000).OrderBy(kh => kh.HocPhi); break;
                case 5: query = query.Where(kh => kh.HocPhi >= 7000000).OrderBy(kh => kh.HocPhi); break;
            }

            var list = query
                .AsEnumerable()
                .Select(kh =>
                {
                    var daYeuThich = listDaQuanTam.Contains(kh.MaKhoaHoc);
                    var tongLuotQuanTam = allDanhSachQuanTam.Count(q => q.MaKhoaHoc == kh.MaKhoaHoc);
                    var tongLuotBinhLuan = allDanhGia.Count(d => d.KhoaHocMaKhoaHoc == kh.MaKhoaHoc);
                    var soSaoTrungBinh = LamTron(
                        allDanhGia.Where(d => d.KhoaHocMaKhoaHoc == kh.MaKhoaHoc)
                                   .Select(d => d.SoSaoDanhGia)
                                   .DefaultIfEmpty(0)
                                   .Average()
                    );
                    var hinhAnh = allHinhAnh
                        .Where(h => h.KhoaHocMaKhoaHoc == kh.MaKhoaHoc && h.LaAnhDaiDien)
                        .Select(h => $"{_appSettings.BaseUrl}/api/Image/Get?id={h.MaHinh}")
                        .FirstOrDefault();

                    return new
                    {
                        kh.MaKhoaHoc,
                        kh.TenKhoaHoc,
                        kh.NgayHoc,
                        kh.HocPhi,
                        DaYeuThich = daYeuThich,
                        TongLuotQuanTam = tongLuotQuanTam,
                        TongLuotBinhLuan = tongLuotBinhLuan,
                        SoSaoTrungBinh = soSaoTrungBinh,
                        HinhAnh = hinhAnh
                    };
                }).ToList();

            return Ok(list);
        }
        [AllowAnonymous]
        [HttpGet]
        public IActionResult GetTinTuc()
        {
            var listTinTuc = _db.TinTucTuyenDung
                                .OrderByDescending(t => t.NgayDang)
                                .Select(t => new
                                {
                                    t.MaTinTuc,
                                    t.TieuDeTinTuc,
                                    t.NgayDang,
                                    t.NgayKetThuc,
                                    NoiDung = t.NoiDungTinTuc,
                                    HinhAnh = t.HinhTinTuc != null
                                        ? $"{_appSettings.BaseUrl}/api/Image/GetTinTuc?id={t.MaTinTuc}"
                                        : null
                                })
                                .ToList();
            return Ok(listTinTuc);
        }

        [AllowAnonymous]
        [HttpGet("{id}")]
        public IActionResult GetChiTietTin(int id)
        {
            var tin = _db.TinTucTuyenDung.FirstOrDefault(t => t.MaTinTuc == id);
            if (tin == null)
                return NotFound(new { message = "Không tìm thấy tin tức" });

            var result = new
            {
                tin.MaTinTuc,
                tin.TieuDeTinTuc,
                tin.NgayDang,
                tin.NgayKetThuc,
                NoiDung = tin.NoiDungTinTuc,
                HinhAnh = tin.HinhTinTuc != null
                    ? $"{_appSettings.BaseUrl}/api/Image/GetTinTuc?id={tin.MaTinTuc}"
                    : null
            };
            return Ok(result);
        }
    }
}
