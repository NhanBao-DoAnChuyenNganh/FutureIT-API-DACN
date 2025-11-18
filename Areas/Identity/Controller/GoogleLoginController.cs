using System.Security.Claims;
using DoAnCoSo_Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DoAnCoSo_Web.Areas.Identity
{
    [Area("Identity")]
    public class GoogleLoginController : Controller
    {
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;
        public GoogleLoginController(SignInManager<User> signInManager, UserManager<User> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }
        public IActionResult Index()
        {
            return View();
        }
        [HttpGet]
        public async Task LoginWithGoogle()
        {
            await HttpContext.ChallengeAsync(GoogleDefaults.AuthenticationScheme,
                new AuthenticationProperties
                {
                    RedirectUri = Url.Action("GoogleResponse")
                });
        }

        public async Task<IActionResult> GoogleResponse()
        {
            var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

            if (!result.Succeeded)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            // Lấy thông tin từ claims
            var email = result.Principal.FindFirst(ClaimTypes.Email)?.Value;
            var name = result.Principal.FindFirst(ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            //tìm user trong DB
            var user = await _userManager.FindByEmailAsync(email);
            var hashedpassword = new PasswordHasher<User>();

            var AvatarPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/image/avatar.jpg");
            byte[] avatarBytes = await System.IO.File.ReadAllBytesAsync(AvatarPath);
            // Chưa tồn tại = tạo tài khoản
            if (user == null)
            {
                user = new User
                {
                    UserName = email,
                    Email = email,
                    HoTen = name,
                    SDT = "0000000000",
                    DiaChi = "Lô E1, Võ Nguyên Giáp, Tp. Thủ Đức",
                    PasswordHash = hashedpassword.HashPassword(null, "LeThanhNhan123456@"),
                    NgayDK = DateTime.Now,
                    Avatar = avatarBytes,
                    IsBanned = false,
                    IsMainAdmin = false,
                    PhoneNumberConfirmed = false,
                    TwoFactorEnabled = false,
                    LockoutEnabled = true,
                    AccessFailedCount = 0,
                    EmailConfirmed = true,
                    IsApproved = true
                };
                var Result = await _userManager.CreateAsync(user);
                if (!Result.Succeeded)
                {
                    return RedirectToAction("Login", "Account", new { area = "Identity" });
                }
                //role mặc định = Student
                await _userManager.AddToRoleAsync(user, "Student"); 
            }
            //đã tồn tại = đăng nhập giúp
            await _signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToAction("Index", "StudentHome", new { area = "Student" });
        }

    }
}
