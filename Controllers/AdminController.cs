using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using Web_Food_App.Filters;
using Web_Food_App.Helpers;
using Web_Food_App.Models;

namespace Web_Food_App.Controllers
{
    [AdminOnly]
    public class AdminController : BaseController
    {
        // ---------- Tổng quan: doanh thu theo tháng (Q4), top món bán chạy (Q5), lịch sử giá (Q9) ----------

        [HttpGet]
        public ActionResult Dashboard()
        {
            var vm = new DashboardViewModel
            {
                TotalUsers = db.Users.Count(u => u.Role == "Customer"),
                TotalOrders = db.Orders.Count(),
                PendingOrders = db.Orders.Count(o => o.Status == "Pending"),
                TotalRevenue = db.Orders.Where(o => o.Status == "Completed").Sum(o => (decimal?)o.FinalAmount) ?? 0m,

                // Doanh thu tiền hàng theo tháng của các đơn đã hoàn thành
                Monthly = db.OrderDetails
                    .Where(d => d.Order.Status == "Completed")
                    .GroupBy(d => new { d.Order.OrderDate.Year, d.Order.OrderDate.Month })
                    .Select(g => new MonthlyRevenue
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        Revenue = g.Sum(x => x.Quantity * x.UnitPrice)
                    })
                    .OrderBy(m => m.Year).ThenBy(m => m.Month)
                    .ToList(),

                // Top 5 món bán chạy (bỏ qua đơn đã hủy)
                TopFoods = db.OrderDetails
                    .Where(d => d.Order.Status != "Cancelled")
                    .GroupBy(d => new { d.FoodId, d.Food.FoodName })
                    .Select(g => new TopFood { FoodName = g.Key.FoodName, Quantity = g.Sum(x => x.Quantity) })
                    .OrderByDescending(t => t.Quantity)
                    .Take(5)
                    .ToList(),

                // Nhật ký đổi giá do trigger trg_Foods_PriceHistory ghi
                PriceChanges = db.PriceHistories.Include(p => p.Food)
                    .OrderByDescending(p => p.ChangedAt)
                    .Take(10)
                    .ToList()
            };
            return View(vm);
        }

        // ---------- Quản lý món ăn ----------

