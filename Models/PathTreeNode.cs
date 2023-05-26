using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sift.Models
{
    public class PathTreeNode : ITreeNode
    {
        public ITreeNode Next { get; set; }

        public void DidStart() { }

        public void DidEnd() { }
    }
}