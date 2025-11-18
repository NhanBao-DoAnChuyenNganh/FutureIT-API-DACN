using DoAnCoSo_Web.Data;
using DoAnCoSo_Web.Models.AppSettings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DoAnCoSo_Web_TestAPI.Areas.Student.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImageController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        private readonly AppSettings _appSettings;

        public ImageController(ApplicationDbContext db, IOptions<AppSettings> options)
        {
            _db = db;
            _appSettings = options.Value;
        }


        [HttpGet("Get")]
        public IActionResult Get(int id)
        {
            var img = _db.HinhAnhKhoaHoc.FirstOrDefault(x => x.MaHinh == id);

            if (img == null || img.NoiDungHinh == null)
                return NotFound();

            return File(img.NoiDungHinh, "image/jpeg");
        }
    }

}
