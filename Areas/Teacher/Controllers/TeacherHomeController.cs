using System.Security.Claims;
using DoAnCoSo_Web.Areas.Teacher.ViewModels;
using DoAnCoSo_Web.Data;
using DoAnCoSo_Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoAnCoSo_Web.Areas.Teacher.Controllers
{
    [Area("Teacher")]
    [Authorize(Roles = "Teacher")]
    public class TeacherHomeController : Controller
    {
        private readonly ApplicationDbContext _db;
        public TeacherHomeController(ApplicationDbContext db)
        {
            _db = db;
        }
        public IActionResult Index(DateTime? startDate)
        {
            string userID = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Nếu không có startDate -> lấy thứ 2 tuần hiện tại
            DateTime today = startDate ?? DateTime.Today;
            // nếu là Chủ nhật -> lùi đúng 6 ngày để về t2. Các ngày khác -> lùi về t2
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

            ViewBag.CurrentWeekStart = weekStart;

            IndexViewModels model = new IndexViewModels()
            {
                listLopHocDangDay = _db.ChiTietGiangDay.Where(m => m.UserId == userID && m.LopHoc.NgayKetThuc > DateTime.Now)
                .Include(m => m.LopHoc).ThenInclude(m => m.PhongHoc)
                .Include(m => m.LopHoc).ThenInclude(m => m.KhoaHoc).ThenInclude(m => m.HinhAnhKhoaHoc).ToList(),
                                    
            };
            return View(model);
        }

        public IActionResult XemChiTietLop(int MaLop)
        {
            XemChiTietLopViewModels model = new XemChiTietLopViewModels()
            {
                LopHoc = _db.LopHoc.Include(p => p.KhoaHoc).ThenInclude(p => p.HinhAnhKhoaHoc)
                .Include(p => p.PhongHoc).FirstOrDefault(l => l.MaLopHoc == MaLop),
                listSVTrongLop = _db.ChiTietHocTap.Where(p => p.MaLopHoc == MaLop).Include(p => p.User).ToList()
            };
            return View(model);
        }

        [HttpPost]
        public IActionResult LuuNhanXet(string IDHocVien, int MaLop, string Diem, string NhanXet)
        {
            ChiTietHocTap ctht = _db.ChiTietHocTap.FirstOrDefault(p => p.UserId == IDHocVien && p.MaLopHoc == MaLop);
            ctht.NhanXetCuaGiaoVien = NhanXet;
            ctht.DiemTongKet = int.Parse(Diem);
            _db.SaveChanges();

            return Json(new { success = true, message = "Lưu nhận xét thành công!" });
        }
    }
}
