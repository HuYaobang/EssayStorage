using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EssayStorage.Data;
using EssayStorage.Models;
using EssayStorage.Models.AccountViewModels;
using EssayStorage.Models.Database;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EssayStorage.Controllers
{
    public class EditorController : Controller
    {
        private ApplicationDbContext db;
        private UserManager<ApplicationUser> userManager;

        public EditorController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            this.db = db;
            this.userManager = userManager;
        }

        [HttpGet]
        public IActionResult NewEssay()
        {
            return View();
        }

        [HttpPost]
        public IActionResult EditEssay(string Id)
        {
            int id;
            if (!int.TryParse(Id, out id))
            {
                return View();
            }

            Essay essay = db.Essays.Where(e => e.Id == id).First();
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
        
        [HttpPost]
        public IActionResult UpdateEssay(CreateEssayViewModel model)
        {
            if(ModelState.IsValid)
            {
                Essay essay = db.Essays.Where(e => e.Id == model.Id).First();
                essay.Name = model.Name; 
                essay.Description = model.Description;
                essay.Specialization = model.Specialization;
                essay.Content = model.Content;
                db.Update(essay);
                db.SaveChanges();
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
                    TotalRating = 0,
                    VotersCount = 0,
                    AverageRating = 0,
                    UserId = user.Id
                });
                await userManager.UpdateAsync(user);

                return View();
            }
            return View("NewEssay");
        }
    }
}