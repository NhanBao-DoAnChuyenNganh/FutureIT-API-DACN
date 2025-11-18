using DoAnCoSo_Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoAnCoSo_Web.Areas.Staff.Controllers
{
    [Area("Staff")]
    [Authorize(Roles = "Staff,Admin")]
    public class HoaDonController : Controller
    {
        private readonly ApplicationDbContext _db;

        public HoaDonController(ApplicationDbContext db)
        {
            _db = db;
        }
        public IActionResult Index()
        {
            var danhSachHoaDonNo = _db.HoaDon
                .Include(h => h.PhieuDangKyKhoaHoc)
                    .ThenInclude(p => p.KhoaHoc)
                .Include(h => h.PhieuDangKyKhoaHoc)
                    .ThenInclude(p => p.User)
                .Include(h => h.User) // Nhân viên lập hóa đơn
                .Where(h => h.PhieuDangKyKhoaHoc.Any(p => p.TrangThaiThanhToan == "Thanh toán 50%"))
                .ToList();

            return View(danhSachHoaDonNo);
        }
        [HttpPost]
        public IActionResult ThanhToanLan2(int maHoaDon)
        {
            var hoaDon = _db.HoaDon
                .Include(h => h.PhieuDangKyKhoaHoc)
                    .ThenInclude(p => p.KhoaHoc)
                .FirstOrDefault(h => h.MaHoaDon == maHoaDon);

            if (hoaDon == null)
                return NotFound();

            var phieuDangKyCanCapNhat = hoaDon.PhieuDangKyKhoaHoc
                .Where(p => p.TrangThaiThanhToan == "Thanh toán 50%")
                .ToList();

            if (!phieuDangKyCanCapNhat.Any())
                return RedirectToAction("Index");

            // Cập nhật các phiếu
            foreach (var phieu in phieuDangKyCanCapNhat)
            {
                phieu.TrangThaiThanhToan = "Hoàn tất học phí";
            }

            // Cập nhật hóa đơn
            var tongHocPhi = phieuDangKyCanCapNhat.Sum(p => p.KhoaHoc.HocPhi);
            hoaDon.TienDongLan2 = tongHocPhi - hoaDon.TienDongLan1;
            hoaDon.NgayDongLan2 = DateTime.Now;

            _db.SaveChanges();

            return RedirectToAction("Index");
        }



    }
}
