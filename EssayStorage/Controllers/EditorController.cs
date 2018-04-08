using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
            if (file != null)
            {
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
        public List<Tag> GetAutocomplitedList(string lineBeginning)
        {
            return db.Tags.Where(t => t.TagId.StartsWith(lineBeginning)).OrderByDescending(k => k.TagId).Take(5).ToList();
        }

        [HttpPost]
        public IActionResult EditEssay(int essayId)
        {
            Essay essay = db.Essays.Where(e => e.Id == essayId).FirstOrDefault();
            var essayTag = db.EssayToTags.Where(e => e.EssayId == essayId).ToList();
            StringBuilder str = new StringBuilder("");
            if (essayTag != null)
            {
                int n = essayTag.Count;
                for (int i = 0; i < n; i++)
                    if (i != n - 1)
                        str.Append(essayTag[i].TagId + ",");
                    else
                        str.Append(essayTag[i].TagId);
            }
            if (essay != null)
            {
                var model = new CreateEssayViewModel
                {
                    Content = essay.Content,
                    Description = essay.Description,
                    Name = essay.Name,
                    Specialization = essay.Specialization,
                    Id = essayId,
                    Tags = Convert.ToString(str)
                };
                return View(model);
            }
            return null;
        }

        [HttpPost]
        public async Task<IActionResult> DeleteEssay(int essayId)
        {
            Essay essay = db.Essays.Where(e => e.Id == essayId).FirstOrDefault();
            var comments = db.Comments.Where(c => c.EssayId == essayId).ToList();
            foreach(var comment in comments)
            {
                db.Comments.Remove(comment);
            }
            await db.SaveChangesAsync();
            var essayTags = db.EssayToTags.Where(et => et.EssayId == essayId).ToList();
            foreach(var essayTag in essayTags)
            {
                Tag tag = db.Tags.Where(t => t.TagId == essayTag.TagId).FirstOrDefault();
                if (tag.Frequency == 1)
                {
                    db.Tags.Remove(tag);
                }
                else
                {
                    tag.Frequency--;
                    db.Tags.Update(tag);
                }
                db.Tags.Update(tag);
                db.EssayToTags.Remove(essayTag);
            }
            await db.SaveChangesAsync();
            var essayComments = db.UserEssayRatings.Where(e => e.EssayId == essayId).ToList();
            foreach (var e in essayComments)
            {
                db.UserEssayRatings.Remove(e);
            }
            await db.SaveChangesAsync();
            db.Essays.Remove(essay);
            await db.SaveChangesAsync();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SaveEssay(CreateEssayViewModel model)
        {
                string[] tags = model.Tags.Split(",");


                if (db.Essays.Any(e => e.Id == model.Id))
                {
                    Essay essay = db.Essays.Where(e => e.Id == model.Id).First();
                    essay.Name = model.Name;
                    essay.Description = model.Description;
                    essay.Specialization = model.Specialization;
                    essay.Content = model.Content;
                    db.Update(essay);
                    await db.SaveChangesAsync();
                    var tagsToEssay = db.EssayToTags.Where(e => e.EssayId == model.Id).ToList();
                    foreach (var temporary in tagsToEssay)
                    {
                        if (tags.Contains(temporary.TagId))
                            continue;
                        else
                        {
                            db.EssayToTags.Remove(temporary);
                            var updatedTag = db.Tags.Where(e => e.TagId == temporary.TagId).First();
                            if (updatedTag.Frequency == 1)
                            {
                                db.Tags.Remove(updatedTag);
                            }
                            else
                            {
                                updatedTag.Frequency--;
                                db.Tags.Update(updatedTag);
                            }
                        }
                    }
                    await db.SaveChangesAsync();
                    foreach (var temporary in tags)
                    {
                        Tag tag = db.Tags.Where(t => t.TagId == temporary).FirstOrDefault();
                        if (tag != null)
                        {
                            tag.Frequency++;
                            db.Tags.Update(tag);
                        }
                        else
                        {
                            db.Tags.Add(new Tag { TagId = temporary, Frequency = 1 });
                        }
                        if (db.EssayToTags.Where(e => e.TagId == temporary && e.EssayId == essay.Id).FirstOrDefault() == null)
                            db.EssayToTags.Add(new EssayTag { TagId = temporary, EssayId = essay.Id });
                        await db.SaveChangesAsync();
                    }
                    await db.SaveChangesAsync();
                }
                else
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
                    await db.SaveChangesAsync();
                    var essay = db.Essays.Where
                        (
                            t =>
                            t.Name == model.Name &&
                            t.Specialization == model.Specialization &&
                            t.Description == model.Description
                        )
                        .FirstOrDefault();
                    foreach (var temporary in tags)
                    {
                        Tag tag = db.Tags.Where(t => t.TagId == temporary).FirstOrDefault();
                        if (tag != null)
                        {
                            tag.Frequency++;
                            db.Tags.Update(tag);
                        }
                        else
                        {
                            db.Tags.Add(new Tag { TagId = temporary, Frequency = 1 });
                        }
                        if (db.EssayToTags.Where(e => e.TagId == temporary && e.EssayId == essay.Id).FirstOrDefault() == null)
                            db.EssayToTags.Add(new EssayTag { TagId = temporary, EssayId = essay.Id });
                        await db.SaveChangesAsync();
                    }
                    await userManager.UpdateAsync(user);
                }
                return View("SaveEssay");
            
        }
    }
}