using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HonorsProject_0._1.Models
{
    internal sealed class Adaptor
    {
        private readonly int[] array;
        private readonly Action<int, int> a;
        public Adaptor(int[] array, Action<int, int> a)
        {
            this.array = array;
            this.a = a;
        }

        public int this[int i]
        {
            get { return array[i]; }
            set { a(i, value); }
        }
    }
}
