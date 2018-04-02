using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EssayStorage.Models.Database
{
    public class Tag
    {
        public Tag()
        {
            EssayTags = new List<EssayTag>();
        }

        //public ICollection<Essay> Essays { get; set; }

        public int Id { get; set; }
        public string Name { get; set; }
        public int Frequency { get; set; }

        public virtual ICollection<EssayTag> EssayTags { get; set; }
    }
}
