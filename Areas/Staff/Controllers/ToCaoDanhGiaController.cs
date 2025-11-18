using DoAnCoSo_Web.Areas.Staff.ViewModels;
using DoAnCoSo_Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoAnCoSo_Web.Areas.Staff.Controllers
{
    [Area("Staff")]
    [Authorize(Roles = "Staff,Admin")]
    public class ToCaoDanhGiaController : Controller
    {
        private readonly ApplicationDbContext _db;

        public ToCaoDanhGiaController(ApplicationDbContext db)
        {
            _db = db;
        }
        public IActionResult Index()
        {
            var danhSachToCao = _db.ToCaoDanhGia
                .Include(tc => tc.DanhGia).ThenInclude(dg => dg.User)
                .Include(tc => tc.User)
                .Select(tc => new ToCaoDanhGiaViewModel
                {
                    ToCaoId = tc.Id,
                    MaDanhGia = tc.MaDanhGia,
                    NoiDungDanhGia = tc.DanhGia.NoiDungDanhGia,
                    LyDoToCao = tc.LyDo,
                    NguoiGuiDanhGia = tc.DanhGia.User.HoTen,
                    NguoiToCao = tc.User.HoTen,
                    UserDanhGiaId = tc.DanhGia.User.Id,
                    UserToCaoId = tc.User.Id,
                    IsDanhGiaBanned = tc.DanhGia.User.IsBanned,
                    IsToCaoBanned = tc.User.IsBanned
                }).ToList();

            return View(danhSachToCao);
        }

        [HttpPost]
        public IActionResult BanUser(string id)
        {
            var user = _db.Users.FirstOrDefault(u => u.Id == id);
            if (user == null)
            {
                TempData["Error"] = "Không tìm thấy người dùng.";
                return RedirectToAction("Index");
            }

            if (!user.IsBanned)
            {
                user.IsBanned = true;
                _db.SaveChanges();
                TempData["Success"] = $"Đã cấm tài khoản {user.HoTen}.";
            }
            else
            {
                TempData["Info"] = "Người dùng này đã bị cấm trước đó.";
            }

            return RedirectToAction("Index");
        }


        [HttpPost]
        public IActionResult XoaToCao(int id)
        {
            var toCao = _db.ToCaoDanhGia.FirstOrDefault(t => t.Id == id);
            if (toCao != null)
            {
                _db.ToCaoDanhGia.Remove(toCao);
                _db.SaveChanges();
            }
            return RedirectToAction(nameof(Index));
        }

    }
}
