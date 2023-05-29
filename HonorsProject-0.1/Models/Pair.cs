using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HonorsProject_0._1.Models
{
    internal struct Pair
    {
        public static readonly ImmutableList<Pair> 
        Pairs = ImmutableList.Create(
           new Pair(0, 1), new Pair(1, 0),
           new Pair(-1, 0), new Pair(0, -1),
           new Pair(1, 1), new Pair(-1, -1),
           new Pair(1, -1), new Pair(-1, 1)
        );

        public readonly int x, y;

        private Pair(int x, int y)
        { this.x = x; this.y = y; }
    }
}
