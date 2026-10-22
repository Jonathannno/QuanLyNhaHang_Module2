// Họ và tên: Trần Tuấn Sơn (Sinh viên 2)
// Mã sinh viên: 23203100043
// Nội dung thực hiện: Quản lý bàn ăn + Tìm kiếm + Lọc + Sắp xếp + Phân trang
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyNhaHang.Models;

namespace QuanLyNhaHang.Controllers
{
    public class BanAnController : Controller
    {
        private readonly ApplicationDbContext _db;
        public BanAnController(ApplicationDbContext db) => _db = db;

        // Danh sách bàn: công khai cho cả khách hàng và admin xem/tra cứu
        // Quy trình: Truy vấn -> Tìm kiếm -> Lọc -> Sắp xếp -> Phân trang -> Hiển thị
        [HttpGet]
        public IActionResult Index(string? tuKhoa, int? maLoaiBan, int? soCho, TrangThaiBan? trangThai, string? sapXep, int trang = 1)
        {
            IQueryable<BanAn> query = _db.BanAns.Include(b => b.LoaiBan);
            tuKhoa = tuKhoa?.Trim();

            // ----- Tìm kiếm (LINQ) theo Tên bàn hoặc Vị trí -----
            if (!string.IsNullOrWhiteSpace(tuKhoa))
            {
                query = query.Where(b => b.TenBan.Contains(tuKhoa) || b.ViTri.Contains(tuKhoa));
            }

            // ----- Lọc: có thể kết hợp nhiều điều kiện -----
            if (maLoaiBan.HasValue)
                query = query.Where(b => b.MaLoaiBan == maLoaiBan.Value);

            if (soCho.HasValue)
                query = query.Where(b => b.SoChoNgoi == soCho.Value);

            if (trangThai.HasValue)
                query = query.Where(b => b.TrangThai == trangThai.Value);

            // ----- Sắp xếp -----
            query = sapXep switch
            {
                "ten_za" => query.OrderByDescending(b => b.TenBan).ThenBy(b => b.MaBan),
                "choNgoi_tang" => query.OrderBy(b => b.SoChoNgoi).ThenBy(b => b.TenBan),
                "choNgoi_giam" => query.OrderByDescending(b => b.SoChoNgoi).ThenBy(b => b.TenBan),
                _ => query.OrderBy(b => b.TenBan) // mặc định A -> Z
            };

            var vm = new BanAnListVM
            {
                TuKhoa = tuKhoa,
                MaLoaiBanLoc = maLoaiBan,
                SoChoNgoiLoc = soCho,
                TrangThaiLoc = trangThai,
                SapXep = sapXep,
                DanhSachLoaiBan = _db.LoaiBans.Where(l => l.TrangThai).OrderBy(l => l.TenLoaiBan).ToList()
            };

            vm.TongSoBan = query.Count();

            trang = Math.Clamp(trang, 1, Math.Max(vm.TongSoTrang, 1));
            vm.TrangHienTai = trang;

            // ----- Phân trang bằng Skip()/Take() trên truy vấn EF Core -----
            vm.DanhSachBan = query
                .Skip((trang - 1) * vm.SoDongMoiTrang)
                .Take(vm.SoDongMoiTrang)
                .ToList();

            return View(vm);
        }

        public IActionResult Details(int id)
        {
            var ban = _db.BanAns.Include(b => b.LoaiBan).FirstOrDefault(b => b.MaBan == id);
            if (ban == null) return NotFound();
            return View(ban);
        }

        // ---------------- CRUD (Admin) ----------------
        [RequireAdmin, HttpGet]
        public IActionResult Create()
        {
            ViewBag.DanhSachLoaiBan = _db.LoaiBans.ToList();
            return View();
        }

