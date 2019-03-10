// Copyright (c) Leonardo Brugnara
// Full copyright and license information in LICENSE file

namespace DmlLib.Nodes
{
    public class GroupNode : DmlElement
    {
        public override string OuterXml
        {
            get
            {
                return InnerXml;
            }
        }

        public override DmlElementType ElementType => DmlElementType.Group;
    }
}