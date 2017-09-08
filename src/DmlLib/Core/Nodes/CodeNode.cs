// Copyright (c) Leonardo Brugnara
// Full copyright and license information in LICENSE file

using System.Linq;
using DmlLib.Core.Formats;

namespace DmlLib.Core.Nodes
{
    public class CodeNode: DmlElement
    {
        private bool isBlock;

        public CodeNode(bool block)
            : base ()
        {
            TagName = "code";
            isBlock = block;
            if (isBlock)
            {
                Attributes["style"] = "display: block; white-space: pre-wrap";
            }
        }

        public override DmlElementType ElementType => isBlock ? DmlElementType.CodeBlock : DmlElementType.InlineCode;

        public override string ToMarkdown(MarkdownTranslationContext ctx)
        {
            string content = "";
            if (!isBlock)
            {
                content = base.ToMarkdown(ctx);
                int needed = 0;
                while (content.Contains("".PadLeft(needed++, '`')));
                string wrap = "".PadLeft(needed-1, '`');
                content = wrap + "" + content + "" + wrap;
            }
            else
            {
                string lang = (Attributes.ContainsKey("class") ? Attributes["class"] : "");
                string body = base.ToMarkdown(ctx);
                
                int needed = 2;
                while (body.Contains("".PadLeft(needed++, '`')));
                string wrap = "".PadLeft(needed, '`');

                if (ctx.CodeBlockSupport.HasFlag(CodeBlockSupport.Full))
                {
                    body = body.TrimEnd('\n');
                }
                else if (ctx.CodeBlockSupport.HasFlag(CodeBlockSupport.Multiline))
                {
                    body = string.Join("\n", body.Split('\n').Where(s => s != string.Empty)); // -> Remove empty spaces, MD doesn't continue the block with them
                }
                else
                {
                    var bodyparts = body.Split('\n');
                    if (bodyparts.Last() == "")
                        bodyparts = bodyparts.Take(bodyparts.Length - 1).ToArray();
                    body = string.Join("\n", bodyparts.Select(s => s != string.Empty ? s : wrap + "\n" + wrap)); // -> Close and re open in different code blocks
                }

                content = wrap + lang + "\n" + body + "\n" + wrap + "\n\n"; // End with two NewLines
            }
            return content;
        }
    }
}