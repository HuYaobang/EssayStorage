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
        public async Task<IActionResult> EditEssay(string Id)
        {
            int id;
            if (!int.TryParse(Id, out id))
            {
                return View();
            }
            var model = new CreateEssayViewModel
            {
                Content = "123",
                Description = "123",
                Name = "123",
                Specialization = "123"
            };
           /* var essay = new Essay
            {
                Id = id,
                Content = "big dick",
                Description = "228",
                Name = "23",
                Specialization = "33"
            };
            ViewData.Add("essay", "123");*/
            return View(model);
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