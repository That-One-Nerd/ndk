using NLang.DevelopmentKit.Shared.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace NLang.DevelopmentKit.Shared;

public class TreeNode<T>(T value) : IComparable<TreeNode<T>>, IEnumerable<TreeNode<T>>
{
    public T Value { get; set; } = value;
    public List<TreeNode<T>> Children { get; } = [];
    public string? ValueFormat { get; set; }

    public TreeNode(T value, string valueFormat) : this(value)
    {
        ValueFormat = valueFormat;
    }

    public void Add(TreeNode<T> node) => Children.Add(node);
    public void Add(T item) => Children.Add(new(item));

    // Sorts the whole tree. These functions are recursive.
    public void Sort()
    {
        if (Children.Count == 0) return;

        foreach (TreeNode<T> node in Children) node.Sort();
        Children.Sort();
    }
    public void Sort(Comparison<TreeNode<T>> comparison)
    {
        if (Children.Count == 0) return;

        foreach (TreeNode<T> node in Children) node.Sort(comparison);
        Children.Sort(comparison);
    }
    public void Sort(Comparison<T> comparison)
    {
        if (Children.Count == 0) return;

        foreach (TreeNode<T> node in Children) node.Sort(comparison);
        Children.Sort((a, b) => comparison(a.Value, b.Value));
    }

    public IEnumerable<TreeNode<T>> Recurse()
    {
        foreach (TreeNode<T> subNode in Children)
        {
            yield return subNode;
            foreach (TreeNode<T> recursedSubNode in subNode.Recurse())
                yield return recursedSubNode;
        }
    }

    public IEnumerator<TreeNode<T>> GetEnumerator() => Children.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public int CompareTo(TreeNode<T>? other)
    {
        // TODO: Maybe sort by depth of children? Seems hard and kind of pointless.
        if (other is null) return 1;
        else if (Value is IComparable<T> compThisVal) return compThisVal.CompareTo(other.Value);
        else return 0;
    }

    public override string ToString() => ToString(new());
    public string ToString(ToStringProperties props)
    {
        // I know a BitArray could be used here for memory compactness,
        // but I don't know how to safely increase the size of one without
        // undoing all that compactness.
        // One could also just make the bit array the length of the deepest node,
        // but that's another recursive call that could take time.
        List<bool> lastChildFlags = [];

        StringBuilder result = new();
        result.Append($"{new string(' ', props.indent)}{ValueFormat}{Value}\x1b[0m{StringNodes(props, ref lastChildFlags)}\n");
        return result.ToString();
    }
    private StringBuilder StringNodes(ToStringProperties props, ref List<bool> lastChildFlags)
    {
        StringBuilder header = new();
        header.Append(' ', props.indent).Append(props.lineFormat);
        foreach (bool last in lastChildFlags)
        {
            if (last) header.Append("   ");
            else header.Append("│  ");
        }
        header.Append("\x1b[0m");

        StringBuilder result = new();
        for (int i = 0; i < Children.Count; i++)
        {
            TreeNode<T> child = Children[i];
            bool last = i == Children.Count - 1;

            int index = lastChildFlags.Count;
            lastChildFlags.Add(last);
            result.Append($"\n{header}{props.lineFormat}{(last ? "└─ " : "├─ ")}\x1b[0m{child.ValueFormat}{child.Value}\x1b[0m{child.StringNodes(props, ref lastChildFlags)}");
            lastChildFlags.RemoveAt(index);
        }

        return result;
    }

    public class ToStringProperties
    {
        public int indent;
        public string? lineFormat;

        public ToStringProperties()
        {
            indent = 2;
            lineFormat = null;
        }
    }
}

