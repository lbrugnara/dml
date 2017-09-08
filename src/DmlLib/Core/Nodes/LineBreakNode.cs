// Copyright (c) Leonardo Brugnara
// Full copyright and license information in LICENSE file

using DmlLib.Core.Formats;

namespace DmlLib.Core.Nodes
{
    public class LineBreakNode : DmlElement
    {
        public LineBreakNode()
            : base(null, false)
        {
            TagName = "br";
        }

        public override DmlElementType ElementType => DmlElementType.LineBreak;

        public override string ToMarkdown(MarkdownTranslationContext ctx)
        {
            return "<br/>";
        }
    }
}