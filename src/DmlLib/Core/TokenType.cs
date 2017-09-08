// Copyright (c) Leonardo Brugnara
// Full copyright and license information in LICENSE file

namespace DmlLib.Core
{
    public enum TokenType
    {
        NewLine,
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
        ReferenceStart,
        ReferenceEnd, 
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