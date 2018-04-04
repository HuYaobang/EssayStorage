using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EssayStorage.Data;
using EssayStorage.Models;
using EssayStorage.Models.AccountViewModels;
using EssayStorage.Models.Database;
using EssayStorage.StaticClasses;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EssayStorage.Controllers
{
    public class EditorController : Controller
    {
        private ApplicationDbContext db;
        private UserManager<ApplicationUser> userManager;
        private IHostingEnvironment appEnvironment;

        public EditorController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IHostingEnvironment appEnvironment)
        {
            this.db = db;
            this.userManager = userManager;
            this.appEnvironment = appEnvironment;
        }

        [HttpPost]
        public async Task<double?> SetRating(int rating, int essayId)
        {
            if (rating < 1 || rating > 5)
                return null;
            var user = await userManager.GetUserAsync(User);
            if (user == null)
                return null;
            var essay = db.Essays.Where(e => e.Id == essayId).FirstOrDefault();
            if (essay == null)
                return null;
            var userEssayRating = db.UserEssayRatings.Where(uer => uer.EssayId == essayId && uer.UserId == user.Id).FirstOrDefault();
            if (userEssayRating == null)
            {
                db.UserEssayRatings.Add(new UserEssayRating { EssayId = essayId, UserId = user.Id, Rating = rating });
                double total = essay.AverageRating * essay.VotersCount + rating;
                essay.VotersCount++;
                essay.AverageRating = total / essay.VotersCount;
            }
            else
            {
                db.UserEssayRatings.Update(userEssayRating);
                double total = essay.AverageRating * essay.VotersCount - userEssayRating.Rating + rating;
                essay.AverageRating = total / essay.VotersCount;
                userEssayRating.Rating = rating;
                db.UserEssayRatings.Update(userEssayRating);
            }
            await db.SaveChangesAsync();
            return Math.Round(essay.AverageRating, 1);
        }

        [HttpPost]
        public async Task<string> SavePicture(IFormFile file)
        {
            if (file != null) {
                string path = String.Format("/images/pictures/{0}.png", Rand.GetRandomEnglishLiteralString(64));
                using (var filestream = new FileStream(appEnvironment.WebRootPath + path, FileMode.Create))
                    await file.CopyToAsync(filestream);
                return path;
            }
            return null;
        }

        [HttpGet]
        public IActionResult NewEssay()
        {
            return View("EditEssay");
        }

        [HttpPost]
        public IActionResult EditEssay(int essayId)
        {
            Essay essay = db.Essays.Where(e => e.Id == essayId).FirstOrDefault();
            if (essay != null)
            {
                var model = new CreateEssayViewModel
                {
                    Content = essay.Content,
                    Description = essay.Description,
                    Name = essay.Name,
                    Specialization = essay.Specialization,
                    Id = essay.Id
                };
                return View(model);
            }
            return null;
        }
        
        [HttpPost]
        public async Task<IActionResult> UpdateEssay(CreateEssayViewModel model)
        {
            if(ModelState.IsValid)
            {
                Essay essay = db.Essays.Where(e => e.Id == model.Id).First();
                essay.Name = model.Name; 
                essay.Description = model.Description;
                essay.Specialization = model.Specialization;
                essay.Content = model.Content;
                db.Update(essay);
                await db.SaveChangesAsync();
            }
            return View("SaveEssay");
        }

        [HttpPost]
        public async Task<IActionResult> SaveEssay(CreateEssayViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await userManager.GetUserAsync(User);
                db.Essays.Add(new Essay
                {
                    Name = model.Name,
                    Description = model.Description,
                    Specialization = model.Specialization,
                    Content = model.Content,
                    CreationTime = DateTime.Now,
                    VotersCount = 0,
                    AverageRating = 0,
                    UserId = user.Id
                });
                await userManager.UpdateAsync(user);

                return View();
            }
            return View("EditEssay");
        }
    }
}