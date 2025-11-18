using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnCoSo_Web.Models
{
    public class KhoaHoc
    {
        [Key]
        public int MaKhoaHoc { get; set; }
        [Required]
        public string TenKhoaHoc { get; set; }
        [Required]
        public decimal HocPhi {  get; set; }
        
        public string? MoTa {  get; set; }

       public string? NgayHoc { get; set; }
        public string  GioBatDau { get; set; }
        public string GioKetThuc { get; set; }
        public ICollection<PhieuDangKyKhoaHoc> PhieuDangKyKhoaHoc { get; set; }
        public ICollection<DanhSachQuanTam> DanhSachQuanTam { get; set; }
        public ICollection<HinhAnhKhoaHoc> HinhAnhKhoaHoc { get; set; }
       
    }


}

