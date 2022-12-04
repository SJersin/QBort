using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QBort.Core.Managers
{
    public class ActiveStats
    {
        public static List<ActiveStats> Secretary = new List<ActiveStats>();
        public ulong GuildId { get; set; } = 0;
        public int UserFIFOCounter { get; set; } = 0;
        public ActiveStats(ulong GuildId)
        {
            this.GuildId = GuildId;
        }
    }
}
