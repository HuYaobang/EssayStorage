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
            var user = await userManager.GetUserAsync(User);
            var essay = db.Essays.Where(e => e.Id == essayId).FirstOrDefault();
            if (RatingVerification(rating, user, essay))
            {               
                return Math.Round((await SetNewUserRating(rating, essayId, user, essay)).AverageRating, 1);
            }
            else
                return null;           
        }

        public async Task<Essay> SetNewUserRating(int rating, int essayId, ApplicationUser user, Essay essay)
        {
            var userEssayRating = db.UserEssayRatings.Where(uer => uer.EssayId == essayId && uer.UserId == user.Id).FirstOrDefault();
            if (userEssayRating == null)
                AddNewRating(rating, essayId, user, essay); 
            else
                UpdateOldRating(rating, essayId, user, essay, userEssayRating);
            await db.SaveChangesAsync();
            return essay;
        }

        public void AddNewRating(int rating, int essayId, ApplicationUser user, Essay essay)
        {
            db.UserEssayRatings.Add(new UserEssayRating { EssayId = essayId, UserId = user.Id, Rating = rating });
            double total = essay.AverageRating * essay.VotersCount + rating;
            essay.VotersCount++;
            essay.AverageRating = total / essay.VotersCount;
        }

        public void UpdateOldRating(int rating, int essayId, ApplicationUser user, Essay essay, UserEssayRating userEssayRating)
        {
            db.UserEssayRatings.Update(userEssayRating);
            double total = essay.AverageRating * essay.VotersCount - userEssayRating.Rating + rating;
            essay.AverageRating = total / essay.VotersCount;
            userEssayRating.Rating = rating;
            db.UserEssayRatings.Update(userEssayRating);
        }

        public bool RatingVerification(int rating, ApplicationUser user, Essay essay)
        {
            if ((rating < 1 || rating > 5) || (user == null) || (essay == null))
                return false;
            else
                return true;
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
            return View(ReturnEssayViewModel(essay, essayTag));
        }

        public CreateEssayViewModel ReturnEssayViewModel(Essay essay, List<EssayTag> essayTag)
        {
            var model = new CreateEssayViewModel{ Content = essay.Content, Description = essay.Description, Name = essay.Name,
                    Specialization = essay.Specialization, Id = essay.Id, Tags = ConvertTagsToString(essayTag) };
            return model;                   
        }

        public string ConvertTagsToString(List<EssayTag> essayTag)
        {
            StringBuilder str = new StringBuilder("");
            for (int i = 0; i < essayTag.Count; i++)
                if (i != essayTag.Count - 1)
                    str.Append(essayTag[i].TagId + ",");
                else
                    str.Append(essayTag[i].TagId);            
            return Convert.ToString(str);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteEssay(int essayId)
        {            
            await DeleteCommentRows(essayId);
            await DeleteTagRows(essayId);
            await DeleteUserEssayCommentRows(essayId);
            await DeleteEssayRow(essayId);
            return View();
        }

        public async Task DeleteEssayRow(int essayId)
        {
            Essay essay = db.Essays.Where(e => e.Id == essayId).FirstOrDefault();
            db.Essays.Remove(essay);
            await db.SaveChangesAsync();
        }

        public async Task DeleteUserEssayCommentRows(int essayId)
        {
            var userEssayComments = db.UserEssayRatings.Where(e => e.EssayId == essayId).ToList();
            foreach (var e in userEssayComments)
            {
                db.UserEssayRatings.Remove(e);
            }
            await db.SaveChangesAsync();
        }

        public async Task DeleteCommentRows(int essayId)
        {
            var comments = db.Comments.Where(c => c.EssayId == essayId).ToList();
            foreach (var comment in comments)
            {
                db.Comments.Remove(comment);
            }
            await db.SaveChangesAsync();
        }

        public async Task DeleteTagRows(int essayId)
        {
            var essayTags = db.EssayToTags.Where(et => et.EssayId == essayId).ToList();
            foreach (var essayTag in essayTags)
            {
                DeleteCertainTag(essayTag);
            }
            await db.SaveChangesAsync();
        }

        public void DeleteCertainTag(EssayTag essayTag)
        {
            Tag tag = db.Tags.Where(t => t.TagId == essayTag.TagId).FirstOrDefault();
            if (tag.Frequency == 1)
                db.Tags.Remove(tag);
            else
            {
                tag.Frequency--;
                db.Tags.Update(tag);
            }
            db.EssayToTags.Remove(essayTag);
        }

        public Essay ReturnUpdatedEssay(CreateEssayViewModel model)
        {
            Essay essay = db.Essays.Where(e => e.Id == model.Id).First();
            essay.Name = model.Name;
            essay.Description = model.Description;
            essay.Specialization = model.Specialization;
            essay.Content = model.Content;
            return essay;
        }

        public async Task DeleteMissingTags(CreateEssayViewModel model, string[] tags)
        {
            var tagsToEssay = db.EssayToTags.Where(e => e.EssayId == model.Id).ToList();
            foreach (var temporary in tagsToEssay)
            {
                if (tags.Contains(temporary.TagId))
                    continue;
                else
                    TakeMissingTagFromDb(temporary);
            }
            await db.SaveChangesAsync();
        }

        public void TakeMissingTagFromDb(EssayTag temporary)
        {
            db.EssayToTags.Remove(temporary);
            var updatedTag = db.Tags.Where(e => e.TagId == temporary.TagId).First();
            DeleteMissingTagRow(updatedTag);
        }

        public void DeleteMissingTagRow(Tag updatedTag)
        {
            if (updatedTag.Frequency == 1)
                db.Tags.Remove(updatedTag);
            else
            {
                updatedTag.Frequency--;
                db.Tags.Update(updatedTag);
            }
        }

        public async Task AddNewTags(string[] tags, Essay essay)
        {
            foreach (var temporary in tags)
            {
                TryTakeNewTag(temporary);
                if (db.EssayToTags.Where(e => e.TagId == temporary && e.EssayId == essay.Id).FirstOrDefault() == null)
                    db.EssayToTags.Add(new EssayTag { TagId = temporary, EssayId = essay.Id });
                await db.SaveChangesAsync();
            }
        }

        public void TryTakeNewTag(string temporary)
        {
            Tag tag = db.Tags.Where(t => t.TagId == temporary).FirstOrDefault();
            if (tag != null)
            {
                tag.Frequency++;
                db.Tags.Update(tag);
            }
            else            
                db.Tags.Add(new Tag { TagId = temporary, Frequency = 1 });            
        }

        public async Task UpdateTagsInDb(CreateEssayViewModel model, string[] tags, Essay essay)
        {
            await DeleteMissingTags(model, tags);
            await AddNewTags(tags, essay);
        }

        public async Task UpdateEssayInDb(CreateEssayViewModel model, string[] tags)
        {
            Essay essay = ReturnUpdatedEssay(model);
            db.Update(essay);
            await db.SaveChangesAsync();
            await UpdateTagsInDb(model, tags, essay);
        }

        public async Task<Essay> AddNewEssayToDb(CreateEssayViewModel model, ApplicationUser user)
        {
            db.Essays.Add(new Essay{ Name = model.Name, Description = model.Description, Specialization = model.Specialization,
                Content = model.Content, CreationTime = DateTime.Now, VotersCount = 0, AverageRating = 0, UserId = user.Id });
            await db.SaveChangesAsync();
            return db.Essays.Where(t => t.Name == model.Name && t.Specialization == model.Specialization &&
                t.Description == model.Description).FirstOrDefault();
        }

        public async Task CreateEssayInDb(CreateEssayViewModel model, string[] tags)
        {
            var user = await userManager.GetUserAsync(User);
            Essay essay = AddNewEssayToDb(model, user).Result;
            await AddNewTags(tags, essay);
            await userManager.UpdateAsync(user);
        }

        [HttpPost]
        public async Task<IActionResult> SaveEssay(CreateEssayViewModel model)
        {
            string[] tags = model.Tags.Split(",");
            if (db.Essays.Any(e => e.Id == model.Id))            
                await UpdateEssayInDb(model, tags);            
            else            
                await CreateEssayInDb(model, tags);            
            return View("SaveEssay");            
        }
    }
}