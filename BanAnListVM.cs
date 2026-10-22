namespace QuanLyNhaHang.Models
{
    // ViewModel cho danh sách bàn ăn: giữ điều kiện Tìm kiếm + Lọc + Sắp xếp + Phân trang
    public class BanAnListVM
    {
        public List<BanAn> DanhSachBan { get; set; } = new();
        public List<LoaiBan> DanhSachLoaiBan { get; set; } = new();

        // Tìm kiếm
        public string? TuKhoa { get; set; }

        // Lọc
        public int? MaLoaiBanLoc { get; set; }
        public int? SoChoNgoiLoc { get; set; }
        public TrangThaiBan? TrangThaiLoc { get; set; }

        // Sắp xếp
        public string? SapXep { get; set; } // ten_az, ten_za, choNgoi_tang, choNgoi_giam

        // Phân trang
        public int TrangHienTai { get; set; } = 1;
        public int SoDongMoiTrang { get; set; } = 6;
        public int TongSoBan { get; set; }
        public int TongSoTrang => (int)Math.Ceiling(TongSoBan / (double)SoDongMoiTrang);
    }
}
