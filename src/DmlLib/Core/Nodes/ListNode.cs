// Copyright (c) Leonardo Brugnara
// Full copyright and license information in LICENSE file

using System.Linq;
using DmlLib.Core.Formats;

namespace DmlLib.Core.Nodes
{
    public enum ListType
    {
        Unordered,
        Ordered,
        Todo
    }

    public class ListNode : DmlElement
    {
        private ListType type;
        public ListNode(ListType type)
        {
            this.type = type;
            switch (type)
            {
                case ListType.Unordered:
                    TagName = "ul";
                    break;
                case ListType.Ordered:
                    TagName = "ol";
                    break;
                case ListType.Todo:
                    TagName = "ul";
                    Attributes["class"] = "todo";
                    break;
            }
        }

        public ListType Type
        {
            get
            {
                return type;
            }
        }

        public override DmlElementType ElementType => DmlElementType.List;

        public override string ToMarkdown(MarkdownTranslationContext ctx)
        {
            string content = base.ToMarkdown(ctx);
            var parts = content.Split('\n');

            // Beauty hack: Check if after the first "\n" there is an indentation
            // in that case, use the \n, if not, discard it
            if (parts.Count() > 1 && !parts[1].StartsWith("    "))
            {
                parts = parts.Skip(1).ToArray();
            }
            content = string.Join("\n", parts);
            if ((int)Properties["level"] == 0)
                content += "\n";
            return content;
        }
    }
}