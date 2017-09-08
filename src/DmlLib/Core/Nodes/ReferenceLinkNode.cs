// Copyright (c) Leonardo Brugnara
// Full copyright and license information in LICENSE file

using DmlLib.Core.Formats;

namespace DmlLib.Core.Nodes
{
    public class ReferenceLinkNode : DmlElement
    {
        public ReferenceLinkNode(string reference, string title)
        {
            TagName = "sup";
            LinkNode a = new LinkNode("#" + reference, title);
            a.AddChild(new TextNode(reference));
            AddChild(a);
        }

        public override DmlElementType ElementType => DmlElementType.ReferenceLink;

        public override string ToMarkdown(MarkdownTranslationContext ctx)
        {
            if (AncestorIs(DmlElementType.CodeBlock, DmlElementType.InlineCode))
            { 
                return base.ToMarkdown(ctx);
            }
            return "<sup>" + base.ToMarkdown(ctx) + "</sup>";
        }
    }
}