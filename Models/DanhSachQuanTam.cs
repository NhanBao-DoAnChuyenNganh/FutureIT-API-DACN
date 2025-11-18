using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnCoSo_Web.Models
{
    public class DanhSachQuanTam
    {
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public User User { get; set; }
        public int MaKhoaHoc { get; set; }
        [ForeignKey("MaKhoaHoc")]
        public KhoaHoc KhoaHoc { get; set; }
    }
}
