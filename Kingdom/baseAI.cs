using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kingdom
{
    public abstract class baseAI
    {
        public Player me;

        public abstract void Play();
    }
}
