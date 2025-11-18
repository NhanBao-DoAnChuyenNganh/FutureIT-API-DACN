using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnCoSo_Web.Models
{
    public class TinTucTuyenDung
    {
        [Key]
        public int MaTinTuc { get; set; }
        [Required]
        public string TieuDeTinTuc { get; set; }
        [Required]
        public string NoiDungTinTuc { get;  set; }

        public DateTime NgayDang { get; set; }

        public DateTime NgayKetThuc { get; set; }
        public byte[]? HinhTinTuc { get; set; }
        
    }
}
