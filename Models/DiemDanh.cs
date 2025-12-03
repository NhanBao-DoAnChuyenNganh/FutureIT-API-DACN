using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnCoSo_Web.Models
{
    public class DiemDanh
    {
        [Key]
        public int MaDiemDanh { get; set; }

        [Required]
        public int MaLopHoc { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public DateTime NgayDiemDanh { get; set; }

        [Required]
        public bool CoMat { get; set; } // true = có mặt, false = vắng

        public string? GhiChu { get; set; } // Lý do vắng, hoặc ghi chú khác

        // Navigation properties
        [ForeignKey("MaLopHoc")]
        public virtual LopHoc LopHoc { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }
}
