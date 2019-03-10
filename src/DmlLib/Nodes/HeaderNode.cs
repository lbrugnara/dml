// Copyright (c) Leonardo Brugnara
// Full copyright and license information in LICENSE file

using DmlLib.Semantic;
using DmlLib.Output.Markdown;

namespace DmlLib.Nodes
{
    public enum HeaderType
    {
        H1,
        H2,
        H3,
        H4
    }

    public class HeaderNode : DmlElement
    {
        private HeaderType type;

        public HeaderNode(HeaderType type)
        {
            TagName = type.ToString().ToLower();
            this.type = type;
        }

        public override DmlElementType ElementType => DmlElementType.Header;

        public override string ToMarkdown(MarkdownTranslationContext ctx)
        {
            string content = "";
            switch (type)
            {
                case HeaderType.H1:
                    content = "# ";
                    break;
                case HeaderType.H2:
                    content = "## ";
                    break;
                case HeaderType.H3:
                    content = "### ";
                    break;
                case HeaderType.H4:
                    content = "#### ";
                    break;
            }
            string htype = content;
            content += base.ToMarkdown(ctx);
            return content + "\n";
        }
    }
}