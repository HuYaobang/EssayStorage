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

        //private ConcurrentDictionary<>

        public DisplayController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            this.db = db;
            this.userManager = userManager;
        }

        public IActionResult Essay(int essayId)
        {
            Essay essay = db.Essays.Where(e => e.Id == essayId).FirstOrDefault();
            if (essay == null)
            {
                return View("essaynotfound");
            }
            return View(essay);
        }

        public async Task<bool?> PressLike(int id)
        {
            var user = await userManager.GetUserAsync(User);
            var obj = new UserComment { UserId = user.Id, CommentId = id };
            if (db.UserToLikedComments.Any(o => o.CommentId == obj.CommentId && o.UserId == user.Id))
            {
                db.Remove(obj);
                await db.SaveChangesAsync();
                return false;
            }
            else
            {
                db.UserToLikedComments.Add(obj);
                await db.SaveChangesAsync();
                return true;
            }
        }

        public async Task SaveComment(int? parentId, int essayId, string text)
        {
            if (User.Identity.IsAuthenticated)
            {
                if (parentId != null)
                {
                    Comment comment = db.Comments.Where(c => c.Id == parentId).FirstOrDefault();
                    if (comment == null) return;
                    if (comment.EssayId != essayId) return;
                }
                else if (db.Essays.Where(e => e.Id == essayId).FirstOrDefault() == null) return;

                var user = await userManager.GetUserAsync(User);
                db.Comments.Add(new Comment
                {
                    UserId = user.Id,
                    UserName = user.UserName,
                    ParentId = parentId,
                    EssayId = essayId,
                    Text = text,
                    CreationDate = DateTime.Now
                });
                db.SaveChanges();
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
                    List<Comment> comments = db.Comments.Where(c => c.EssayId == essayId && c.Id > lastCommentId).ToList();
                    if (comments.Count != 0)
                    {
                        List<ClientComment> ret = new List<ClientComment>();
                        foreach (var c in comments)
                        {
                            var user = db.Users.First(u => u.Id == c.UserId);
                            ret.Add(new ClientComment
                            {
                                Id = c.Id,
                                ParentId = c.ParentId,
                                CreationDate = c.CreationDate,
                                LikesCount = db.UserToLikedComments.Where(o => o.CommentId == c.Id).Count(),
                                Text = c.Text,
                                UserPicturePath = user.PicturePath,
                                UserName = user.UserName
                            });
                        }
                        return ret;
                    }
                    await Task.Delay(step);
                    sum += step;
                }
            }
            return null;
        }

        public List<ClientComment> GetComments(int essayId)
        {
            //getting author's name via users table by id takes by 2 times more time
            //need to add real time responses
            List<Comment> comments = db.Comments.Where(c => c.EssayId == essayId).ToList();
            comments.Reverse();
            List<ClientComment> ret = new List<ClientComment>();
            foreach (var c in comments)
            {
                var user = db.Users.First(u => u.Id == c.UserId);
                ret.Add(new ClientComment
                {
                    Id = c.Id,
                    ParentId = c.ParentId,
                    CreationDate = c.CreationDate,
                    LikesCount = db.UserToLikedComments.Where(o => o.CommentId == c.Id).Count(),
                    Text = c.Text,
                    UserPicturePath = user.PicturePath,
                    UserName = user.UserName
                });
            }
            return ret;
        }
    }
}