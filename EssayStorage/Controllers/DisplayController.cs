using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EssayStorage.Data;
using EssayStorage.Models;
using EssayStorage.Models.ClientsModels;
using EssayStorage.Models.Database;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EssayStorage.Controllers
{
    public class DisplayController : Controller
    {
        private ApplicationDbContext db;
        private UserManager<ApplicationUser> userManager;

        public DisplayController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            this.db = db;
            this.userManager = userManager;
        }

        public async Task<IActionResult> Essay(int essayId)
        {
            Essay essay = db.Essays.Where(e => e.Id == essayId).FirstOrDefault();
            if (essay == null) return View("essaynotfound");
            var user = await userManager.GetUserAsync(User);
            if (user != null) SetUserRatingInViewData(essayId, user.Id);
            return View(essay);
        }

        public async Task<bool?> PressLike(int id)
        {
            var user = await userManager.GetUserAsync(User);
            var obj = new UserComment { UserId = user.Id, CommentId = id };
            bool isCommentLikedByUser = db.UserToLikedComments.Any(o => o.CommentId == obj.CommentId && o.UserId == user.Id);
            await LikeOrUnlikeComment(isCommentLikedByUser, obj);
            return !isCommentLikedByUser;
        }

        public async Task SaveComment(int? parentId, int essayId, string text)
        {
            if (User.Identity.IsAuthenticated)
            {
                if (!IsSaveCommentDataValid(parentId, essayId, text))
                    return;
                var user = await userManager.GetUserAsync(User);
                await AddCommentInDb(user, parentId, essayId, text);
            }
        }

        public async Task<List<ClientComment>> UpdateComments(int essayId, int lastCommentId)
        {
            int sum = 0;
            int step = 3000;
            int timeout = 58000;
            if (lastCommentId != 0 && essayId != 0)
            {
                while (sum < timeout)
                {
                    var comments = GetNewCommentsForUser(essayId, lastCommentId);
                    if (comments != null)
                        return comments;
                    await Task.Delay(step);
                    sum += step;
                }
            }
            return null;
        }

        public List<ClientComment> GetComments(int essayId)
        {
            List<Comment> comments = db.Comments.Where(c => c.EssayId == essayId).ToList();
            comments.Reverse();
            List<ClientComment> ret = new List<ClientComment>();
            foreach (var c in comments)
            {
                var user = db.Users.First(u => u.Id == c.UserId);
                ret.Add(CreateClientCommentFromComment(c, user));
            }
            return ret;
        }

        private List<ClientComment> GetNewCommentsForUser(int essayId, int lastCommentId)
        {
            List<Comment> comments = db.Comments.Where(c => c.EssayId == essayId && c.Id > lastCommentId).ToList();
            if (comments.Count != 0)
            {
                List<ClientComment> ret = new List<ClientComment>();
                foreach (var c in comments)
                {
                    var user = db.Users.First(u => u.Id == c.UserId);
                    ret.Add(CreateClientCommentFromComment(c, user));
                }
                return ret;
            }
            else return null;
        }

        private ClientComment CreateClientCommentFromComment(Comment c, ApplicationUser user)
        {
            return new ClientComment
            {
                Id = c.Id,
                ParentId = c.ParentId,
                CreationDate = c.CreationDate,
                LikesCount = db.UserToLikedComments.Where(uc => uc.CommentId == c.Id).Count(),
                Text = c.Text,
                UserPicturePath = user.PicturePath,
                UserName = user.UserName
            };
        }

        private async Task AddCommentInDb(ApplicationUser user, int? parentId, int essayId, string text)
        {
            db.Comments.Add(new Comment
            {
                UserId = user.Id,
                UserName = user.UserName,
                ParentId = parentId,
                EssayId = essayId,
                Text = text,
                CreationDate = DateTime.Now
            });
            await db.SaveChangesAsync();
        }

        private bool IsSaveCommentDataValid(int? parentId, int essayId, string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            if (parentId != null) {
                Comment comment = db.Comments.Where(c => c.Id == parentId).FirstOrDefault();
                if (comment == null) return false;
                if (comment.EssayId != essayId) return false;
            } else if (db.Essays.Where(e => e.Id == essayId).FirstOrDefault() == null) return false;
            return true;
        }

        private async Task LikeOrUnlikeComment(bool isCommentLikedByUser, UserComment obj)
        {
            if (isCommentLikedByUser)
                db.Remove(obj);
            else
                db.UserToLikedComments.Add(obj);
            await db.SaveChangesAsync();
        }

        private void SetUserRatingInViewData(int essayId, string userId)
        {
            var userrating = db.UserEssayRatings.Where(uer => uer.EssayId == essayId && uer.UserId == userId).FirstOrDefault();
            if (userrating == null)
                ViewData.Add("user-rating", 0);
            else
                ViewData.Add("user-rating", userrating.Rating);
        }
    }
}