using System.ComponentModel.DataAnnotations;

namespace DoAnCoSo_Web.Models
{
    public class ChuyenNganh
    {
        [Key]
        public int MaChuyenNganh { get; set; }

        [Required]
        public string TenChuyenNganh { get; set; }
    }
}
