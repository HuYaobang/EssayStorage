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

        public string TagId { get; set; }
        public int Frequency { get; set; }

        public virtual ICollection<EssayTag> EssayTags { get; set; }
    }
}
