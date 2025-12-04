using DoAnCoSo_Web.Data;
using DoAnCoSo_Web.Models.AppSettings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace DoAnCoSo_Web_TestAPI.Areas.Teacher.Controllers
{
    [Area("Teacher")]
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "JwtBearer", Roles = "Teacher")]
    public class TeacherHomeApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly AppSettings _appSettings;

        public TeacherHomeApiController(ApplicationDbContext db, IOptions<AppSettings> options)
        {
            _db = db;
            _appSettings = options.Value;
        }

        // ===============================
        // 1) API: Danh sách lớp đang dạy
        // ===============================

        [HttpGet]
        public IActionResult GetLopDangDay(DateTime? startDate)
        {
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            DateTime today = startDate ?? DateTime.Today;

            int x = (today.DayOfWeek == DayOfWeek.Sunday)
                ? -6
                : -(int)today.DayOfWeek + 1;

            DateTime weekStart = today.AddDays(x);

            var list = _db.ChiTietGiangDay
                .Where(m => m.UserId == userId && m.LopHoc.NgayKetThuc > DateTime.Now)
                .Include(m => m.LopHoc).ThenInclude(l => l.PhongHoc)
                .Include(m => m.LopHoc).ThenInclude(l => l.KhoaHoc).ThenInclude(k => k.HinhAnhKhoaHoc)
                .Select(m => new
                {
                    m.LopHoc.MaLopHoc,
                    TenKhoaHoc = m.LopHoc.KhoaHoc.TenKhoaHoc,
                    NgayKhaiGiang = m.LopHoc.NgayKhaiGiang,
                    NgayKetThuc = m.LopHoc.NgayKetThuc,
                    PhongHoc = m.LopHoc.PhongHoc.TenPhongHoc,
                    NgayHoc = m.LopHoc.KhoaHoc.NgayHoc,
                    HinhAnh = m.LopHoc.KhoaHoc.HinhAnhKhoaHoc
                        .Where(h => h.LaAnhDaiDien)
                        .Select(h => $"{_appSettings.BaseUrl}/api/Image/Get?id={h.MaHinh}")
                        .FirstOrDefault()
                })
                .ToList();

            return Ok(new
            {
                weekStart,
                list
            });
        }

        // =====================================
        // 2) API: Xem chi tiết 1 lớp + danh sách SV
        // =====================================
        [HttpGet("{maLop}")]
        public IActionResult GetChiTietLop(int maLop)
        {
            var lop = _db.LopHoc
                .Include(p => p.PhongHoc)
                .Include(p => p.KhoaHoc).ThenInclude(k => k.HinhAnhKhoaHoc)
                .FirstOrDefault(l => l.MaLopHoc == maLop);

            if (lop == null)
                return NotFound(new { message = "Không tìm thấy lớp học" });

            var listSV = _db.ChiTietHocTap
                .Where(ct => ct.MaLopHoc == maLop)
                .Include(ct => ct.User)
                .Select(ct => new
                {
                    ct.User.HoTen,
                    ct.User.Email,
                    ct.User.Id,
                    Avatar = $"{_appSettings.BaseUrl}/api/Image/GetAvatar?userId={ct.User.Id}",
                    ct.NhanXetCuaGiaoVien,
                    ct.DiemTongKet
                })
                .ToList();

            return Ok(new
            {
                Lop = new
                {
                    lop.MaLopHoc,
                    lop.NgayKhaiGiang,
                    lop.NgayKetThuc,
                    lop.PhongHoc.TenPhongHoc,
                    TenKhoaHoc = lop.KhoaHoc.TenKhoaHoc,
                    HinhAnh = lop.KhoaHoc.HinhAnhKhoaHoc
                        .Where(h => h.LaAnhDaiDien)
                        .Select(h => $"{_appSettings.BaseUrl}/api/Image/Get?id={h.MaHinh}")
                        .FirstOrDefault()
                },
                DanhSachHocVien = listSV
            });
        }

        // ===============================
        // 3) API: Lưu nhận xét
        // ===============================

        public class LuuNhanXetDTO
        {
            public string IDHocVien { get; set; }
            public int MaLop { get; set; }
            public int Diem { get; set; }
            public string NhanXet { get; set; }
        }

        [HttpPost]
        public IActionResult LuuNhanXet([FromBody] LuuNhanXetDTO data)
        {
            var ct = _db.ChiTietHocTap
                .FirstOrDefault(p => p.UserId == data.IDHocVien && p.MaLopHoc == data.MaLop);

            if (ct == null)
                return NotFound(new { message = "Không tìm thấy học viên trong lớp" });

            ct.NhanXetCuaGiaoVien = data.NhanXet;
            ct.DiemTongKet = data.Diem;

            _db.SaveChanges();

            return Ok(new { success = true, message = "Lưu nhận xét thành công!" });
        }

        // ===============================
        // 4) API: Lấy danh sách điểm danh theo ngày
        // ===============================
        [HttpGet("{maLop}")]
        public IActionResult GetDiemDanhTheoNgay(int maLop, DateTime? ngay)
        {
            DateTime ngayDiemDanh = ngay ?? DateTime.Today;

            var danhSachHocVien = _db.ChiTietHocTap
                .Where(ct => ct.MaLopHoc == maLop)
                .Include(ct => ct.User)
                .Select(ct => new
                {
                    ct.User.Id,
                    ct.User.HoTen,
                    ct.User.Email,
                    Avatar = $"{_appSettings.BaseUrl}/api/Image/GetAvatar?userId={ct.User.Id}",
                    DiemDanh = _db.DiemDanh
                        .Where(dd => dd.MaLopHoc == maLop 
                                  && dd.UserId == ct.User.Id 
                                  && dd.NgayDiemDanh.Date == ngayDiemDanh.Date)
                        .Select(dd => new
                        {
                            dd.MaDiemDanh,
                            dd.CoMat,
                            dd.GhiChu
                        })
                        .FirstOrDefault()
                })
                .ToList();

            return Ok(new
            {
                NgayDiemDanh = ngayDiemDanh,
                DanhSach = danhSachHocVien
            });
        }

        // ===============================
        // 5) API: Lưu điểm danh
        // ===============================
        public class DiemDanhDTO
        {
            public int MaLop { get; set; }
            public string UserId { get; set; }
            public DateTime NgayDiemDanh { get; set; }
            public bool CoMat { get; set; }
            public string? GhiChu { get; set; }
        }

        [HttpPost]
        public IActionResult LuuDiemDanh([FromBody] DiemDanhDTO data)
        {
            // Kiểm tra giáo viên có dạy lớp này không
            string teacherId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var giangDay = _db.ChiTietGiangDay
                .Any(gd => gd.UserId == teacherId && gd.MaLopHoc == data.MaLop);

            if (!giangDay)
                return Forbid();

            // Kiểm tra học viên có trong lớp không
            var hocVien = _db.ChiTietHocTap
                .Any(ct => ct.UserId == data.UserId && ct.MaLopHoc == data.MaLop);

            if (!hocVien)
                return NotFound(new { message = "Học viên không thuộc lớp này" });

            // Tìm bản ghi điểm danh (nếu đã có)
            var diemDanh = _db.DiemDanh
                .FirstOrDefault(dd => dd.MaLopHoc == data.MaLop 
                                   && dd.UserId == data.UserId 
                                   && dd.NgayDiemDanh.Date == data.NgayDiemDanh.Date);

            if (diemDanh != null)
            {
                // Cập nhật
                diemDanh.CoMat = data.CoMat;
                diemDanh.GhiChu = data.GhiChu;
            }
            else
            {
                // Thêm mới
                diemDanh = new DoAnCoSo_Web.Models.DiemDanh
                {
                    MaLopHoc = data.MaLop,
                    UserId = data.UserId,
                    NgayDiemDanh = data.NgayDiemDanh.Date,
                    CoMat = data.CoMat,
                    GhiChu = data.GhiChu
                };
                _db.DiemDanh.Add(diemDanh);
            }

            _db.SaveChanges();

            return Ok(new { success = true, message = "Điểm danh thành công!" });
        }

        // ===============================
        // 6) API: Lưu điểm danh hàng loạt
        // ===============================
        public class DiemDanhHangLoatDTO
        {
            public int MaLop { get; set; }
            public DateTime NgayDiemDanh { get; set; }
            public List<DiemDanhItemDTO> DanhSach { get; set; }
        }

        public class DiemDanhItemDTO
        {
            public string UserId { get; set; }
            public bool CoMat { get; set; }
            public string? GhiChu { get; set; }
        }

        [HttpPost]
        public IActionResult LuuDiemDanhHangLoat([FromBody] DiemDanhHangLoatDTO data)
        {
            // Kiểm tra giáo viên có dạy lớp này không
            string teacherId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var giangDay = _db.ChiTietGiangDay
                .Any(gd => gd.UserId == teacherId && gd.MaLopHoc == data.MaLop);

            if (!giangDay)
                return Forbid();

            foreach (var item in data.DanhSach)
            {
                var diemDanh = _db.DiemDanh
                    .FirstOrDefault(dd => dd.MaLopHoc == data.MaLop
                                       && dd.UserId == item.UserId
                                       && dd.NgayDiemDanh.Date == data.NgayDiemDanh.Date);

                if (diemDanh != null)
                {
                    diemDanh.CoMat = item.CoMat;
                    diemDanh.GhiChu = item.GhiChu;
                }
                else
                {
                    diemDanh = new DoAnCoSo_Web.Models.DiemDanh
                    {
                        MaLopHoc = data.MaLop,
                        UserId = item.UserId,
                        NgayDiemDanh = data.NgayDiemDanh.Date,
                        CoMat = item.CoMat,
                        GhiChu = item.GhiChu
                    };
                    _db.DiemDanh.Add(diemDanh);
                }
            }

            _db.SaveChanges();

            return Ok(new { success = true, message = "Điểm danh hàng loạt thành công!" });
        }

        // ===============================
        // 7) API: Thống kê điểm danh của học viên
        // ===============================
        [HttpGet("{maLop}/{userId}")]
        public IActionResult GetThongKeDiemDanh(int maLop, string userId)
        {
            var danhSachDiemDanh = _db.DiemDanh
                .Where(dd => dd.MaLopHoc == maLop && dd.UserId == userId)
                .OrderByDescending(dd => dd.NgayDiemDanh)
                .Select(dd => new
                {
                    dd.NgayDiemDanh,
                    dd.CoMat,
                    dd.GhiChu
                })
                .ToList();

            int tongBuoi = danhSachDiemDanh.Count;
            int soLanCoMat = danhSachDiemDanh.Count(dd => dd.CoMat);
            int soLanVang = tongBuoi - soLanCoMat;
            double tyLeCoMat = tongBuoi > 0 ? (double)soLanCoMat / tongBuoi * 100 : 0;

            return Ok(new
            {
                TongBuoi = tongBuoi,
                SoLanCoMat = soLanCoMat,
                SoLanVang = soLanVang,
                TyLeCoMat = Math.Round(tyLeCoMat, 2),
                ChiTiet = danhSachDiemDanh
            });
        }

    }
}
