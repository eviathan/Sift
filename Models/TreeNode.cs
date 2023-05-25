using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sift.Models
{
    public class TreeNode
    {
        public TreeNodeType Type { get; set; }
        public TreeNode Next { get; internal set; }
    }
}