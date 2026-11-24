using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using Web_Food_App.Filters;
using Web_Food_App.Models;

namespace Web_Food_App.Controllers
{
    public class FoodController : BaseController
    {
        // GET: Food/Details/5
        [HttpGet]
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            int foodId = id.Value;
            var food = db.Foods.Include(f => f.Category)
                               .Include(f => f.Restaurant)
                               .FirstOrDefault(f => f.FoodId == foodId && f.IsActive);
            if (food == null)
            {
                return HttpNotFound();
            }

            var reviews = db.Reviews.Include(r => r.User)
                                    .Where(r => r.FoodId == foodId)
                                    .OrderByDescending(r => r.CreatedAt)
                                    .ToList();

            var vm = new FoodDetailsViewModel
            {
                Food = food,
                Reviews = reviews,
                AvgRating = reviews.Count > 0 ? reviews.Average(r => (double)r.Rating) : 0,
                Related = db.FoodDetails
                            .Where(f => f.IsActive && f.CategoryId == food.CategoryId && f.FoodId != foodId)
                            .OrderBy(f => f.FoodId)
                            .Take(3)
                            .ToList()
            };

            int? userId = CurrentUserId;
            if (userId.HasValue)
            {
                int uid = userId.Value;
                vm.MyReview = reviews.FirstOrDefault(r => r.UserId == uid);
                vm.IsFavorite = db.Favorites.Any(f => f.UserId == uid && f.FoodId == foodId);
            }

            ViewBag.ShowSidebar = true;
            ViewBag.Title = food.FoodName;
            return View(vm);
        }

        // Thêm hoặc sửa đánh giá (mỗi người chỉ đánh giá 1 lần cho mỗi món: UQ_Reviews_User_Food)
        [HttpPost]
        [RequireLogin]
        [ValidateAntiForgeryToken]
        public ActionResult AddReview(int foodId, int rating, string comment)
        {
            if (rating < 1 || rating > 5)
            {
                TempData["Error"] = "Điểm đánh giá phải từ 1 đến 5 sao.";
                return RedirectToAction("Details", new { id = foodId });
            }
            if (!db.Foods.Any(f => f.FoodId == foodId))
            {
                return HttpNotFound();
            }

            comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
            if (comment != null && comment.Length > 500) comment = comment.Substring(0, 500);

            int uid = CurrentUserId.Value;
            var review = db.Reviews.FirstOrDefault(r => r.FoodId == foodId && r.UserId == uid);
            if (review == null)
            {
                db.Reviews.Add(new Review
                {
                    FoodId = foodId,
                    UserId = uid,
                    Rating = rating,
                    Comment = comment,
                    CreatedAt = DateTime.Now
                });
            }
            else
            {
                review.Rating = rating;
                review.Comment = comment;
                review.CreatedAt = DateTime.Now;
            }
            db.SaveChanges();

            TempData["Success"] = "Cảm ơn bạn đã đánh giá món ăn!";
            return RedirectToAction("Details", new { id = foodId });
        }
    }
}
