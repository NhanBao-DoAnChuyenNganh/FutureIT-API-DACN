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
        [AllowAnonymous]
        [HttpGet("{id}")]
        public IActionResult GetChiTietKhoaHoc(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var kh = _db.KhoaHoc
                .Include(k => k.HinhAnhKhoaHoc)
                .FirstOrDefault(k => k.MaKhoaHoc == id);

            if (kh == null)
                return NotFound(new { message = "Không tìm thấy khóa học" });

            // Lấy danh sách hình ảnh
            var listHinh = _db.HinhAnhKhoaHoc
                .Where(h => h.KhoaHocMaKhoaHoc == id)
                .Select(h => $"{_appSettings.BaseUrl}/api/Image/Get?id={h.MaHinh}")
                .ToList();

            // Lấy toàn bộ đánh giá ra memory
            var listDanhGia = _db.DanhGia
                .Where(d => d.KhoaHocMaKhoaHoc == id)
                .Include(d => d.User)
                .ToList();

            bool daQuanTam = _db.DanhSachQuanTam.Any(q => q.MaKhoaHoc == id && q.UserId == userId);

            // Tính số sao trung bình
            var soSaoTB = LamTron(
                listDanhGia
                    .Select(d => d.SoSaoDanhGia)
                    .DefaultIfEmpty(0)
                    .Average()
            );

            // Map danh sách đánh giá sang object cần thiết
            var danhGiaDto = listDanhGia
                .Select(d => new
                {
                    d.MaDanhGia,
                    d.SoSaoDanhGia,
                    d.NoiDungDanhGia,
                    d.NgayDanhGia,
                    User = d.User.HoTen
                })
                .ToList();

            return Ok(new
            {
                kh.MaKhoaHoc,
                kh.TenKhoaHoc,
                kh.NgayHoc,
                kh.GioBatDau,
                kh.GioKetThuc,
                kh.HocPhi,
                kh.MoTa,
                DaYeuThich = daQuanTam,
                SoSaoTrungBinh = soSaoTB,
                TongLuotBinhLuan = danhGiaDto.Count,
                HinhAnh = listHinh,
                DanhGia = danhGiaDto
            });
        }

        [Authorize(AuthenticationSchemes = "JwtBearer", Roles = "Student")]
        [HttpPost]
        public IActionResult ToggleQuanTam([FromBody] int maKhoaHoc)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var exists = _db.DanhSachQuanTam
                .FirstOrDefault(x => x.MaKhoaHoc == maKhoaHoc && x.UserId == userId);

            if (exists != null)
            {
                _db.DanhSachQuanTam.Remove(exists);
                _db.SaveChanges();
                return Ok(new { success = true, message = "Đã xóa khỏi danh sách quan tâm" });
            }

            _db.DanhSachQuanTam.Add(new DanhSachQuanTam
            {
                MaKhoaHoc = maKhoaHoc,
                UserId = userId
            });

            _db.SaveChanges();
            return Ok(new { success = true, message = "Đã thêm vào danh sách quan tâm" });
        }
        [Authorize(AuthenticationSchemes = "JwtBearer", Roles = "Student")]
        [HttpGet]
        public IActionResult GetDanhSachQuanTam()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var list = _db.DanhSachQuanTam
                .Include(ds => ds.KhoaHoc)
                .ThenInclude(kh => kh.HinhAnhKhoaHoc)
                .Where(ds => ds.UserId == userId)
                .Select(kh => new
                {
                    kh.KhoaHoc.MaKhoaHoc,
                    kh.KhoaHoc.TenKhoaHoc,
                    kh.KhoaHoc.HocPhi,
                    HinhAnh = kh.KhoaHoc.HinhAnhKhoaHoc
                        .Where(h => h.LaAnhDaiDien)
                        .Select(h => $"{_appSettings.BaseUrl}/api/Image/Get?id={h.MaHinh}")
                        .FirstOrDefault()
                })
                .ToList();

            return Ok(list);
        }
        public class GuiDanhGiaDTO
        {
            public int MaKhoaHoc { get; set; }
            public int SoSao { get; set; }
            public string NoiDung { get; set; }
        }

        [Authorize(AuthenticationSchemes = "JwtBearer", Roles = "Student")]
        [HttpPost]
        public IActionResult GuiDanhGia([FromBody] GuiDanhGiaDTO model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var danhGia = new DanhGia
            {
                KhoaHocMaKhoaHoc = model.MaKhoaHoc,
                SoSaoDanhGia = model.SoSao,
                NoiDungDanhGia = model.NoiDung,
                NgayDanhGia = DateTime.Now,
                UserUserId = userId
            };

            _db.DanhGia.Add(danhGia);
            _db.SaveChanges();

            return Ok(new { success = true, message = "Gửi đánh giá thành công" });
        }
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetTeacher()
        {
            // Lấy RoleId của role Teacher
            var roleId = await _db.Roles
                .Where(r => r.Name == "Teacher")
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            if (roleId == null)
            {
                return NotFound(new { message = "Không tìm thấy role Teacher" });
            }

            // Lấy danh sách user có role Teacher
            var listTeacher = await _db.Users
                .Where(u => _db.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == roleId))
                .ToListAsync();

            var result = listTeacher.Select(u => new
            {
                HoTen = u.HoTen,
                DiaChi = u.DiaChi,
                Avatar = u.Avatar != null
                 ? $"{_appSettings.BaseUrl}/api/Image/GetAvatar?userId={u.Id}"
                 : "/image/avatar.png",

                ChuyenNganh = u.ChuyenNganhMaChuyenNganh != null
                    ? _db.ChuyenNganh
                        .Where(c => c.MaChuyenNganh == u.ChuyenNganhMaChuyenNganh)
                        .Select(c => c.TenChuyenNganh)
                        .FirstOrDefault()
                    : "Chưa cập nhật"
            }).ToList();

            return Ok(result);
        }

        [HttpGet]
        public IActionResult GetKhoaHocDaDangKy(DateTime? startDate)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            DateTime today = startDate ?? DateTime.Today;
            int x;
            if (today.DayOfWeek == DayOfWeek.Sunday)
            {
                x = -6;
            }
            else
            {
                x = -(int)today.DayOfWeek + 1;
            }
            DateTime weekStart = today.AddDays(x);
            // Chờ xếp lớp: listPhieuDangKy
            var listPhieuDangKy = _db.PhieuDangKyKhoaHoc
                .Where(p => p.UserId == userId)
                .Include(p => p.KhoaHoc).ThenInclude(k => k.HinhAnhKhoaHoc)
                .Where(p => p.TrangThaiDangKy == "Chờ xử lý")
                .Select(p => new
                {
                    p.MaKhoaHoc,
                    p.KhoaHoc.TenKhoaHoc,
                    p.TrangThaiDangKy,
                    p.TrangThaiThanhToan,
                    HinhAnh = p.KhoaHoc.HinhAnhKhoaHoc
                        .Where(h => h.LaAnhDaiDien)
                        .Select(h => $"{_appSettings.BaseUrl}/api/Image/Get?id={h.MaHinh}")
                        .FirstOrDefault()
                })
                .ToList();

            // Đang học: listDangHoc
            var listDangHoc = _db.ChiTietHocTap
                .Where(m => m.UserId == userId && m.LopHoc.NgayKetThuc > DateTime.Now)
                .Include(m => m.LopHoc)
                    .ThenInclude(l => l.KhoaHoc)
                        .ThenInclude(k => k.HinhAnhKhoaHoc)
                .Include(m => m.LopHoc)
                    .ThenInclude(l => l.PhongHoc)
                .Select(m => new
                {
                    m.LopHoc.MaLopHoc,
                    m.LopHoc.KhoaHoc.TenKhoaHoc,
                    m.LopHoc.NgayKhaiGiang,
                    m.LopHoc.NgayKetThuc,
                    PhongHoc = m.LopHoc.PhongHoc.TenPhongHoc,
                    HinhAnh = m.LopHoc.KhoaHoc.HinhAnhKhoaHoc
                        .Where(h => h.LaAnhDaiDien)
                        .Select(h => $"{_appSettings.BaseUrl}/api/Image/Get?id={h.MaHinh}")
                        .FirstOrDefault()
                }).ToList();

            // Đã học: listDaHoc
            var listDaHoc = _db.ChiTietHocTap
                .Where(m => m.UserId == userId && m.LopHoc.NgayKetThuc <= DateTime.Now)
                .Include(m => m.LopHoc)
                    .ThenInclude(l => l.KhoaHoc)
                        .ThenInclude(k => k.HinhAnhKhoaHoc)
                .Select(m => new
                {
                    m.LopHoc.MaLopHoc,
                    m.LopHoc.KhoaHoc.TenKhoaHoc,
                    m.LopHoc.NgayKhaiGiang,
                    m.LopHoc.NgayKetThuc,
                    HinhAnh = m.LopHoc.KhoaHoc.HinhAnhKhoaHoc
                        .Where(h => h.LaAnhDaiDien)
                        .Select(h => $"{_appSettings.BaseUrl}/api/Image/Get?id={h.MaHinh}")
                        .FirstOrDefault(),
                    m.NhanXetCuaGiaoVien,
                    m.DiemTongKet
                }).ToList();

            // Chưa thanh toán hết: listConNo
            var listConNo = _db.PhieuDangKyKhoaHoc
                .Where(p => p.UserId == userId && p.TrangThaiThanhToan == "Thanh toán 50%")
                .Include(p => p.KhoaHoc).ThenInclude(k => k.HinhAnhKhoaHoc)
                .Include(p => p.HoaDon)
                .Select(p => new
                {
                    p.MaKhoaHoc,
                    p.KhoaHoc.TenKhoaHoc,
                    p.HoaDon.TienDongLan1,
                    p.KhoaHoc.HocPhi,
                    HinhAnh = p.KhoaHoc.HinhAnhKhoaHoc
                        .Where(h => h.LaAnhDaiDien)
                        .Select(h => $"{_appSettings.BaseUrl}/api/Image/Get?id={h.MaHinh}")
                        .FirstOrDefault()
                }).ToList();

            return Ok(new
            {
                listPhieuDangKy,
                listDangHoc,
                listDaHoc,
                listConNo
            });
        }
    }
}
