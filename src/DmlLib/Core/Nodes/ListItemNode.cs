// Copyright (c) Leonardo Brugnara
// Full copyright and license information in LICENSE file

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
            int level = 0;

            if (parent.Properties.ContainsKey("indents"))
                level = (int) parent.Properties["indents"] * 4;
            else
                parent.Properties["indents"] = level;

            if (parent.Type == ListType.Ordered)
            {
                return "\n" + "".PadRight(level, ' ') + parent.Properties["index"] + ". " + base.ToMarkdown(ctx);
            }
            return "\n" + "".PadRight(level, ' ') + "- " + base.ToMarkdown(ctx);
        }
    }
}