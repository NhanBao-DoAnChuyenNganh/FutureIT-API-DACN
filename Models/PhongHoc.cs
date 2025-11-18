using System.ComponentModel.DataAnnotations;

namespace DoAnCoSo_Web.Models
{
    public class PhongHoc
    {
        [Key]
        public int MaPhongHoc { get; set; }
        [Required]
        public string TenPhongHoc { get; set; }
        public int SucChua {  get; set; }
    }
}
