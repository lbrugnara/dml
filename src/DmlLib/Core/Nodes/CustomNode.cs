// Copyright (c) Leonardo Brugnara
// Full copyright and license information in LICENSE file

using System.Collections.Generic;

namespace DmlLib.Core.Nodes
{
    public class CustomNode : DmlElement
    {
        public CustomNode(string name, Dictionary<string, string> attrs = null, bool endtag = true)
            : base (attrs, endtag)
        {
            TagName = name;
        }

        public override DmlElementType ElementType => DmlElementType.Custom;
    }
}