        [RequireAdmin, HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(BanAn model)
        {
            model.TenBan = model.TenBan?.Trim() ?? string.Empty;
            model.ViTri = model.ViTri?.Trim() ?? string.Empty;

            if (_db.BanAns.Any(b => b.TenBan == model.TenBan))
                ModelState.AddModelError(nameof(model.TenBan), "Tên bàn đã tồn tại.");
            if (!_db.LoaiBans.Any(l => l.MaLoaiBan == model.MaLoaiBan))
                ModelState.AddModelError(nameof(model.MaLoaiBan), "Loại bàn không tồn tại.");
            if (!Enum.IsDefined(typeof(TrangThaiBan), model.TrangThai))
                ModelState.AddModelError(nameof(model.TrangThai), "Trạng thái bàn không hợp lệ.");

            if (!ModelState.IsValid)
            {
                ViewBag.DanhSachLoaiBan = _db.LoaiBans.ToList();
                return View(model);
            }

            _db.BanAns.Add(model);
            _db.SaveChanges();
            TempData["ThongBao"] = "Thêm bàn ăn thành công.";
            return RedirectToAction(nameof(Index));
        }

        [RequireAdmin, HttpGet]
        public IActionResult Edit(int id)
        {
            var ban = _db.BanAns.Find(id);
            if (ban == null) return NotFound();
            ViewBag.DanhSachLoaiBan = _db.LoaiBans.ToList();
            return View(ban);
        }

        [RequireAdmin, HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, BanAn model)
        {
            if (id != model.MaBan) return NotFound();

            model.TenBan = model.TenBan?.Trim() ?? string.Empty;
            model.ViTri = model.ViTri?.Trim() ?? string.Empty;

            if (_db.BanAns.Any(b => b.TenBan == model.TenBan && b.MaBan != id))
                ModelState.AddModelError(nameof(model.TenBan), "Tên bàn đã tồn tại.");
            if (!_db.LoaiBans.Any(l => l.MaLoaiBan == model.MaLoaiBan))
                ModelState.AddModelError(nameof(model.MaLoaiBan), "Loại bàn không tồn tại.");
            if (!Enum.IsDefined(typeof(TrangThaiBan), model.TrangThai))
                ModelState.AddModelError(nameof(model.TrangThai), "Trạng thái bàn không hợp lệ.");

            if (!ModelState.IsValid)
            {
                ViewBag.DanhSachLoaiBan = _db.LoaiBans.ToList();
                return View(model);
            }

            var ban = _db.BanAns.Find(id);
            if (ban == null) return NotFound();
            ban.TenBan = model.TenBan;
            ban.MaLoaiBan = model.MaLoaiBan;
            ban.SoChoNgoi = model.SoChoNgoi;
            ban.ViTri = model.ViTri;
            ban.MoTa = model.MoTa;
            ban.TrangThai = model.TrangThai;
            _db.SaveChanges();
            TempData["ThongBao"] = "Cập nhật bàn ăn thành công.";
            return RedirectToAction(nameof(Index));
        }

        [RequireAdmin, HttpGet]
        public IActionResult Delete(int id)
        {
            var ban = _db.BanAns.Include(b => b.LoaiBan).FirstOrDefault(b => b.MaBan == id);
            if (ban == null) return NotFound();
            ViewBag.ConLienKet = _db.PhieuDatBans.Any(p => p.MaBan == id);
            return View(ban);
        }

        [RequireAdmin, HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var ban = _db.BanAns.Find(id);
            if (ban == null) return NotFound();

            if (_db.PhieuDatBans.Any(p => p.MaBan == id))
            {
                TempData["Loi"] = "Không thể xóa: bàn đã có phiếu đặt liên quan.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            _db.BanAns.Remove(ban);
            _db.SaveChanges();
            TempData["ThongBao"] = "Xóa bàn ăn thành công.";
            return RedirectToAction(nameof(Index));
        }

        [RequireAdmin, HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CapNhatTrangThai(int id, TrangThaiBan trangThai)
        {
            if (!Enum.IsDefined(typeof(TrangThaiBan), trangThai))
                return BadRequest();

            var ban = _db.BanAns.Find(id);
            if (ban == null) return NotFound();
            ban.TrangThai = trangThai;
            _db.SaveChanges();
            TempData["ThongBao"] = "Cập nhật trạng thái bàn thành công.";
            return RedirectToAction(nameof(Index));
        }
    }
}
