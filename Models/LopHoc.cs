using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnCoSo_Web.Models
{
    public class LopHoc
    {
        [Key]
        public int MaLopHoc { get; set; }
        [Required]
        public string TenLopHoc { get; set; }
        [Required]
        public DateTime NgayKhaiGiang {  get; set; }
        [Required]
        public DateTime NgayKetThuc { get; set; }
        public int KhoaHocMaKhoaHoc { get; set; }
        [ForeignKey("KhoaHocMaKhoaHoc")]
        [ValidateNever]
        public KhoaHoc KhoaHoc { get; set; }
        public int PhongHocMaPhongHoc {  get; set; }
        [ForeignKey("PhongHocMaPhongHoc")]
        [ValidateNever]
        public PhongHoc PhongHoc { get; set; }
        public ICollection<ChiTietHocTap> ChiTietHocTap { get; set; }
        public ICollection<ChiTietGiangDay> ChiTietGiangDay { get; set; }
    }
}
