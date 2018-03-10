// Copyright (c) Leonardo Brugnara
// Full copyright and license information in LICENSE file

namespace DmlLib.Core
{
    public enum TokenType
    {
        NewLine,
        DoubleNewLine,
        Indentation,
        BoldOpen,
        BoldClose,
        Italic,
        Underlined,
        InlineCode,
        CodeBlock,
        CodeBlockLang,
        Preformatted,
        Strikethrough,
        Text,
        Blockquote,
        HeaderStart,
        HeaderEnd,
        ListItem,
        Pipe,
        Reference,
        Colon,
        LinkStart,
        LinkEnd,
        ImageStart,
        ImageEnd,
        Escape,
        EscapeBlock,
        ThematicBreak,
        BlockquoteEndMarker,
        Lt
    }
}