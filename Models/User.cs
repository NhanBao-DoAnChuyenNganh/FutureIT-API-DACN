using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.EntityFrameworkCore;

namespace DoAnCoSo_Web.Models
{
    public class User :IdentityUser
    {
        [Required]
        public string HoTen { get; set; }
        public string SDT { get; set; }
        public string DiaChi { get; set; }
        public DateTime NgayDK { get; set; } = DateTime.Now;
        public bool IsApproved { get; set; } = false; // Mặc định là chưa duyệt
        public bool IsBanned {  get; set; } = false;//Mặc định là chưa khóa tài khoản
        public bool IsMainAdmin { get; set; } = false;//Mặc định không phải là admin chính
        public byte[]? Avatar { get; set; }

        public int? ChuyenNganhMaChuyenNganh {  get; set; }
        [ForeignKey("ChuyenNganhMaChuyenNganh")]
        [ValidateNever]
        public ChuyenNganh ChuyenNganh { get; set;}
        public ICollection<PhieuDangKyKhoaHoc> PhieuDangKyKhoaHoc { get; set; }
        public ICollection<ChiTietHocTap> ChiTietHocTap { get; set; }
        public ICollection<ChiTietGiangDay> ChiTietGiangDay{ get; set; }
        public ICollection<ToCaoDanhGia> ToCaoDanhGia { get; set; }

        public ICollection<DanhSachQuanTam> DanhSachQuanTam { get; set; }


    }
}
