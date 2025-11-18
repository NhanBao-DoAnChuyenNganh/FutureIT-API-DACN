using System.Globalization;
using System.IO;
using DoAnCoSo_Web.Areas.Staff.ViewModels;
using DoAnCoSo_Web.Areas.Student.ViewModels;
using DoAnCoSo_Web.Data;
using DoAnCoSo_Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoAnCoSo_Web.Areas.Staff.Controllers
{
    [Area("Staff")]
    [Authorize(Roles = "Staff,Admin")]
    public class KhoaHocController : Controller
    {
        private readonly ApplicationDbContext _db;

        public KhoaHocController(ApplicationDbContext db)
        {
            _db = db;
        }
        public async Task<IActionResult> Index()
        {
            KhoaHocIndexViewModel model = new KhoaHocIndexViewModel()
            {
                dsKhoaHoc = _db.KhoaHoc.ToList()
            };
            return View(model);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(KhoaHoc khoaHoc, IFormFile? avatarImage, List<IFormFile>? additionalImages)
        {
            if (string.IsNullOrEmpty(khoaHoc.TenKhoaHoc))
            {
                ModelState.AddModelError("TenKhoaHoc", "Tên khóa học không được để trống.");
                return View(khoaHoc);
            }
            if (khoaHoc.HocPhi <= 0)
            {
                ModelState.AddModelError("HocPhi", "Học phí phải lớn hơn 0.");
                return View(khoaHoc);
            }
            if (string.IsNullOrEmpty(khoaHoc.NgayHoc))
            {
                ModelState.AddModelError("NgayHoc", "Vui lòng nhập ngày học.");
                return View(khoaHoc);
            }

            DateTime dtGioBatDau, dtGioKetThuc;

            string format = "h:mm tt";
            if (DateTime.TryParseExact(khoaHoc.GioBatDau, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out dtGioBatDau)
                && DateTime.TryParseExact(khoaHoc.GioKetThuc, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out dtGioKetThuc))
            {
                if (dtGioKetThuc.TimeOfDay <= dtGioBatDau.TimeOfDay)
                {
                    ModelState.AddModelError("GioKetThuc", "Giờ kết thúc phải lớn hơn giờ bắt đầu.");
                    return View(khoaHoc);
                }
            }
            else
            {
                ModelState.AddModelError("", "Định dạng giờ không hợp lệ.");
                return View(khoaHoc);
            }
            // Kiểm tra ảnh đại diện
            if (avatarImage == null || avatarImage.Length == 0)
            {
                ViewData["avatarImageError"] = "Vui lòng chọn ảnh đại diện.";
            }

            // Kiểm tra ảnh bổ sung
            if (additionalImages == null || additionalImages.Count != 3)
            {
                ViewData["additionalImagesError"] = "Vui lòng chọn 3 ảnh.";
            }

            try
            {
                // Lưu khóa học

                _db.KhoaHoc.Add(khoaHoc);
                await _db.SaveChangesAsync();

                // Lưu ảnh đại diện
                if (avatarImage != null && avatarImage.Length > 0)
                {
                    using var ms = new MemoryStream();
                    await avatarImage.CopyToAsync(ms);
                    _db.HinhAnhKhoaHoc.Add(new HinhAnhKhoaHoc
                    {
                        NoiDungHinh = ms.ToArray(),
                        LaAnhDaiDien = true,
                        KhoaHocMaKhoaHoc = khoaHoc.MaKhoaHoc
                    });
                }

                // Lưu 3 ảnh bổ sung
                if (additionalImages != null)
                {
                    foreach (var img in additionalImages)
                    {
                        if (img != null && img.Length > 0)
                        {
                            using var ms = new MemoryStream();
                            await img.CopyToAsync(ms);
                            _db.HinhAnhKhoaHoc.Add(new HinhAnhKhoaHoc
                            {
                                NoiDungHinh = ms.ToArray(),
                                LaAnhDaiDien = false,
                                KhoaHocMaKhoaHoc = khoaHoc.MaKhoaHoc
                            });
                        }
                    }
                }

                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi khi lưu vào cơ sở dữ liệu: " + ex.Message);
                return View(khoaHoc);
            }
        }






        [HttpGet]
        public IActionResult Detail(int id)
        {
            var kH = _db.KhoaHoc.FirstOrDefault(t => t.MaKhoaHoc == id);
            var lishHinhAnh = _db.HinhAnhKhoaHoc.Where(t => t.KhoaHocMaKhoaHoc == id).ToList();

            if (kH == null)
            {
                return NotFound("Không tìm thấy khóa học");
            }

            // Tạo ViewModel
            var model = new ChiTietKhoaHocViewModel
            {
                KhoaHoc = kH,
                HinhAnhKhoaHoc = lishHinhAnh
            };

            return View(model);
        }

        [HttpGet]
        public IActionResult DeleteConfirm(int id)
        {
            var khoaHoc = _db.KhoaHoc.Find(id);
            if (khoaHoc == null)
            {
                return NotFound();
            }
            return View(khoaHoc); // Trả về view xác nhận xóa
        }
        [HttpPost]
        public IActionResult Delete(int id)
        {
            var khoaHoc = _db.KhoaHoc.Find(id);
            if (khoaHoc == null)
            {
                return Json(new { success = false });
            }

            // Xóa ảnh liên quan
            var hinhAnhKhoaHoc = _db.HinhAnhKhoaHoc.Where(h => h.KhoaHocMaKhoaHoc == id).ToList();
            _db.HinhAnhKhoaHoc.RemoveRange(hinhAnhKhoaHoc);

            // Xóa khóa học
            _db.KhoaHoc.Remove(khoaHoc);
            _db.SaveChanges();

            return Json(new { success = true });
        }


        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var khoaHoc = await _db.KhoaHoc.FindAsync(id);
            if (khoaHoc == null)
                return NotFound();

            return View(khoaHoc);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(KhoaHoc khoaHoc)
        {
            if (string.IsNullOrEmpty(khoaHoc.TenKhoaHoc))
            {
                ModelState.AddModelError("TenKhoaHoc", "Tên khóa học không được để trống.");
                return View(khoaHoc);
            }
            if (khoaHoc.HocPhi <= 0)
            {
                ModelState.AddModelError("HocPhi", "Học phí phải lớn hơn 0.");
                return View(khoaHoc);
            }
            if (string.IsNullOrEmpty(khoaHoc.NgayHoc))
            {
                ModelState.AddModelError("NgayHoc", "Vui lòng nhập ngày học.");
                return View(khoaHoc);
            }
            DateTime dtGioBatDau, dtGioKetThuc;
            string format = "h:mm tt";
            if (DateTime.TryParseExact(khoaHoc.GioBatDau, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out dtGioBatDau)
                && DateTime.TryParseExact(khoaHoc.GioKetThuc, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out dtGioKetThuc))
            {
                if (dtGioKetThuc.TimeOfDay <= dtGioBatDau.TimeOfDay)
                {
                    ModelState.AddModelError("GioKetThuc", "Giờ kết thúc phải lớn hơn giờ bắt đầu.");
                    return View(khoaHoc);
                }
            }
            else
            {
                ModelState.AddModelError("", "Định dạng giờ không hợp lệ.");
                return View(khoaHoc);
            }
            var existing = await _db.KhoaHoc.FindAsync(khoaHoc.MaKhoaHoc);
            if (existing == null)
                return NotFound();

            existing.TenKhoaHoc = khoaHoc.TenKhoaHoc;
            existing.HocPhi = khoaHoc.HocPhi;
            existing.MoTa = khoaHoc.MoTa;
            existing.NgayHoc = khoaHoc.NgayHoc;
            existing.GioBatDau = khoaHoc.GioBatDau;
            existing.GioKetThuc = khoaHoc.GioKetThuc;

            await _db.SaveChangesAsync();
            return RedirectToAction("EditImages", new { id = khoaHoc.MaKhoaHoc });

        }

        [HttpGet]
        public async Task<IActionResult> EditImages(int id)
        {
            var hinhAnh = await _db.HinhAnhKhoaHoc
                .Where(h => h.KhoaHocMaKhoaHoc == id)
                .ToListAsync();

            ViewBag.KhoaHocId = id;
            return View(hinhAnh);
        }

        [HttpPost]
        public async Task<IActionResult> EditImages(int id, IFormFile? avatarImage, List<IFormFile>? additionalImages)
        {
            var existingImages = await _db.HinhAnhKhoaHoc.Where(h => h.KhoaHocMaKhoaHoc == id).ToListAsync();

            // Xóa ảnh cũ (nếu có ảnh mới)
            if (avatarImage != null)
                _db.HinhAnhKhoaHoc.RemoveRange(existingImages.Where(h => h.LaAnhDaiDien));
            if (additionalImages != null && additionalImages.Any())
                _db.HinhAnhKhoaHoc.RemoveRange(existingImages.Where(h => !h.LaAnhDaiDien));

            // Thêm ảnh mới nếu có
            if (avatarImage != null)
            {
                using var ms = new MemoryStream();
                await avatarImage.CopyToAsync(ms);
                _db.HinhAnhKhoaHoc.Add(new HinhAnhKhoaHoc
                {
                    NoiDungHinh = ms.ToArray(),
                    LaAnhDaiDien = true,
                    KhoaHocMaKhoaHoc = id
                });
            }

            if (additionalImages != null)
            {
                foreach (var img in additionalImages)
                {
                    if (img.Length > 0)
                    {
                        using var ms = new MemoryStream();
                        await img.CopyToAsync(ms);
                        _db.HinhAnhKhoaHoc.Add(new HinhAnhKhoaHoc
                        {
                            NoiDungHinh = ms.ToArray(),
                            LaAnhDaiDien = false,
                            KhoaHocMaKhoaHoc = id
                        });
                    }
                }
            }

            await _db.SaveChangesAsync();
            return RedirectToAction("Index");
        }





    }
}
