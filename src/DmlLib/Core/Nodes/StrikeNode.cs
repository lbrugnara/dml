// Copyright (c) Leonardo Brugnara
// Full copyright and license information in LICENSE file

using DmlLib.Core.Formats;

namespace DmlLib.Core.Nodes
{
    public class StrikeNode: DmlElement
    {
        public StrikeNode()
            : base ()
        {
            TagName = "span";
            Attributes["style"] = "text-decoration: line-through";
        }

        public override DmlElementType ElementType => DmlElementType.Strike;

        public override string ToMarkdown(MarkdownTranslationContext ctx)
        {
            string content = base.ToMarkdown(ctx);
            if (AncestorIs(DmlElementType.CodeBlock, DmlElementType.InlineCode))
            { 
                return content;
            }
            return "~~" + content + "~~";
        }
    }
}