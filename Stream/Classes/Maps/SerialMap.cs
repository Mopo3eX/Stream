using FluentNHibernate.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stream.Classes.Maps
{
    public class SerialMap : ClassMap<Serial>
    {
        public SerialMap()
        {
            Table("Serials");
            Id(x => x.Id).GeneratedBy.Identity();
            Map(x => x.ShikimoriId);
            Map(x => x.Name).Length(512);
            Map(x => x.RussianName).Length(512);
            Map(x => x.Description).Length(2048);
            Map(x => x.Episodes);
            Map(x => x.Episodes_Aired);
            Map(x => x.State);
            References(x => x.NextSeason).Nullable();
            Map(x => x.OneSeason).Default("1");
            HasMany(x => x.EpisodesDb)
                .KeyColumn("SerialId")
                .Inverse()
                .Cascade.All();
            BatchSize(500);
        }
    }
}
