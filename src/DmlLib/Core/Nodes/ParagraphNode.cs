// Copyright (c) Leonardo Brugnara
// Full copyright and license information in LICENSE file

using DmlLib.Core.Formats;

namespace DmlLib.Core.Nodes
{
    public class ParagraphNode : DmlElement
    {
        public ParagraphNode()
            : base ()
        {
            TagName = "p";
        }

        public override DmlElementType ElementType => DmlElementType.Paragraph;

        public override string ToMarkdown(MarkdownTranslationContext ctx)
        {
            return base.ToMarkdown(ctx) + "\n";
        }
    }
}