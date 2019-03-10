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
        protected string TagName;
        private bool _endTag;
        private Dictionary<string, object> _properties;
        private Dictionary<string, string> _attributes;
        protected List<DmlElement> children;
        public DmlElement Parent { get; private set; }

        public abstract DmlElementType ElementType { get; }

        public DmlElement(Dictionary<string, string> attrs = null, bool endTag = true)
        {
            _attributes = attrs;
            _endTag = endTag;
            children = new List<DmlElement>();
        }

        public Dictionary<string, string> Attributes
        {
            get
            {
                if (_attributes == null)
                {
                    _attributes = new Dictionary<string, string>();
                }
                return _attributes;
            }
        }

        public Dictionary<string, object> Properties
        {
            get
            {
                if (_properties == null)
                {
                    _properties = new Dictionary<string, object>();
                }
                return _properties;
            }
        }

        public virtual string InnerXml
        {
            get
            {
                return String.Join("", children.Select(c => c.OuterXml));
            }
        }

        public virtual string OuterXml
        {
            get
            {
                string attrs = "";
                if (Attributes != null)
                {
                    foreach (var key in Attributes.Keys)
                    {
                        attrs += string.Format(" {0}=\"{1}\"", key, Attributes[key]);
                    }
                }
                if (_endTag)
                {
                    return "<" + TagName + attrs + ">" + String.Join("", children.Select(c => c.OuterXml)) + "</"+ TagName +">";
                }
                return "<" + TagName + attrs + " />";
            }
        }

        public virtual List<DmlElement> Children
        {
            get
            {
                return this.children;
            }
        }

        public virtual string ToMarkdown(MarkdownTranslationContext ctx)
        {
            return String.Join("", children.Select(c => c.ToMarkdown(ctx)));
        }

        public void AddChild(DmlElement element)
        {
            element.Parent = this;
            children.Add(element);
        }

        public void InsertChild(int index, DmlElement element)
        {
            element.Parent = this;
            children.Insert(index, element);
        }

        public void MergeChildren(DmlElement elements)
        {
            elements.children.ForEach(n => n.Parent = this);
            elements.children.ForEach(children.Add);
        }

        public DmlElement LastChild()
        {
            return children.LastOrDefault();
        }

        public bool HasChildren()
        {
            return children.Count > 0;
        }

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