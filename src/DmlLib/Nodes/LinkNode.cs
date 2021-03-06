// Copyright (c) Leonardo Brugnara
// Full copyright and license information in LICENSE file

using DmlLib.Semantic;
using DmlLib.Output.Markdown;

namespace DmlLib.Nodes
{
    public class LinkNode : DmlElement
    {
        public LinkNode(string href, string title)
        {
            TagName = "a";
            Attributes["href"] = href;
            Attributes["title"] = title;
        }

        public override DmlElementType ElementType => DmlElementType.Link;

        public override string ToMarkdown(MarkdownTranslationContext ctx)
        {
            string href = (Attributes.ContainsKey("href") ? Attributes["href"] : "").Trim();
            string title = (Attributes.ContainsKey("title") ? Attributes["title"] : "").Trim();
            if (AncestorIs(DmlElementType.CodeBlock, DmlElementType.InlineCode))
            { 
                return string.Format("[[ {0} | {1} | {2} ]]", base.ToMarkdown(ctx), href, title);
            }
            return $"[{base.ToMarkdown(ctx)}]({href} \"{title}\")";
        }

        public override string InnerXml => base.InnerXml.Trim();

        public override string InnerText => base.InnerText.Trim();
    }
}