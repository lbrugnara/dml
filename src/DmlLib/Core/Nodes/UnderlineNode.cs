// Copyright (c) Leonardo Brugnara
// Full copyright and license information in LICENSE file

using DmlLib.Core.Formats;

namespace DmlLib.Core.Nodes
{
    public class UnderlineNode: DmlElement
    {
        public UnderlineNode()
            : base ()
        {
            TagName = "span";
            Attributes["style"] = "text-decoration: underline";
        }

        public override DmlElementType ElementType => DmlElementType.Underlined;

        public override string ToMarkdown(MarkdownTranslationContext ctx)
        {
            string content = base.ToMarkdown(ctx);
            if (AncestorIs(DmlElementType.CodeBlock, DmlElementType.InlineCode))
            { 
                return content;
            }
            return "<span style=\"" + Attributes["style"] + "\">" + content + "</span>";
        }
    }
}