        [HttpGet]
        public ActionResult Foods(string keyword)
        {
            var query = db.Foods.Include(f => f.Category).Include(f => f.Restaurant).AsQueryable();
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                string kw = keyword.Trim();
                query = query.Where(f => f.FoodName.Contains(kw));
            }
            ViewBag.Keyword = keyword;
            return View(query.OrderBy(f => f.FoodId).ToList());
        }

        [HttpGet]
        public ActionResult CreateFood()
        {
            var food = new Food { IsActive = true };
            FillLists(food);
            ViewBag.FormTitle = "Thêm món mới";
            return View("FoodForm", food);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateFood([Bind(Include = "FoodName,CategoryId,RestaurantId,Price,Stock,Description,ImageUrl,IsActive")] Food food)
        {
            if (ModelState.IsValid)
            {
                db.Foods.Add(food);
                db.SaveChanges();
                TempData["Success"] = "Đã thêm món \"" + food.FoodName + "\".";
                return RedirectToAction("Foods");
            }
            FillLists(food);
            ViewBag.FormTitle = "Thêm món mới";
            return View("FoodForm", food);
        }

        [HttpGet]
        public ActionResult EditFood(int id)
        {
            var food = db.Foods.Find(id);
            if (food == null) return HttpNotFound();
            FillLists(food);
            ViewBag.FormTitle = "Sửa món #" + food.FoodId;
            return View("FoodForm", food);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditFood([Bind(Include = "FoodId,FoodName,CategoryId,RestaurantId,Price,Stock,Description,ImageUrl,IsActive")] Food food)
        {
            if (ModelState.IsValid)
            {
                // Đổi Price sẽ kích hoạt trigger ghi PriceHistory
                db.Entry(food).State = EntityState.Modified;
                db.SaveChanges();
                TempData["Success"] = "Đã cập nhật món \"" + food.FoodName + "\".";
                return RedirectToAction("Foods");
            }
            FillLists(food);
            ViewBag.FormTitle = "Sửa món #" + food.FoodId;
            return View("FoodForm", food);
        }

        // Không xóa hẳn vì món đã có trong đơn hàng (FK chặn): chỉ bật/tắt IsActive
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ToggleFood(int id)
        {
            var food = db.Foods.Find(id);
            if (food == null) return HttpNotFound();

            food.IsActive = !food.IsActive;
            db.SaveChanges();
            TempData["Success"] = food.IsActive
                ? "Đã mở bán lại \"" + food.FoodName + "\"."
                : "Đã ngừng bán \"" + food.FoodName + "\".";
            return RedirectToAction("Foods");
        }

        private void FillLists(Food food)
        {
            ViewBag.Categories = new SelectList(db.Categories.OrderBy(c => c.CategoryName), "CategoryId", "CategoryName", food.CategoryId);
            ViewBag.Restaurants = new SelectList(db.Restaurants.OrderBy(r => r.RestaurantName), "RestaurantId", "RestaurantName", food.RestaurantId);
        }

        // ---------- Quản lý voucher ----------

        [HttpGet]
        public ActionResult Vouchers()
        {
            return View(db.Vouchers.OrderByDescending(v => v.EndDate).ThenBy(v => v.Code).ToList());
        }

        [HttpGet]
        public ActionResult CreateVoucher()
        {
            var today = DateTime.Today;
            var voucher = new Voucher
            {
                DiscountPercent = 10,
                MaxDiscount = 20000,
                MinOrderAmount = 50000,
                StartDate = today,
                EndDate = today.AddMonths(1),
                Quantity = 100,
                IsActive = true
            };
            ViewBag.FormTitle = "Thêm voucher";
            return View("VoucherForm", voucher);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateVoucher([Bind(Include = "Code,DiscountPercent,MaxDiscount,MinOrderAmount,StartDate,EndDate,Quantity,IsActive")] Voucher voucher)
        {
            if (ModelState.IsValid && db.Vouchers.Any(v => v.Code == voucher.Code))
            {
                ModelState.AddModelError("Code", "Mã voucher này đã tồn tại.");
            }

            if (ModelState.IsValid)
            {
                voucher.EndDate = EndOfDay(voucher.EndDate);
                db.Vouchers.Add(voucher);
                db.SaveChanges();
                TempData["Success"] = "Đã thêm voucher " + voucher.Code + ".";
                return RedirectToAction("Vouchers");
            }

            ViewBag.FormTitle = "Thêm voucher";
            return View("VoucherForm", voucher);
        }

        [HttpGet]
        public ActionResult EditVoucher(int id)
        {
            var voucher = db.Vouchers.Find(id);
            if (voucher == null) return HttpNotFound();
            ViewBag.FormTitle = "Sửa voucher " + voucher.Code;
            return View("VoucherForm", voucher);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditVoucher([Bind(Include = "VoucherId,Code,DiscountPercent,MaxDiscount,MinOrderAmount,StartDate,EndDate,Quantity,IsActive")] Voucher voucher)
        {
            if (ModelState.IsValid && db.Vouchers.Any(v => v.Code == voucher.Code && v.VoucherId != voucher.VoucherId))
            {
                ModelState.AddModelError("Code", "Mã voucher này đã tồn tại.");
            }

            if (ModelState.IsValid)
            {
                voucher.EndDate = EndOfDay(voucher.EndDate);
                db.Entry(voucher).State = EntityState.Modified;
                db.SaveChanges();
                TempData["Success"] = "Đã cập nhật voucher " + voucher.Code + ".";
                return RedirectToAction("Vouchers");
            }

            ViewBag.FormTitle = "Sửa voucher " + voucher.Code;
            return View("VoucherForm", voucher);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ToggleVoucher(int id)
        {
            var voucher = db.Vouchers.Find(id);
            if (voucher == null) return HttpNotFound();

            voucher.IsActive = !voucher.IsActive;
            db.SaveChanges();
            TempData["Success"] = voucher.IsActive
                ? "Đã bật voucher " + voucher.Code + "."
                : "Đã tắt voucher " + voucher.Code + ".";
            return RedirectToAction("Vouchers");
        }

        // Ngày kết thúc chọn từ ô "date" -> tính đến hết ngày đó
        private static DateTime EndOfDay(DateTime date)
        {
            return date.Date.AddDays(1).AddSeconds(-1);
        }

        // ---------- Quản lý người dùng ----------

        [HttpGet]
        public ActionResult Users()
        {
            ViewBag.OrderCounts = db.Orders
                .GroupBy(o => o.UserId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToDictionary(x => x.UserId, x => x.Count);

            return View(db.Users.OrderBy(u => u.Role).ThenBy(u => u.UserId).ToList());
        }

        // Khóa / mở khóa khách hàng (không khóa được tài khoản quản trị)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ToggleUser(int id)
        {
            var user = db.Users.Find(id);
            if (user == null) return HttpNotFound();

            if (user.Role == "Admin")
            {
                TempData["Error"] = "Không thể khóa tài khoản quản trị.";
                return RedirectToAction("Users");
            }

            user.IsActive = !user.IsActive;
            db.SaveChanges();
            TempData["Success"] = user.IsActive
                ? "Đã mở khóa tài khoản " + user.Username + "."
                : "Đã khóa tài khoản " + user.Username + ".";
            return RedirectToAction("Users");
        }

        // ---------- Quản lý đơn hàng ----------

        [HttpGet]
        public ActionResult Orders(string status)
        {
            var query = db.Orders.Include(o => o.User).Include(o => o.OrderDetails.Select(d => d.Food)).AsQueryable();
            if (!string.IsNullOrEmpty(status) && ViewHelper.AllStatuses.Contains(status))
            {
                query = query.Where(o => o.Status == status);
            }
            ViewBag.Status = status;
            return View(query.OrderByDescending(o => o.OrderDate).ToList());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateOrderStatus(int id, string status, string filter)
        {
            var order = db.Orders.Find(id);
            if (order == null) return HttpNotFound();

            if (!ViewHelper.NextStatuses(order.Status).Contains(status))
            {
                TempData["Error"] = "Không thể chuyển đơn #" + id + " từ \"" + ViewHelper.StatusText(order.Status) +
                                    "\" sang \"" + ViewHelper.StatusText(status) + "\".";
                return RedirectToAction("Orders", new { status = filter });
            }

            if (status == "Cancelled")
            {
                OrderService.Cancel(db, order); // trả lại kho + voucher
            }
            else
            {
                order.Status = status;
                db.SaveChanges();
            }

            TempData["Success"] = "Đơn #" + id + " đã chuyển sang \"" + ViewHelper.StatusText(status) + "\".";
            return RedirectToAction("Orders", new { status = filter });
        }
    }
}
