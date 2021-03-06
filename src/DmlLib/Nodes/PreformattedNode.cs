// Copyright (c) Leonardo Brugnara
// Full copyright and license information in LICENSE file

namespace DmlLib.Nodes
{
    public class PreformattedNode : DmlElement
    {
        public PreformattedNode()
        {
            TagName = "pre";
        }

        public override DmlElementType ElementType => DmlElementType.Preformatted;
    }
}