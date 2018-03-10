// Copyright (c) Leonardo Brugnara
// Full copyright and license information in LICENSE file

using System.Collections.Generic;

namespace DmlLib.Core
{
    public partial class Lexer
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
}
