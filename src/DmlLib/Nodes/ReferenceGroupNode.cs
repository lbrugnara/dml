// Copyright (c) Leonardo Brugnara
// Full copyright and license information in LICENSE file

using System.Collections.Generic;
using System.Linq;

namespace DmlLib.Nodes
{
    public class ReferenceGroupNode : DmlElement
    {
        public List<DmlElement> Links { get; private set; }

        public ReferenceGroupNode()
        {
            this.Links = new List<DmlElement>();
        }

        public override DmlElementType ElementType => DmlElementType.ReferenceGroup;

        public override string OuterXml
        {
            get
            {
                return InnerXml.Trim();
            }
        }

        public override string InnerXml
        {
            get
            {
                return base.InnerXml.Trim() + string.Join("", Links.Select(l => l.OuterXml.Trim()));
            }
        }

        public void AddLink(ReferenceLinkNode link)
        {
            link.Parent = this;
            this.Links.Add(link);
        }
    }
}