// Copyright (c) Leonardo Brugnara
// Full copyright and license information in LICENSE file

using DmlLib.Core.Formats;

namespace DmlLib.Core.Nodes
{
    public class ReferenceNode : DmlElement
    {
        public ReferenceNode(string id)
        {
            TagName = "span";
            Attributes["id"] = id;
        }

        public override DmlElementType ElementType => DmlElementType.Reference;

        public override string ToMarkdown(MarkdownTranslationContext ctx)
        {
            if (AncestorIs(DmlElementType.CodeBlock, DmlElementType.InlineCode))
            { 
                return base.ToMarkdown(ctx);
            }
            return "<span id=\"" + Attributes["id"] + "\">" + base.ToMarkdown(ctx) + "</span>";
        }
    }
}