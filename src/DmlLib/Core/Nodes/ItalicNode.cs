// Copyright (c) Leonardo Brugnara
// Full copyright and license information in LICENSE file

using DmlLib.Core.Formats;

namespace DmlLib.Core.Nodes
{
    public class ItalicNode: DmlElement
    {
        public ItalicNode()
            : base ()
        {
            TagName = "i";
        }

        public override DmlElementType ElementType => DmlElementType.Italic;

        public override string ToMarkdown(MarkdownTranslationContext ctx)
        {
            string content = base.ToMarkdown(ctx);
            if (AncestorIs(DmlElementType.CodeBlock, DmlElementType.InlineCode))
            { 
                return content;
            }
            return "_" + content?.Trim() + "_";
        }
    }
}