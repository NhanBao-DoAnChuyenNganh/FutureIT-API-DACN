using DoAnCoSo_Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace DoAnCoSo_Web.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountApiController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;

        public AccountApiController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IConfiguration configuration,
            IWebHostEnvironment env)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _env = env;
        }

        // -----------------------
        // DTO cho Login
        // -----------------------
        public class LoginDto
        {
            public string Email { get; set; }
            public string Password { get; set; }
        }

        // -----------------------
        // DTO cho Register (đầy đủ thông tin)
        // -----------------------
        public class RegisterDto
        {
            public string HoTen { get; set; }
            public string SDT { get; set; }
            public string DiaChi { get; set; }
            public string Email { get; set; }
            public string Password { get; set; }
            public string Role { get; set; }
            public IFormFile? Avatar { get; set; }
        }

        // -----------------------
        // Đăng nhập
        // -----------------------
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return Unauthorized(new { message = "Tài khoản không tồn tại" });

            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);
            if (!result.Succeeded)
                return Unauthorized(new { message = "Sai mật khẩu" });

            // 🔹 Kiểm tra IsApproved
            if (!user.IsApproved)
                return Unauthorized(new { message = "Tài khoản chưa được duyệt", isApproved = false });

            var roles = await _userManager.GetRolesAsync(user);

            var jwtSection = _configuration.GetSection("Jwt");
            var key = jwtSection["Key"];
            var issuer = jwtSection["Issuer"];
            var audience = jwtSection["Audience"];
            var expireMinutes = int.Parse(jwtSection["ExpireMinutes"] ?? "43200");

            var claims = new List<Claim>
    {
        new Claim(JwtRegisteredClaimNames.Sub, user.Id),
        new Claim(JwtRegisteredClaimNames.Email, user.Email),
        new Claim(ClaimTypes.Name, user.UserName ?? user.Email)
    };

            foreach (var role in roles)
                claims.Add(new Claim(ClaimTypes.Role, role));

            var keyBytes = Encoding.UTF8.GetBytes(key);
            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expireMinutes),
                signingCredentials: new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256)
            );

            string? avatarBase64 = null;
            if (user.Avatar != null && user.Avatar.Length > 0)
            {
                string base64 = Convert.ToBase64String(user.Avatar);
                avatarBase64 = $"data:image/png;base64,{base64}";
            }

            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                expires = DateTime.UtcNow.AddMinutes(expireMinutes),
                roles,
                email = user.Email,
                username = user.HoTen,
                sdt = user.SDT,
                diaChi = user.DiaChi,
                ngayDK = user.NgayDK.ToString("yyyy-MM-dd"),
                avatarBase64,
                isApproved = user.IsApproved // 🔹 Thêm trường này
            });
        }



        // -----------------------
        // Đăng ký (đầy đủ thông tin + role + avatar byte[])
        // -----------------------
        [HttpPost("register")]
        [RequestSizeLimit(10_000_000)] // Giới hạn 10MB ảnh
        public async Task<IActionResult> Register([FromForm] RegisterDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
                return BadRequest(new { message = "Tài khoản đã tồn tại" });

            byte[] avatarBytes;

            // Nếu có ảnh upload
            if (model.Avatar != null && model.Avatar.Length > 0)
            {
                using (var ms = new MemoryStream())
                {
                    await model.Avatar.CopyToAsync(ms);
                    ms.Position = 0;

                    // Resize ảnh nhỏ gọn trước khi lưu
                    using (var image = await Image.LoadAsync(ms))
                    {
                        image.Mutate(x => x.Resize(new ResizeOptions
                        {
                            Size = new Size(100, 100),
                            Mode = ResizeMode.Crop
                        }));

                        using (var output = new MemoryStream())
                        {
                            await image.SaveAsPngAsync(output);
                            avatarBytes = output.ToArray();
                        }
                    }
                }
            }
            else
            {
                // Nếu không có ảnh thì dùng ảnh mặc định
                string defaultPath = Path.Combine(_env.WebRootPath ?? "wwwroot", "image/avatar.jpg");
                avatarBytes = await System.IO.File.ReadAllBytesAsync(defaultPath);
            }

            //  Tạo user mới
            var user = new User
            {
                UserName = model.Email,
                Email = model.Email,
                HoTen = model.HoTen,
                SDT = model.SDT,
                DiaChi = model.DiaChi,
                Avatar = avatarBytes,
                NgayDK = DateTime.Now,
                IsApproved = false,
                IsBanned = false,
                IsMainAdmin = false
            };
            // Nếu role là Student hoặc không có role => được duyệt ngay
            if (model.Role == "Student" || string.IsNullOrEmpty(model.Role))
            {
                user.IsApproved = true;
            }
            else
            {
                user.IsApproved = false;
            }

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
                return BadRequest(new
                {
                    message = "Đăng ký thất bại",
                    errors = result.Errors.Select(e => e.Description)
                });

            // 🔹 Gán vai trò hợp lệ
            string role = string.IsNullOrEmpty(model.Role) ? "Student" : model.Role;
            var validRoles = new[] { "Admin", "Staff", "Teacher", "Student" };
            if (!validRoles.Contains(role))
                role = "Student";

            await _userManager.AddToRoleAsync(user, role);

            return Ok(new { message = "Đăng ký thành công", role, isApproved = user.IsApproved });
        }
        [Authorize(AuthenticationSchemes = "JwtBearer")]
        [HttpPost("updateprofile")]
        public async Task<IActionResult> UpdateProfile([FromForm] UpdateProfileDto model)
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return NotFound(new { message = "Không tìm thấy tài khoản" });

            user.HoTen = model.HoTen ?? user.HoTen;
            user.SDT = model.SDT ?? user.SDT;
            user.DiaChi = model.DiaChi ?? user.DiaChi;

            if (model.Avatar != null && model.Avatar.Length > 0)
            {
                using var ms = new MemoryStream();
                await model.Avatar.CopyToAsync(ms);
                user.Avatar = ms.ToArray();
            }

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
                return BadRequest(new { message = "Cập nhật thất bại", errors = result.Errors });

            return Ok(new { message = "Cập nhật thành công" });
        }


        public class UpdateProfileDto
        {
            public string? HoTen { get; set; }
            public string? SDT { get; set; }
            public string? DiaChi { get; set; }
            public IFormFile? Avatar { get; set; }
        }






    }
}
