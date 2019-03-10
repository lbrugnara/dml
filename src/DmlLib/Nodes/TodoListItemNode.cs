// Copyright (c) Leonardo Brugnara
// Full copyright and license information in LICENSE file

using System.Collections.Generic;
using DmlLib.Output.Markdown;

namespace DmlLib.Nodes
{
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
            int level = 0;

            if (parent.Properties.ContainsKey("indents"))
                level = (int)parent.Properties["indents"] * 4;
            else
                parent.Properties["indents"] = level;

            if (complete)
            {
                return "\n" + "".PadRight(level, ' ') + "- [x] " + base.ToMarkdown(ctx);
            }
            return "\n" + "".PadRight(level, ' ') + "- [ ] " + base.ToMarkdown(ctx);
        }
    }
}