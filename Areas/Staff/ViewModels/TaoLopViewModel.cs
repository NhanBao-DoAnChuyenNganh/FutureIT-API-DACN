using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DoAnCoSo_Web.Areas.Staff.ViewModels
{
    public class TaoLopViewModel
    {
        public int MaKhoaHoc { get; set; }
        public string TenKhoaHoc { get; set; }
        public string TenLopHoc { get; set; }
        public DateTime NgayKhaiGiang { get; set; }
        public DateTime NgayKetThuc { get; set; }
        public int SelectedPhongHocId { get; set; }
        public List<SelectListItem>? DanhSachPhongHoc { get; set; }
        [Required(ErrorMessage = "Vui lòng chọn giáo viên.")]
        public string SelectedGiaoVienId { get; set; }

        public List<SelectListItem>? DanhSachGiaoVien { get; set; }
    }
}
