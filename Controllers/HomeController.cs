using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;
using Web_Food_App.Models;

namespace Web_Food_App.Controllers
{
    public class HomeController : BaseController
    {
        private const int PageSize = 9;

        [HttpGet]
        public ActionResult Index()
        {
            var ratingsByRestaurant = db.Reviews
                .GroupBy(review => review.Food.RestaurantId)
                .Select(group => new
                {
                    RestaurantId = group.Key,
                    AvgRating = group.Average(review => (double)review.Rating),
                    ReviewCount = group.Count()
                })
                .ToDictionary(item => item.RestaurantId);

            var vm = new HomePageViewModel();
            vm.Restaurants = db.Restaurants
                .Where(r => r.IsActive)
                .Select(r => new RestaurantHomeCard
                {
                    RestaurantId = r.RestaurantId,
                    RestaurantName = r.RestaurantName,
                    Address = r.Address,
                    FoodCount = r.Foods.Count(f => f.IsActive),
                    ImageUrl = r.Foods.Where(f => f.IsActive)
                        .OrderBy(f => f.FoodId)
                        .Select(f => f.ImageUrl)
                        .FirstOrDefault(),
                    CategoryName = r.Foods.Where(f => f.IsActive)
                        .OrderBy(f => f.FoodId)
                        .Select(f => f.Category.CategoryName)
                        .FirstOrDefault()
                })
                .ToList();

            foreach (var restaurant in vm.Restaurants)
            {
                var rating = ratingsByRestaurant.ContainsKey(restaurant.RestaurantId)
                    ? ratingsByRestaurant[restaurant.RestaurantId]
                    : null;
                if (rating != null)
                {
                    restaurant.AvgRating = rating.AvgRating;
                    restaurant.ReviewCount = rating.ReviewCount;
                }
            }

            vm.Restaurants = vm.Restaurants
                .OrderByDescending(r => r.ReviewCount > 0)
                .ThenBy(r => r.RestaurantName)
                .ToList();

            vm.Categories = db.Categories.OrderBy(c => c.CategoryName).ToList();
            ViewBag.ShowSidebar = false;
            return View(vm);
        }

        // Danh sách món: lọc theo nhóm/quán + tìm kiếm + phân trang.
        [HttpGet]
        public ActionResult Products(int? categoryId, int? restaurantId, string keyword, int page = 1)
        {
            if (page < 1) page = 1;

            var query = db.FoodDetails.Where(f => f.IsActive);
            var vm = new HomeIndexViewModel { CategoryId = categoryId, RestaurantId = restaurantId };

            if (restaurantId.HasValue)
            {
                int rid = restaurantId.Value;
                query = query.Where(f => f.RestaurantId == rid);
                vm.CategoryTitle = db.Restaurants.Where(r => r.RestaurantId == rid)
                    .Select(r => r.RestaurantName).FirstOrDefault();
            }

            if (categoryId.HasValue)
            {
                int cid = categoryId.Value;
                query = query.Where(f => f.CategoryId == cid);

                var category = db.Categories.Find(cid);
                if (category != null) vm.CategoryTitle = category.CategoryName;
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                string kw = keyword.Trim();
                query = query.Where(f => f.FoodName.Contains(kw) || f.RestaurantName.Contains(kw));
                vm.Keyword = kw;
                vm.CategoryTitle = "Kết quả cho \"" + kw + "\"";
            }

            vm.TotalItems = query.Count();
            vm.TotalPages = (int)Math.Ceiling(vm.TotalItems / (double)PageSize);
            if (vm.TotalPages > 0 && page > vm.TotalPages) page = vm.TotalPages;
            vm.Page = page;
            vm.Foods = query.OrderBy(f => f.FoodId)
                            .Skip((page - 1) * PageSize)
                            .Take(PageSize)
                            .ToList();

            vm.Recommended = new List<RecommendedFood>();
            int? userId = CurrentUserId;
            if (userId.HasValue && page == 1 && !categoryId.HasValue && !restaurantId.HasValue && string.IsNullOrWhiteSpace(keyword))
            {
                vm.Recommended = db.Database.SqlQuery<RecommendedFood>(
                        "EXEC sp_RecommendFoods @UserId, @Top",
                        new SqlParameter("@UserId", userId.Value),
                        new SqlParameter("@Top", 4))
                    .ToList();
            }

            ViewBag.ShowSidebar = true;
            return View("Products", vm);
        }

        [ChildActionOnly]
        public ActionResult CategoryPartial()
        {
            var categories = db.Categories.OrderBy(c => c.CategoryName).ToList();
            return PartialView("_CategoryPartial", categories);
        }
    }
}
