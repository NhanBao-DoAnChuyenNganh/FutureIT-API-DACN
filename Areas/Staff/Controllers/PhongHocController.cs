using DoAnCoSo_Web.Data;
using DoAnCoSo_Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DoAnCoSo_Web.Areas.Staff.Controllers
{
    [Area("Staff")]
    [Authorize(Roles = "Staff,Admin")]
    public class PhongHocController : Controller
    {
        private readonly ApplicationDbContext _db;

        public PhongHocController(ApplicationDbContext db)
        {
            _db = db;
        }
        public IActionResult Index()
        {
            var danhSachPhongHoc = _db.PhongHoc.ToList();
            return View(danhSachPhongHoc);
        }
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(PhongHoc phongHoc)
        {
            // Kiểm tra dữ liệu
            if (string.IsNullOrEmpty(phongHoc.TenPhongHoc))
            {
                ModelState.AddModelError("TenPhongHoc", "Tên phòng học không được để trống.");
                return View(phongHoc);
            }

            try
            {
                // Lưu vào database
                _db.PhongHoc.Add(phongHoc);
                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Đã xảy ra lỗi khi lưu vào cơ sở dữ liệu: " + ex.Message);
                return View(phongHoc);
            }
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var phongHoc = await _db.PhongHoc.FindAsync(id);
            if (phongHoc == null)
            {
                return NotFound();
            }
            return View(phongHoc);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(PhongHoc phongHoc)
        {
            if (string.IsNullOrEmpty(phongHoc.TenPhongHoc))
            {
                ModelState.AddModelError("TenPhongHoc", "Tên phòng học không được để trống.");
                return View(phongHoc);
            }

            var existing = await _db.PhongHoc.FindAsync(phongHoc.MaPhongHoc);
            if (existing == null)
            {
                return NotFound();
            }

            try
            {
                existing.SucChua = phongHoc.SucChua;
                existing.TenPhongHoc = phongHoc.TenPhongHoc;
                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi khi lưu vào cơ sở dữ liệu: " + ex.Message);
                return View(phongHoc);
            }
        }
        [HttpPost]
        public async Task<JsonResult> Delete(int id)
        {
            var phongHoc = await _db.PhongHoc.FindAsync(id);
            if (phongHoc == null)
            {
                return Json(new { success = false, message = "Không tìm thấy phòng học." });
            }

            try
            {
                _db.PhongHoc.Remove(phongHoc);
                await _db.SaveChangesAsync();
                return Json(new { success = true, message = "Xóa thành công." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi xóa: " + ex.Message });
            }

        }

    }
}
