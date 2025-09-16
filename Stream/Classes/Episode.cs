using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stream.Classes
{
    public class Episode
    {
        public virtual int Id { get; set; }
        public virtual string EpisodeVideo { get; set; }
        public virtual string EpisodeAudio { get; set; }
        public virtual string EpisodeSubs { get; set; }
        public virtual int ServerID { get; set; } = 1;
        public virtual Serial Serial { get; set; }
        public virtual State State { get; set; } 
        public virtual TimeSpan Duration { get; set; }
    }
}
