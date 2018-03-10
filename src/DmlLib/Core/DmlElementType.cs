// Copyright (c) Leonardo Brugnara
// Full copyright and license information in LICENSE file

using System.Linq;

namespace DmlLib.Core
{
    public enum DmlElementType
    {
        Blockquote,
        CodeBlock,
        Custom,
        InlineCode,
        Document,
        Group,
        Header,
        ThematicBreak,
        Image,
        Italic,
        Link,
        List,
        ListItem,
        LineBreak,
        Paragraph,
        Preformatted,
        ReferenceLink,
        Reference,
        Strike,
        Strong,
        Text,
        Underlined
    }

    public static class DmlElementTypeExt
    {
        public static bool IsBlockElement(this DmlElementType self)
        {
            return (new DmlElementType[]
            {
                DmlElementType.Blockquote,
                DmlElementType.CodeBlock,
                DmlElementType.Document,
                DmlElementType.Header,
                DmlElementType.List,
                DmlElementType.ThematicBreak,                
                DmlElementType.Paragraph,
                DmlElementType.Preformatted,
            }).Contains(self);
        }

        public static bool IsInlineElement(this DmlElementType self)
        {
            return (new DmlElementType[]
            {
                DmlElementType.Custom,
                DmlElementType.InlineCode,
                DmlElementType.Group,
                DmlElementType.Image,
                DmlElementType.Italic,
                DmlElementType.Link,
                DmlElementType.LineBreak,
                DmlElementType.ListItem,
                DmlElementType.ReferenceLink,
                DmlElementType.Reference,
                DmlElementType.Strike,
                DmlElementType.Strong,
                DmlElementType.Text,
                DmlElementType.Underlined
            }).Contains(self);
        }
    }
}