// Copyright (c) Leonardo Brugnara
// Full copyright and license information in LICENSE file

using DmlLib.Core.Formats;

namespace DmlLib.Core.Nodes
{
    public class TextNode : DmlElement
    {
        private string text;
        public TextNode(string text)
        {
            this.text = text;
        }

        public override DmlElementType ElementType => DmlElementType.Text;

        private string Content
        {
            get
            {
                return text ?? "";
            }
        }

        public override string InnerXml
        {
            get
            {
                return Content;
            }
        }

        public override string OuterXml
        {
            get
            {
                return Content;
            }
        }

        public override string ToMarkdown(MarkdownTranslationContext ctx)
        {
            if (AncestorIs(DmlElementType.CodeBlock, DmlElementType.InlineCode))
                return Content;
            return Content.Replace("*", "\\*").Replace("~~", "\\~~").Replace("_", "\\_");
        }
    }
}