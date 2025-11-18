using DoAnCoSo_Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using DoAnCoSo_Web.Areas.Identity.Pages.Account;
using System.ComponentModel.DataAnnotations;
using DoAnCoSo_Web.Data;
using Microsoft.EntityFrameworkCore;

namespace DoAnCoSo_Web.Areas.Staff.Pages.Students
{
    [Area("Staff")]

    [Authorize(Roles = "Staff,Admin")]
    public class AddUserModel : PageModel
    {
        private readonly UserManager<User> _userManager;
        private readonly IUserStore<User> _userStore;
        private readonly IUserEmailStore<User> _emailStore;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _db;
        public AddUserModel(
            UserManager<User> userManager,
            IUserStore<User> userStore,
            RoleManager<IdentityRole> roleManager, ApplicationDbContext db)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _roleManager = roleManager;
            _db = db;
        }
        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required]
            public string HoTen { get; set; }

            [Required, EmailAddress]
            public string Email { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }

            public string SDT { get; set; }
            public string DiaChi { get; set; }

            public string TrangThaiThanhToan { get; set; }
            [Required]
            public int MaKhoaHoc { get; set; }
        }

        public async Task OnGetAsync()
        {
            var khoaHocs = await _db.KhoaHoc.ToListAsync();
            ViewData["DanhSachKhoaHoc"] = new SelectList(khoaHocs, "MaKhoaHoc", "TenKhoaHoc");

            // Khởi tạo Input nếu null (tránh NullReferenceException nếu form load lần đầu)
            if (Input == null)
            {
                Input = new InputModel();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var khoaHocs = await _db.KhoaHoc.ToListAsync();
            ViewData["DanhSachKhoaHoc"] = new SelectList(khoaHocs, "MaKhoaHoc", "TenKhoaHoc");

            // Khởi tạo Input nếu null (tránh NullReferenceException nếu form load lần đầu)
            if (Input == null)
            {
                Input = new InputModel();
            }

            var user = new User
            {
                UserName = Input.Email,
                Email = Input.Email,
                HoTen = Input.HoTen,
                SDT = Input.SDT,
                DiaChi = Input.DiaChi,
                NgayDK = DateTime.Now,
                IsApproved = true,
                IsBanned = false,
                IsMainAdmin = false

            };
            string defaultImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/image/avatar.jpg");
            if (System.IO.File.Exists(defaultImagePath))
            {
                using (var image = await Image.LoadAsync<Rgba32>(defaultImagePath))
                {
                    image.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Size = new Size(50, 50),
                        Mode = ResizeMode.Crop
                    }));

                    using (var ms = new MemoryStream())
                    {
                        await image.SaveAsPngAsync(ms);
                        user.Avatar = ms.ToArray();
                    }
                }
            }
            // Kiểm tra email đã tồn tại chưa
            var existingUser = await _userManager.FindByEmailAsync(Input.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Input.Email", "Email đã tồn tại trong hệ thống.");
                return Page();
            }

            var result = await _userManager.CreateAsync(user, Input.Password);
            var khoaHoc = await _db.KhoaHoc.FirstOrDefaultAsync(k => k.MaKhoaHoc == Input.MaKhoaHoc);
            if (khoaHoc == null)
            {
                ModelState.AddModelError(string.Empty, "Khóa học không tồn tại.");
                return Page();
            }
            var currentUserId = _userManager.GetUserId(User);
            var hoaDon = new HoaDon
            {
                NgayLap = DateTime.Now,
                UserUserId = currentUserId,
                TienDongLan1 = Input.TrangThaiThanhToan == "Hoàn tất học phí" ? khoaHoc.HocPhi : khoaHoc.HocPhi * 0.5m,
                TienDongLan2 = Input.TrangThaiThanhToan == "Hoàn tất học phí" ? 0 : null,
                NgayDongLan2 = null
            };

            _db.HoaDon.Add(hoaDon);
            await _db.SaveChangesAsync(); // phải lưu để lấy HoaDonId

            // Bước 2: Tạo phiếu đăng ký
            var phieu = new PhieuDangKyKhoaHoc
            {
                UserId = user.Id,
                MaKhoaHoc = Input.MaKhoaHoc,
                NgayDangKy = DateTime.Now,
                TrangThaiDangKy = "Chờ xử lý",
                TrangThaiThanhToan = Input.TrangThaiThanhToan,
                MaHoaDon = hoaDon.MaHoaDon // liên kết phiếu với hóa đơn
            };

            _db.PhieuDangKyKhoaHoc.Add(phieu);
            await _db.SaveChangesAsync();

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Student");
                TempData["SuccessMessage"] = "Tạo người dùng thành công!";
                return RedirectToAction("Index", "StaffHome", new { area = "Staff" });

            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
                return Page();
            }
            return Page();
        }

        private IUserEmailStore<User> GetEmailStore()
        {
            return (IUserEmailStore<User>)_userStore;
        }
    }
}
