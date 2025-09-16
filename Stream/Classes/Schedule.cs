using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stream.Classes
{
    public class Schedule
    {
        public virtual int Id { get; set; }
        public virtual DateTime Start {  get; set; }
        public virtual Episode Episode { get; set; }

    }
}
