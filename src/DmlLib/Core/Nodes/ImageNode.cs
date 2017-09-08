// Copyright (c) Leonardo Brugnara
// Full copyright and license information in LICENSE file

using DmlLib.Core.Formats;

namespace DmlLib.Core.Nodes
{
    public class ImageNode : DmlElement
    {
        public ImageNode(string title, string source, string alttile)
            : base (null, false)
        {
            TagName = "img";
            Attributes["title"] = title.Trim();
            Attributes["src"] = source.Trim();
            Attributes["alt"] = alttile.Trim();
        }

        public override DmlElementType ElementType => DmlElementType.Image;

        public override string ToMarkdown(MarkdownTranslationContext ctx)
        {
            string src = (Attributes.ContainsKey("src") ? Attributes["src"] : "").Trim();
            string alt = (Attributes.ContainsKey("alt") ? Attributes["alt"] : "").Trim();
            string title = (Attributes.ContainsKey("title") ? Attributes["title"] : "").Trim();
            if (AncestorIs(DmlElementType.CodeBlock, DmlElementType.InlineCode))
            { 
                return string.Format("[{ {0} | {1} | {2} }]", alt, src, title);
            }
            return $"![{alt}]({src} \"{title}\")";
        }
    }
}