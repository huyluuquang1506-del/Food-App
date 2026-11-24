using System;
using System.Linq;
using System.Web.Mvc;
using Web_Food_App.Filters;
using Web_Food_App.Models;

namespace Web_Food_App.Controllers
{
    [RequireLogin]
    public class FavoriteController : BaseController
    {
        // Danh sách món yêu thích của tôi
        [HttpGet]
        public ActionResult Index()
        {
            int uid = CurrentUserId.Value;
            var ids = db.Favorites.Where(f => f.UserId == uid)
                                  .OrderByDescending(f => f.AddedAt)
                                  .Select(f => f.FoodId)
                                  .ToList();

            var foods = db.FoodDetails.Where(f => ids.Contains(f.FoodId) && f.IsActive).ToList();
            return View(foods);
        }

        // Bấm tim: thích / bỏ thích
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Toggle(int foodId, string returnUrl)
        {
            int uid = CurrentUserId.Value;
            var fav = db.Favorites.FirstOrDefault(f => f.UserId == uid && f.FoodId == foodId);

            if (fav != null)
            {
                db.Favorites.Remove(fav);
                TempData["Success"] = "Đã bỏ khỏi danh sách yêu thích.";
            }
            else if (db.Foods.Any(f => f.FoodId == foodId))
            {
                db.Favorites.Add(new Favorite { UserId = uid, FoodId = foodId, AddedAt = DateTime.Now });
                TempData["Success"] = "Đã thêm vào danh sách yêu thích.";
            }
            db.SaveChanges();

            return RedirectBack(returnUrl, "Index", "Favorite");
        }
    }
}
