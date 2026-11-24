using System.Collections.Generic;
using System.Linq;
using System.Web;
using Web_Food_App.Models;

namespace Web_Food_App.Helpers
{
    // Giỏ hàng lưu trong Session dưới dạng { FoodId -> Quantity }.
    // Script SQL không có bảng Cart, giá/tồn kho luôn được đọc lại từ bảng Foods nên không bị lệch.
    public static class CartHelper
    {
        private const string Key = "Cart";

        // Khớp với DEFAULT 15000 của cột Orders.ShippingFee
        public const decimal DefaultShippingFee = 15000m;

        public static Dictionary<int, int> Get(HttpSessionStateBase session)
        {
            var cart = session[Key] as Dictionary<int, int>;
            if (cart == null)
            {
                cart = new Dictionary<int, int>();
                session[Key] = cart;
            }
            return cart;
        }

        public static int Count(HttpSessionStateBase session)
        {
            return Get(session).Values.Sum();
        }

        public static void Clear(HttpSessionStateBase session)
        {
            session[Key] = null;
        }

        // Dựng giỏ hàng từ DB; tự bỏ các món đã bị xóa/ngừng bán
        public static CartViewModel Build(FoodAppContext db, HttpSessionStateBase session)
        {
            var cart = Get(session);
            var ids = cart.Keys.ToList();
            var foods = db.Foods.Where(f => ids.Contains(f.FoodId) && f.IsActive).ToList();

            foreach (int id in ids)
            {
                if (!foods.Any(f => f.FoodId == id)) cart.Remove(id);
            }

            var lines = foods.Select(f => new CartLineViewModel { Food = f, Quantity = cart[f.FoodId] })
                             .OrderBy(l => l.Food.FoodName)
                             .ToList();

            return new CartViewModel
            {
                Lines = lines,
                Subtotal = lines.Sum(l => l.LineTotal),
                ShippingFee = lines.Count > 0 ? DefaultShippingFee : 0m
            };
        }
    }
}
