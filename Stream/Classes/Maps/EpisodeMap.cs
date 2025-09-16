using FluentNHibernate.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stream.Classes.Maps
{
    public class EpisodeMap : ClassMap<Episode>
    {
        public EpisodeMap()
        {
            Table("Episodes");
            Id(x => x.Id).GeneratedBy.Identity();
            Map(x => x.EpisodeVideo).Length(512);
            Map(x => x.EpisodeAudio).Length(512);
            Map(x => x.EpisodeSubs).Length(512);
            Map(x => x.State);
            Map(x => x.Duration);
            Map(x => x.ServerID);
            References(x => x.Serial)
                .Column("SerialId")
                .Cascade.SaveUpdate();
            BatchSize(500);
        }
    }
}
