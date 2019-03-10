// Copyright (c) Leonardo Brugnara
// Full copyright and license information in LICENSE file

namespace DmlLib.Semantic
{
    public class ParsingContext
    {
        public bool MarkupProcessingEnabled { get; private set; }

        public ParsingContext()
        {
            this.MarkupProcessingEnabled = true;
        }

        public ParsingContext(ParsingContext ctx)
        {
            this.MarkupProcessingEnabled = ctx.MarkupProcessingEnabled;
        }

        public bool SetMarkupProcessingEnabled(bool val)
        {
            bool oldstate = this.MarkupProcessingEnabled;
            this.MarkupProcessingEnabled = val;

            return oldstate;
        }
    }
}