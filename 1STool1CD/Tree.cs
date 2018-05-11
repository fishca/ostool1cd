using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _1STool1CD
{
    
    public struct node
    {
        public int _data_;
    }

    public class TreeNode<T>
    {
        public TreeNode(T value)
        {
            Value = value;
        }
        public T Value { get; set; }
        public TreeNode<T> Left { get; set; }
        public TreeNode<T> Right { get; set; }
        public TreeNode<T> Parent { get; set; }

    }

    public class Treee
    {
        public Treee()
        {

        }

    }

    /*
    public class Tree<T> : IEnumerable
    {
        TreeNode<T> root;

        public Tree()
        {

        }
        public IEnumerator<T> GetEnumerator()
        {
            var q = new Queue<T>();
            Action<TreeNode<T>> func = null;
            func = (TreeNode<T> node) =>
            {
                if (node.Left != null) func(node.Left);
                q.Enqueue(node.Value);
                if (node.Right != null) func(node.Right);
            };
            if (root != null) func(root);
            int count = q.Count;
            for (int i = 0; i < count; i++)
            {
                yield return q.Dequeue();
            }
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
    */

}
