// Copyright (c) Leonardo Brugnara
// Full copyright and license information in LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using DmlLib.Output.Markdown;

namespace DmlLib.Nodes
{
    public abstract class DmlElement
    {
        private readonly bool _endTag;

        public DmlElement(Dictionary<string, string> attrs = null, bool endTag = true)
        {
            _endTag = endTag;
            Attributes = attrs ?? new Dictionary<string, string>();
            Properties = new Dictionary<string, object>();
            Children = new List<DmlElement>();
        }

        protected string TagName { get; set; }
        public Dictionary<string, object> Properties { get; private set; }
        public Dictionary<string, string> Attributes { get; private set; }
        public virtual List<DmlElement> Children { get; private set; }
        public DmlElement Parent { get; set; }
        public abstract DmlElementType ElementType { get; }

        public virtual string InnerText => string.Join("", this.Children.Select(c => c.InnerText));

        public virtual string InnerXml => string.Join("", this.Children.Select(c => c.OuterXml));

        public virtual string OuterXml
        {
            get
            {
                string attrs = "";
                if (Attributes != null)
                {
                    foreach (var key in Attributes.Keys)
                        attrs += $" {key}=\"{this.Attributes[key]}\"";
                }

                if (_endTag)
                    return $"<{this.TagName}{attrs}>{this.InnerXml}</{this.TagName}>";

                return $"<{this.TagName}{attrs} />";
            }
        }

        public virtual string ToMarkdown(MarkdownTranslationContext ctx) => string.Join("", Children.Select(c => c.ToMarkdown(ctx)));

        public void AddChild(DmlElement element)
        {
            element.Parent = this;
            Children.Add(element);
        }

        public void InsertChild(int index, DmlElement element)
        {
            element.Parent = this;
            Children.Insert(index, element);
        }

        public void MergeChildren(DmlElement elements)
        {
            elements.Children.ForEach(n => n.Parent = this);
            elements.Children.ForEach(Children.Add);
        }

        public DmlElement LastChild() => Children.LastOrDefault();

        public bool HasChildren() => Children.Count > 0;

        public bool AncestorIs(params DmlElementType[] t)
        {
            var parent = Parent;
            while (parent != null)
            {
                if (t.Contains(parent.ElementType))
                    return true;
                parent = parent.Parent;
            }
            return false;
        }
    }
}