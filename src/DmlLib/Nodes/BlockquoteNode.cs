// Copyright (c) Leonardo Brugnara
// Full copyright and license information in LICENSE file

using System.Linq;
using DmlLib.Semantic;
using DmlLib.Output.Markdown;

namespace DmlLib.Nodes
{
    public class BlockquoteNode : DmlElement
    {
        public BlockquoteNode()
        {
            TagName = "blockquote";
        }

        public override DmlElementType ElementType => DmlElementType.Blockquote;

        public override string ToMarkdown(MarkdownTranslationContext ctx)
        {
            string c = base.ToMarkdown(ctx);
            c = string.Join("\n", c.Split('\n').Select(l => l == "" ? "" : "> " + l ));
            return c;
        }
    }
}