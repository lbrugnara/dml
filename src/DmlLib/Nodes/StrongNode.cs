// Copyright (c) Leonardo Brugnara
// Full copyright and license information in LICENSE file

using DmlLib.Output.Markdown;

namespace DmlLib.Nodes
{
    public class StrongNode: DmlElement
    {
        public StrongNode()
            : base ()
        {
            TagName = "strong";
        }

        public override DmlElementType ElementType => DmlElementType.Strong;

        public override string ToMarkdown(MarkdownTranslationContext ctx)
        {
            string content = base.ToMarkdown(ctx);
            if (AncestorIs(DmlElementType.CodeBlock, DmlElementType.InlineCode))
            { 
                return content;
            }
            return "**" + content  + "**";
        }
    }
}