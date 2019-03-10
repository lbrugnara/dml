// Copyright (c) Leonardo Brugnara
// Full copyright and license information in LICENSE file

using System.Collections.Generic;

namespace DmlLib.Semantic
{
    public class LexerContext
    {
        public Stack<LexerState> State { get; }
        public bool Peek { get; set; }

        public LexerContext()
        {
            this.State = new Stack<LexerState>();
            this.State.Push(LexerState.Normal);
        }

        public enum LexerState
        {
            Normal
        }
    }
}
