using DoAnCoSo_Web.Data;
using DoAnCoSo_Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DoAnCoSo_Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ChuyenNganhController : Controller
    {
        private readonly ApplicationDbContext _db;

        public ChuyenNganhController(ApplicationDbContext db)
        {
            _db = db;
        }
        public IActionResult Index()
        {
            var danhSachChuyenNganh = _db.ChuyenNganh.ToList();
            return View(danhSachChuyenNganh);
        }
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(ChuyenNganh chuyenNganh)
        {
            // Kiểm tra dữ liệu
            if (string.IsNullOrEmpty(chuyenNganh.TenChuyenNganh))
            {
                ModelState.AddModelError("TenChuyenNganh", "Tên chuyên ngành không được để trống.");
                return View(chuyenNganh);
            }

            try
            {
                // Lưu vào database
                _db.ChuyenNganh.Add(chuyenNganh);
                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Đã xảy ra lỗi khi lưu vào cơ sở dữ liệu: " + ex.Message);
                return View(chuyenNganh);
            }
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var phongHoc = await _db.ChuyenNganh.FindAsync(id);
            if (phongHoc == null)
            {
                return NotFound();
            }
            return View(phongHoc);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(ChuyenNganh chuyenNganh)
        {
            if (string.IsNullOrEmpty(chuyenNganh.TenChuyenNganh))
            {
                ModelState.AddModelError("TenChuyenNganh", "Tên chuyên ngành không được để trống.");
                return View(chuyenNganh);
            }

            var existing = await _db.ChuyenNganh.FindAsync(chuyenNganh.MaChuyenNganh);
            if (existing == null)
            {
                return NotFound();
            }

            try
            {
                existing.TenChuyenNganh = chuyenNganh.TenChuyenNganh;
                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi khi lưu vào cơ sở dữ liệu: " + ex.Message);
                return View(chuyenNganh);
            }
        }
        [HttpPost]
        public async Task<JsonResult> Delete(int id)
        {
            var chuyenNganh = await _db.ChuyenNganh.FindAsync(id);
            if (chuyenNganh == null)
            {
                return Json(new { success = false, message = "Không tìm thấy phòng học." });
            }

            try
            {
                _db.ChuyenNganh.Remove(chuyenNganh);
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
