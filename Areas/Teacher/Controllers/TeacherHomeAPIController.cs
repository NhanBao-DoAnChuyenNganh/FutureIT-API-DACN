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

    }
}
