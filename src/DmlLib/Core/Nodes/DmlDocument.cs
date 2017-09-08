// Copyright (c) Leonardo Brugnara
// Full copyright and license information in LICENSE file

using System.Collections.Generic;

namespace DmlLib.Core.Nodes
{
    public class DmlDocument : DmlElement
    {
        private DmlElement _head;
        private DmlElement _body;

        public DmlDocument(Dictionary<string, string> attrs = null, bool endTag = true)
            : base (attrs, endTag)
        {
            TagName = "html";
            _head = new CustomNode("head");
            _body = new CustomNode("body");
            AddChild(_head);
            AddChild(_body);
        }

        public override DmlElementType ElementType => DmlElementType.Document;

        public DmlElement Body
        {
            get { return _body; }
        }

        public DmlElement Head
        {
            get { return _head; }
        }
    }
}