using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sift.Models
{
    public class Tree
    {
        public TreeNode RootNode { get; set; }
        public TreeNode CurrentNode { get; set; }
        public TreeNode NextNode => CurrentNode.Next;
    }
}