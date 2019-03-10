// Copyright (c) Leonardo Brugnara
// Full copyright and license information in LICENSE file

namespace DmlLib.Output.Markdown
{
    public enum CodeBlockSupport
    {
        Original,
        Full,
        Multiline
    }

    public struct MarkdownTranslationContext
    {
        public CodeBlockSupport CodeBlockSupport;
    }
}