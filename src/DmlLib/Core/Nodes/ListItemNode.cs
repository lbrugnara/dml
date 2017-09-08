// Copyright (c) Leonardo Brugnara
// Full copyright and license information in LICENSE file

using System.Collections.Generic;
using DmlLib.Core.Formats;

namespace DmlLib.Core.Nodes
{
    public class ListItemNode : DmlElement
    {
        public ListItemNode()
        {
            TagName = "li";
        }

        public override DmlElementType ElementType => DmlElementType.ListItem;

        public override string ToMarkdown(MarkdownTranslationContext ctx)
        {
            ListNode parent = Parent as ListNode;
            int level = (int)parent.Properties["level"] * 4;
            if (parent.Type == ListType.Ordered)
            {
                return "\n" + "".PadRight(level, ' ') + Properties["index"] + ". " + base.ToMarkdown(ctx);
            }
            return "\n" + "".PadRight(level, ' ') + "- " + base.ToMarkdown(ctx);
        }
    }

    public class TodoListItemNode : DmlElement
    {
        private bool complete;
        public TodoListItemNode(bool complete)
        {
            TagName = "li";
            this.complete = complete;
            var input = new CustomNode("input", new Dictionary<string,string>(){
                { "type", "checkbox" },
                { "disabled", "disabled" }
            }, false);
            AddChild(input);
            if (complete)
            {
                input.Attributes["checked"] = "checked";
            }       
        }

        public override DmlElementType ElementType => DmlElementType.ListItem;

        public override string ToMarkdown(MarkdownTranslationContext ctx)
        {
            ListNode parent = Parent as ListNode;
            int level = (int)parent.Properties["level"] * 4;
            if (complete)
            {
                return "\n" + "".PadRight(level, ' ') + "- [x] " + base.ToMarkdown(ctx);
            }
            return "\n" + "".PadRight(level, ' ') + "- [ ] " + base.ToMarkdown(ctx);
        }
    }
}