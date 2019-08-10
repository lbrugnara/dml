// Copyright (c) Leonardo Brugnara
// Full copyright and license information in LICENSE file

using DmlLib.Output.Markdown;

namespace DmlLib.Nodes
{
    public class ReferenceLinkNode : DmlElement
    {
        public ReferenceLinkNode(string reference, string title)
        {
            TagName = "sup";
            LinkNode a = new LinkNode("#" + reference, reference);
            a.AddChild(new TextNode(!string.IsNullOrEmpty(title) ? title : reference));
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