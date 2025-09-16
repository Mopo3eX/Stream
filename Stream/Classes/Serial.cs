using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stream.Classes
{
    public class Serial
    {
        public virtual int Id { get; set; }
        public virtual int ShikimoriId { get; set; }
        public virtual string Name { get; set; }
        public virtual string RussianName { get; set; }
        public virtual string Description { get; set; }
        public virtual int Episodes { get; set; }
        public virtual int Episodes_Aired { get; set; }
        public virtual State State { get; set; }
        public virtual Serial NextSeason { get; set; }
        public virtual bool OneSeason { get; set; } = true;
        public virtual IList<Episode> EpisodesDb { get; set; }

    }
}
