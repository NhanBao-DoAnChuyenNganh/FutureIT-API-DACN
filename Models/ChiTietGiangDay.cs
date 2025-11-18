using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnCoSo_Web.Models
{
    public class ChiTietGiangDay
    {
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public User User { get; set; }
        public int MaLopHoc { get; set; }
        [ForeignKey("MaLopHoc")]
        public LopHoc LopHoc { get; set; }
    }
}
