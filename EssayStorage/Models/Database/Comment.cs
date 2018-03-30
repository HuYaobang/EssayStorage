using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace EssayStorage.Models.Database
{
    public class Comment
    {
        public Comment()
        {
            UsersWhoLiked = new List<UserComment>();
        }

        public int Id { get; set; }
        public int? ParentId { get; set; }

        public string Text { get; set; }
        public DateTime CreationDate { get; set; }

        //public ICollection<Comment> Children { get; set; }

        //author
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string UserPicturePath { get; set; }
        //public User User { get; set; }

        public virtual ICollection<UserComment> UsersWhoLiked { get; set; }

        public int EssayId { get; set; }
        public Essay Essay { get; set; }
    }
}
