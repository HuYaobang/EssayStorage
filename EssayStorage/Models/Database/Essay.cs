using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EssayStorage.Models.Database
{
    public class Essay
    {
        public Essay()
        {
            Comments = new List<Comment>();
            EssayTags = new List<EssayTag>();
            UserEssayRatings = new List<UserEssayRating>();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Specialization { get; set; }
        public string Content { get; set; }
        public DateTime CreationTime { get; set; }

        public int VotersCount { get; set; }
        public double AverageRating { get; set; }

        //author
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public virtual ICollection<Comment> Comments { get; set; }
        public virtual ICollection<EssayTag> EssayTags { get; set; }
        public virtual ICollection<UserEssayRating> UserEssayRatings { get; set; }

        public class DateComparer : IComparer<Essay>
        {
            public int Compare(Essay x, Essay y)
            {
                return y.CreationTime.CompareTo(x.CreationTime);
            }
        }
    }
}
