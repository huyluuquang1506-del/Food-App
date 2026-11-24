using System.Linq;
using System.Web.Mvc;
using Web_Food_App.Helpers;
using Web_Food_App.Models;

namespace Web_Food_App.Controllers
{
    public class CartController : BaseController
    {
        // Trang giỏ hàng
        [HttpGet]
        public ActionResult Index()
        {
            return View(CartHelper.Build(db, Session));
        }

        // Thêm món vào giỏ (nút "Thêm vào giỏ" ở trang chủ / trang chi tiết)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Add(int id, int? quantity)
        {
            int qty = (quantity.HasValue && quantity.Value > 0) ? quantity.Value : 1;

            var food = db.Foods.Find(id);
            if (food == null || !food.IsActive)
            {
                return HttpNotFound();
            }
            if (food.Stock <= 0)
            {
                TempData["Error"] = "Món \"" + food.FoodName + "\" đã hết hàng.";
                return RedirectToAction("Index", "Home");
            }

            var cart = CartHelper.Get(Session);
            int current;
            cart.TryGetValue(id, out current);
            int newQty = current + qty;

            if (newQty > food.Stock)
            {
                newQty = food.Stock;
                TempData["Warning"] = "Món \"" + food.FoodName + "\" chỉ còn " + food.Stock + " phần.";
            }
            else
            {
                TempData["Success"] = "Đã thêm \"" + food.FoodName + "\" vào giỏ hàng.";
            }

            cart[id] = newQty;
            return RedirectToAction("Index");
        }

        // Đổi số lượng 1 dòng trong giỏ
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Update(int id, int quantity)
        {
            var cart = CartHelper.Get(Session);
            if (!cart.ContainsKey(id)) return RedirectToAction("Index");

            if (quantity <= 0)
            {
                cart.Remove(id);
                return RedirectToAction("Index");
            }

            var food = db.Foods.Find(id);
            if (food != null && quantity > food.Stock)
            {
                quantity = food.Stock;
                TempData["Warning"] = "Món \"" + food.FoodName + "\" chỉ còn " + food.Stock + " phần.";
            }
            cart[id] = quantity;
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Remove(int id)
        {
            CartHelper.Get(Session).Remove(id);
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Clear()
        {
            CartHelper.Clear(Session);
            return RedirectToAction("Index");
        }
    }
}
