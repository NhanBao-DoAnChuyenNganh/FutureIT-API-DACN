using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnCoSo_Web.Models
{
    public class ToCaoDanhGia
    {
        public int Id { get; set; }
        public int MaDanhGia { get; set; }
        [ForeignKey("MaDanhGia")]
        public DanhGia DanhGia { get; set; }
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public User User { get; set; }
        public string LyDo {  get; set; }
    }
}
