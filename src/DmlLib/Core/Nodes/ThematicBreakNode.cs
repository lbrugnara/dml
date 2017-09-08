// Copyright (c) Leonardo Brugnara
// Full copyright and license information in LICENSE file

using DmlLib.Core.Formats;

namespace DmlLib.Core.Nodes
{
    public class ThematicBreakNode : DmlElement
    {
        public ThematicBreakNode()
            : base(null, false)
        {
            TagName = "hr";
        }

        public override DmlElementType ElementType => DmlElementType.ThematicBreak;

        public override string ToMarkdown(MarkdownTranslationContext ctx)
        {
            return "- - - -";
        }
    }
}