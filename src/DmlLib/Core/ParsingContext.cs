// Copyright (c) Leonardo Brugnara
// Full copyright and license information in LICENSE file

namespace DmlLib.Core
{
    public class ParsingContext
    {
        private int _blockquoteLevel = 0;
        private int _listLevel = 0;
        private bool _processMarkupNodes;

        public ParsingContext()
        {
            _processMarkupNodes = true;
        }

        public ParsingContext(ParsingContext cloneThis)
        {
            _processMarkupNodes = cloneThis._processMarkupNodes;
            _blockquoteLevel = cloneThis._blockquoteLevel;
            _listLevel = cloneThis._listLevel;
        }

        public void IncrementBlockquoteLevel (int l = 1)
        {
            _blockquoteLevel += l;
        }

        public void DecrementBlockquoteLevel (int l = 1)
        {
            _blockquoteLevel -= l;
        }

        public int BlockquoteLevel { get { return _blockquoteLevel; } }

        public void IncrementListLevel (int l = 1)
        {
            _listLevel += l;
        }

        public void DecrementListLevel (int l = 1)
        {
            _listLevel -= l;
        }

        public int ListLevel { get { return _listLevel; } }

        public void MarkupProcessingEnabled(bool val)
        {
            _processMarkupNodes = val;
        }

        public bool IsMarkupProcessingEnabled()
        {
            return _processMarkupNodes;
        }
    }
}