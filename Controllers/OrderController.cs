using System;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;
using Web_Food_App.Filters;
using Web_Food_App.Helpers;
using Web_Food_App.Models;

namespace Web_Food_App.Controllers
{
    [RequireLogin]
    public class OrderController : BaseController
    {
        // ---------- Đặt hàng ----------

        [HttpGet]
        public ActionResult Checkout()
        {
            var cart = CartHelper.Build(db, Session);
            if (cart.Lines.Count == 0)
            {
                TempData["Warning"] = "Giỏ hàng đang trống.";
                return RedirectToAction("Index", "Cart");
            }

            int uid = CurrentUserId.Value;
            var user = db.Users.Find(uid);
            return View(BuildCheckout(cart, user != null ? user.Address : "", "COD", null));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Checkout(string shippingAddress, string paymentMethod, string voucherCode)
        {
            var cart = CartHelper.Build(db, Session);
            if (cart.Lines.Count == 0)
            {
                TempData["Warning"] = "Giỏ hàng đang trống.";
                return RedirectToAction("Index", "Cart");
            }

            if (!ViewHelper.PaymentMethods.Contains(paymentMethod)) paymentMethod = "COD";

            if (string.IsNullOrWhiteSpace(shippingAddress))
            {
                ViewBag.Error = "Vui lòng nhập địa chỉ giao hàng.";
                return View(BuildCheckout(cart, shippingAddress, paymentMethod, voucherCode));
            }

            var overStock = cart.Lines.FirstOrDefault(l => l.OverStock);
            if (overStock != null)
            {
                ViewBag.Error = "Món \"" + overStock.Food.FoodName + "\" chỉ còn " + overStock.Food.Stock +
                                " phần. Vui lòng cập nhật lại giỏ hàng.";
                return View(BuildCheckout(cart, shippingAddress, paymentMethod, voucherCode));
            }

            try
            {
                int orderId = PlaceOrder(CurrentUserId.Value, shippingAddress.Trim(), paymentMethod, voucherCode,
                                         CartHelper.Get(Session).ToDictionary(k => k.Key, k => k.Value));
                CartHelper.Clear(Session);
                TempData["Success"] = "Đặt hàng thành công! Mã đơn của bạn là #" + orderId + ".";
                return RedirectToAction("History");
            }
            catch (SqlException ex)
            {
                ViewBag.Error = TranslateSqlError(ex);
                return View(BuildCheckout(cart, shippingAddress, paymentMethod, voucherCode));
            }
        }

        // Gọi stored procedure sp_PlaceOrder: tạo đơn + chi tiết + áp voucher trong 1 transaction (xử lý trong SQL)
        private int PlaceOrder(int userId, string address, string payment, string voucherCode,
                               System.Collections.Generic.Dictionary<int, int> cart)
        {
            var items = new DataTable();
            items.Columns.Add("FoodId", typeof(int));
            items.Columns.Add("Quantity", typeof(int));
            foreach (var kv in cart) items.Rows.Add(kv.Key, kv.Value);

            string cs = ConfigurationManager.ConnectionStrings["FoodAppContext"].ConnectionString;
            using (var conn = new SqlConnection(cs))
            using (var cmd = new SqlCommand("sp_PlaceOrder", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                cmd.Parameters.Add("@ShippingAddress", SqlDbType.NVarChar, 255).Value = address;
                cmd.Parameters.Add("@PaymentMethod", SqlDbType.NVarChar, 20).Value = payment;
                cmd.Parameters.Add("@VoucherCode", SqlDbType.VarChar, 30).Value =
                    string.IsNullOrWhiteSpace(voucherCode) ? (object)DBNull.Value : voucherCode.Trim();

                var itemsParam = cmd.Parameters.Add("@Items", SqlDbType.Structured);
                itemsParam.TypeName = "dbo.OrderItemType";
                itemsParam.Value = items;

                var outId = cmd.Parameters.Add("@OrderId", SqlDbType.Int);
                outId.Direction = ParameterDirection.Output;

                conn.Open();
                cmd.ExecuteNonQuery();
                return (int)outId.Value;
            }
        }

        // Dịch mã lỗi THROW trong sp_PlaceOrder (50001..50003) sang tiếng Việt có dấu
        private static string TranslateSqlError(SqlException ex)
        {
            switch (ex.Number)
            {
                case 50001: return "Giỏ hàng đang trống.";
                case 50002: return "Có món không còn tồn tại hoặc đã ngừng bán. Vui lòng kiểm tra lại giỏ hàng.";
                case 50003: return "Mã voucher không hợp lệ, đã hết hạn/hết lượt hoặc đơn chưa đủ giá trị tối thiểu.";
                case 547: return "Một số món không đủ số lượng tồn kho. Vui lòng cập nhật lại giỏ hàng.";
                default: return "Không thể đặt hàng lúc này. Vui lòng thử lại sau.";
            }
        }

        private CheckoutViewModel BuildCheckout(CartViewModel cart, string address, string payment, string voucherCode)
        {
            var now = DateTime.Now;
            return new CheckoutViewModel
            {
                Cart = cart,
                ShippingAddress = address,
                PaymentMethod = payment,
                VoucherCode = voucherCode,
                AvailableVouchers = db.Vouchers
                    .Where(v => v.IsActive && v.Quantity > 0 && v.StartDate <= now && v.EndDate >= now)
                    .OrderBy(v => v.MinOrderAmount)
                    .ToList()
            };
        }

        // ---------- Lịch sử đơn hàng (Q3) ----------

        [HttpGet]
        public ActionResult History()
        {
            int uid = CurrentUserId.Value;
            var orders = db.Orders.Include(o => o.OrderDetails.Select(d => d.Food))
                                  .Include(o => o.Voucher)
                                  .Where(o => o.UserId == uid)
                                  .OrderByDescending(o => o.OrderDate)
                                  .ToList();
            return View(orders);
        }

        // Khách chỉ được hủy đơn của chính mình khi còn "Chờ xác nhận"
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Cancel(int id)
        {
            int uid = CurrentUserId.Value;
            var order = db.Orders.FirstOrDefault(o => o.OrderId == id && o.UserId == uid);
            if (order == null)
            {
                return HttpNotFound();
            }
            if (order.Status != "Pending")
            {
                TempData["Error"] = "Chỉ có thể hủy đơn đang ở trạng thái \"Chờ xác nhận\".";
                return RedirectToAction("History");
            }

            OrderService.Cancel(db, order);
            TempData["Success"] = "Đã hủy đơn #" + order.OrderId + ".";
            return RedirectToAction("History");
        }
    }
}
