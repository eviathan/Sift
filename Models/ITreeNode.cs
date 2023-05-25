using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sift.Models
{
    public interface ITreeNode
    {
        ITreeNode Next { get; set; }
        
        void Invoke();
    }
}