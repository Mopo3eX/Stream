using FluentNHibernate.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stream.Classes.Maps
{
    public  class ScheduleMap : ClassMap<Schedule>
    {
        public ScheduleMap()
        {
            Table("Schedule");
            Id(x => x.Id).GeneratedBy.Identity();
            Map(x => x.Start);
            References(x => x.Episode)
                .Column("EpisodeId")
                .Cascade.SaveUpdate();
            BatchSize(500);
        }
    }
}
