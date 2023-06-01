using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sift.Sequencer
{
    public class Pool<TItem>
    {
        public TItem TakeNode<TItem>()
        {
            throw new NotImplementedException();
        }

        public TItem ReturnNode<TItem>(TItem item)
        {
            throw new NotImplementedException();
        }
    }
}