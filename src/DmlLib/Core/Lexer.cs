// Copyright (c) Leonardo Brugnara
// Full copyright and license information in LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;

namespace DmlLib.Core
{
    public partial class Lexer
    {
        // Input
        private string Source { get; }
        // Source ptr
        private int Pointer { get; set; }
        // Lookahead token
        private List<Token> Buffer { get; }
        private List<Token> Output { get; }

        // Markup tags
        public const string Header1 = "=";
        public const string Header2 = "~";
        public const string Header3 = "-";
        public const string Header4 = "`";
        public const string Ulist1 = "-";        
        public const string Ulist2 = "+";
        public const string Ulist3 = "*";
        public const string Olist1 = "#";        
        public const string ThematicBreak = "- - -";
        public const string Blockquote = ">";
        public const string Codeblock = "```";
        public const string DmlCodeblock = "!```";        
        public const string Pipe = "|";
        public const string Reference = "|";
        public const string Colon = ":";
        public const string InlineCode = "`";
        public const string EscapeBlock = "``";
        public const string Escape = "\\";
        public const string Indent = "\t";
        public const string NewLine = "\n";
        public const string DoubleNewLine = "\n\n";
        public const string Italic = "//";
        public const string Italic2 = "´";
        public const string Underline = "__";
        public const string Strikethrough = "~~";
        public const string BoldOpen = "[";
        public const string BoldClose = "]";
        public const string LinkOpen = "[[";
        public const string LinkClose = "]]";
        public const string ImgOpen = "[{";
        public const string ImgClose = "}]";
        public const string Lt = "<";

        private static readonly string[] Headers = new[] { Header1, Header2, Header3, Header4 };
        private static readonly string[] Lists = new[] { Ulist1, Ulist2, Ulist3, Olist1 };

        public Lexer(string source)
        {
            System.Diagnostics.Debug.Assert(source != null, "Input source cannot be null");
            source = source.Replace("\r", "");
            this.Source = source;
            this.Pointer = 0;
            this.Buffer = new List<Token>();
            this.Output = new List<Token>();
        }

        public List<Token> Tokenize()
        {
            List<Token> tokens = new List<Token>();

            Token tmp;
            while ((tmp = this.NextToken()) != null)
                tokens.Add(tmp);

            this.Pointer = 0;
            this.Buffer.Clear();
            this.Output.Clear();

            return tokens;
        }

        public Token NextToken()
        {
            Token tkn = null;

            if (this.Buffer.Count > 0)
            {
                tkn = this.Buffer[0];
                this.Buffer.RemoveAt(0);
                this.Output.Add(tkn);
                return tkn;
            }

            if (this.PeekChar() == null)
                return null;

            tkn = this.NextIsMarkupToken() ? this.GetNextMarkupToken() : this.GetNextTextToken();

            this.Output.Add(tkn);

            return tkn;
        }

        public void RestoreToken(Token token)
        {
            this.Buffer.Insert(0, token);
        }

        public Token PeekToken()
        {
            if (this.Buffer.Count > 0)
                return this.Buffer[0];

            Token peek = this.NextToken();

            this.Buffer.Insert(0, peek);

            return peek;
        }

        private bool NextIsMarkupToken() => this.NextMarkupToken(new LexerContext() { Peek = true }) != null;

        private Token GetNextMarkupToken() => this.NextMarkupToken(new LexerContext() { Peek = false });

        private Token NextMarkupToken(LexerContext ctx)
        {
            if (this.PeekChar() == null)
                return null;

            return this.CheckNewline(ctx)
                    // Block
                    ?? this.CheckIndentation(ctx)
                    ?? this.CheckThematicBreak(ctx)
                    ?? this.CheckListItem(ctx)
                    ?? this.CheckTodoListItem(ctx)
                    ?? this.CheckNumberedListItem(ctx)
                    ?? this.CheckLabeledListItem(ctx)
                    ?? this.CheckPreformatted(ctx)
                    ?? this.CheckBlockquote(ctx)
                    ?? this.CheckHeader(ctx)
                    ?? this.CheckCodeBlock(ctx)
                    ?? this.CheckDmlCodeBlock(ctx)
                    ?? this.CheckCodeBlockLang(ctx)
                    ?? this.CheckEscapeBlock(ctx)
                    ?? this.CheckReference(ctx)
                    // Inline elements
                    ?? this.CheckLt(ctx)
                    ?? this.CheckEscape(ctx)
                    ?? this.CheckColon(ctx)                    
                    ?? this.CheckPipe(ctx)
                    ?? this.CheckLink(ctx)
                    ?? this.CheckImage(ctx)
                    ?? this.CheckBold(ctx)
                    ?? this.CheckInlineCode(ctx)
                    ?? this.CheckItalic(ctx)
                    ?? this.CheckUnderlined(ctx)
                    ?? this.CheckStrikethrough(ctx);
        }

        private Token GetNextTextToken()
        {
            string txt = "";

            while (!this.NextIsMarkupToken() && this.PeekChar() != null)
                txt += this.ConsumeChar();

            Token token = new Token {
                Type = TokenType.Text,
                Value = txt
            };

            /*if (ctx.Peek && this.Buffer.Contains(token))
                this.Buffer.Insert(0, token);*/

            return token;
        }

        private string PeekChar(int length = 1)
        {
            if (this.Pointer >= this.Source.Length)
                return null;

            if (this.Pointer + length >= this.Source.Length)
                return this.Source.Substring(this.Pointer, this.Source.Length - this.Pointer);;

            return this.Source.Substring(this.Pointer, length);
        }

        private bool IsEndOfInput(int index=0) => this.Pointer + index >= this.Source.Length;

        private string ConsumeChar(int length = 1)
        {
            string tmp = this.PeekChar(length);

            this.Pointer += length;

            return tmp;
        }

        private bool LastIs(TokenType type) => Output.LastOrDefault()?.Type == type;

        private Token CheckHeader(LexerContext ctx)
        {
            string lookahead = this.PeekChar(1);

            if (!Headers.Contains(lookahead))
                return null;
            
            if (!this.IsValidBlockStart())
                return null;

            if (this.Output.Count == 0)
                return null;

            // If previous line is just a newline, it is not a header
            if (this.Output.Count >= 2 && this.Output.Last().Type == TokenType.NewLine && this.Output[this.Output.Count-2].Type == TokenType.NewLine)
                return null;

            string tokenval = "";
            string tmp;

            while ((tmp = this.PeekChar()) == lookahead || tmp == " ")
                tokenval += this.ConsumeChar();

            if ((tmp == NewLine || this.IsEndOfInput()) && tokenval.Length >= 4)
            {
                if (ctx.Peek)
                    this.Pointer -= tokenval.Length;

                return new Token { Type = TokenType.HeaderStart, Value = tokenval };
            }

            this.Pointer -= tokenval.Length;

            return null;
        }

        private Token CheckThematicBreak(LexerContext ctx)
        {
            if (!this.IsValidBlockStart())
                return null;

            string lookahead = this.PeekChar(ThematicBreak.Length);

            if (lookahead == ThematicBreak)
                return new Token { Type = TokenType.ThematicBreak, Value = ctx.Peek ? lookahead : this.ConsumeChar(ThematicBreak.Length) };

            return null;
        }

        private Token CheckListItem(LexerContext ctx)
        {
            string lookahead = this.PeekChar(2);

            if (this.IsValidBlockStart() && lookahead.Length == 2 && Lists.Contains(lookahead[0].ToString()) && lookahead[1] == ' ')
                return new Token { Type = TokenType.ListItem, Value = ctx.Peek ? lookahead : this.ConsumeChar(2) };

            return null;
        }

        private Token CheckTodoListItem(LexerContext ctx)
        {
            string lookahead = this.PeekChar(4);

            if (this.IsValidBlockStart() && (lookahead == "[ ] " || lookahead == "[x] " || lookahead == "[X] "))
                return new Token { Type = TokenType.ListItem, Value = ctx.Peek ? lookahead : ConsumeChar(4) };

            return null;
        }

        private Token CheckNumberedListItem(LexerContext ctx)
        {
            if (!this.IsValidBlockStart())
                return null;

            int i=1;
            string lookahead = "";
            while ((lookahead = this.PeekChar(i)).Length > 0 && !this.IsEndOfInput(i) && lookahead.All(c => char.IsDigit(c)))
                i++;

            string tmp = this.PeekChar(++i); // The NOT digit that broke the previos while plus the needed space

            if (tmp == lookahead) // End of file
                return null;

            if (tmp.EndsWith(". ") || tmp.EndsWith(") "))
            {
                lookahead = "# ";

                if (!ctx.Peek)
                    this.ConsumeChar(i);

                return new Token { Type = TokenType.ListItem, Value = lookahead, OriginalValue = tmp };
            }

            return null;
        }

        private Token CheckLabeledListItem(LexerContext ctx)
        {
            if (!this.IsValidBlockStart())
                return null;

            string lookahead = this.PeekChar(3);

            if (lookahead != null && char.IsLetter(lookahead[0]) && (lookahead.EndsWith(". ") || lookahead.EndsWith(") ")))
            {
                string originalValue = lookahead;
                lookahead = "- ";

                if (!ctx.Peek)
                    this.ConsumeChar(3);

                return new Token { Type = TokenType.ListItem, Value = lookahead, OriginalValue = originalValue };
            }
            return null;
        }

        private Token CheckIndentation(LexerContext ctx)
        {
            bool isValidBlockStart = this.IsValidBlockStart();

            // Check tab
            string lookahead = this.PeekChar();

            if (isValidBlockStart && lookahead == Indent)
            {
                if (!ctx.Peek)
                    this.ConsumeChar();

                return new Token { Type = TokenType.Indentation, Value = "    " };
            }

            // Check at least 4 white spaces
            lookahead = PeekChar(4);

            if (isValidBlockStart && lookahead?.Length >= 4 && lookahead?.Trim() == string.Empty)
                return new Token { Type = TokenType.Indentation, Value = ctx.Peek ? lookahead : this.ConsumeChar(4) };

            return null;
        }

        private Token CheckPreformatted(LexerContext ctx)
        {
            Token last = this.Output.LastOrDefault();

            if (last != null && last.Type == TokenType.Indentation && this.CheckListItem(new LexerContext() { Peek = true }) == null)
                return new Token { Type = TokenType.Preformatted, Value = "" };

            return null;
        }

        private Token CheckNewline(LexerContext ctx)
        {
            var lookahead = this.PeekChar(DoubleNewLine.Length);

            if (lookahead == DoubleNewLine)
               return new Token { Type = TokenType.DoubleNewLine, Value = ctx.Peek ? lookahead : this.ConsumeChar(DoubleNewLine.Length) };

            lookahead = this.PeekChar();

            if (lookahead == NewLine)
            {
                return new Token { Type = TokenType.NewLine, Value = ctx.Peek ? lookahead : this.ConsumeChar() };
            }

            return null;
        }

        private Token CheckLt(LexerContext ctx)
        {
            string lookahead = this.PeekChar();

            if (lookahead == Lt)
                return new Token { Type = TokenType.Lt, Value = ctx.Peek ? lookahead : this.ConsumeChar() };

            return null;
        }

        private Token CheckBold(LexerContext ctx)
        {
            string lookahead = this.PeekChar();

            if (lookahead == BoldOpen)
                return new Token { Type = TokenType.BoldOpen, Value = ctx.Peek ? lookahead : this.ConsumeChar() };

            if (lookahead == BoldClose)
                return new Token { Type = TokenType.BoldClose, Value = ctx.Peek ? lookahead : this.ConsumeChar() };

            return null;
        }

        private Token CheckItalic(LexerContext ctx)
        {
            string lookahead = this.PeekChar(Italic.Length);

            if (lookahead == Italic)
                return new Token { Type = TokenType.Italic, Value = ctx.Peek ? lookahead : this.ConsumeChar(Italic.Length) };


            lookahead = this.PeekChar(Italic2.Length);

            if (lookahead == Italic2)
                return new Token { Type = TokenType.Italic, Value = ctx.Peek ? lookahead : this.ConsumeChar(Italic2.Length) };

            return null;
        }

        private Token CheckInlineCode(LexerContext ctx)
        {
            string lookahead = this.PeekChar();

            if (lookahead == InlineCode)
                return new Token { Type = TokenType.InlineCode, Value = ctx.Peek ? lookahead : this.ConsumeChar() };

            return null;
        }        

        private Token CheckUnderlined(LexerContext ctx)
        {
            string lookahead = this.PeekChar(Underline.Length);

            if (lookahead == Underline)
                return new Token { Type = TokenType.Underlined, Value = ctx.Peek ? lookahead : this.ConsumeChar(Underline.Length) };

            return null;
        }

        private Token CheckPipe(LexerContext ctx)
        {
            string lookahead = this.PeekChar();

            if (lookahead == Pipe)
                return new Token { Type = TokenType.Pipe, Value = ctx.Peek ? lookahead : this.ConsumeChar() };

            return null;
        }

        private Token CheckReference(LexerContext ctx)
        {
            string lookahead = this.PeekChar();

            if (this.IsValidBlockStart() && lookahead == Reference)
                return new Token { Type = TokenType.Reference, Value = ctx.Peek ? lookahead : this.ConsumeChar() };

            return null;
        }

        private Token CheckColon(LexerContext ctx)
        {
            string lookahead = this.PeekChar(Colon.Length);

            if (lookahead == Colon)
                return new Token { Type = TokenType.Colon, Value = ctx.Peek ? lookahead : this.ConsumeChar(Colon.Length) };

            return null;
        }

        private Token CheckLink(LexerContext ctx)
        {
            string lookahead = this.PeekChar(LinkOpen.Length);
            bool lastIsEscape = this.LastIs(TokenType.Escape);

            if (lookahead == LinkOpen && !lastIsEscape)
                return new Token { Type = TokenType.LinkStart, Value = ctx.Peek ? lookahead : this.ConsumeChar(LinkOpen.Length) };

            if (lookahead == LinkClose && !lastIsEscape)
                return new Token { Type = TokenType.LinkEnd, Value = ctx.Peek ? lookahead : this.ConsumeChar(LinkClose.Length) };

            return null;
        }

        private Token CheckImage(LexerContext ctx)
        {
            string lookahead = this.PeekChar(ImgOpen.Length);
            bool lastIsEscape = this.LastIs(TokenType.Escape);

            if (lookahead == ImgOpen && !lastIsEscape)
                return new Token { Type = TokenType.ImageStart, Value = ctx.Peek ? lookahead : this.ConsumeChar(ImgOpen.Length) };

            if (lookahead == ImgClose && !lastIsEscape)
                return new Token { Type = TokenType.ImageEnd, Value = ctx.Peek ? lookahead : this.ConsumeChar(ImgClose.Length) };

            return null;
        }

        private Token CheckStrikethrough(LexerContext ctx)
        {
            string lookahead = this.PeekChar(Strikethrough.Length);

            if (lookahead == Strikethrough)
                return new Token { Type = TokenType.Strikethrough, Value = ctx.Peek ? lookahead : this.ConsumeChar(Strikethrough.Length) };

            return null;
        }

        private Token CheckBlockquote(LexerContext ctx)
        {
            if (!this.IsValidBlockStart())
                return null;

            string lookahead = this.PeekChar();

            if (lookahead != Blockquote)
                return null;

            int q = 1;
            string tmp = null;
            lookahead = "";
            do
            {
                if (this.IsEndOfInput(q))
                    break;

                tmp = this.PeekChar(q);

                if (tmp[q-1] == Blockquote[0])
                {
                    q++;
                    lookahead = tmp;
                    continue;
                }

                break;

            } while (true);

            return new Token { 
                Type = TokenType.Blockquote,
                Value = ctx.Peek ? lookahead : this.ConsumeChar(Math.Max(1, q-1)) 
            };
        }

        private Token CheckEscapeBlock(LexerContext ctx)
        {
            string lookahead = this.PeekChar(2);

            if (lookahead == EscapeBlock && !this.LastIs(TokenType.Escape))
                return new Token { Type = TokenType.EscapeBlock, Value = ctx.Peek ? lookahead : this.ConsumeChar(2) };

            return null;
        }

        private Token CheckEscape(LexerContext ctx)
        {
            string lookahead = this.PeekChar();

            if (lookahead == Escape)
                return new Token { Type = TokenType.Escape, Value = ctx.Peek ? lookahead : this.ConsumeChar() };

            return null;
        }

        private Token CheckCodeBlock(LexerContext ctx)
        {
            if (!this.IsValidBlockStart())
                return null;
                
            string lookahead = this.PeekChar(Codeblock.Length);

            if (lookahead != Codeblock)
                return null;

            
            // Check for more backticks to see if it is a header
            string str = this.PeekChar(Codeblock.Length+1);

            if (str.Length == Codeblock.Length+1 && str.Last() == Codeblock[0])
                return null;

            return new Token { Type = TokenType.CodeBlock, Value = ctx.Peek ? lookahead : this.ConsumeChar(Codeblock.Length) };
        }
        
        private Token CheckDmlCodeBlock(LexerContext ctx)
        {
            if (!this.IsValidBlockStart())
                return null;

            string lookahead = this.PeekChar(DmlCodeblock.Length);

            if (lookahead != DmlCodeblock)
                return null;

            string str = this.PeekChar(DmlCodeblock.Length+1);

            if (str.Length == DmlCodeblock.Length+1 && str.Last() == DmlCodeblock[0])
                return null;

            return new Token { Type = TokenType.CodeBlock, Value = ctx.Peek ? lookahead : this.ConsumeChar(DmlCodeblock.Length) };
        }

        private Token CheckCodeBlockLang(LexerContext ctx)
        {
            string lookahead = this.PeekChar();
            Token last = this.Output.LastOrDefault();

            if (last == null || last.Type != TokenType.CodeBlock || lookahead == null || lookahead == "\n")
                return null;

            lookahead = "";
            int q = 1;
            string tmp = null;            
            while ((tmp = this.PeekChar(q)) != lookahead && !tmp.EndsWith("\n"))
            {
                lookahead = tmp;
                q++;
            }

            if (!ctx.Peek)
                this.ConsumeChar(q-1);

            return new Token () { Type = TokenType.CodeBlockLang, Value = lookahead };
        }

        private bool IsValidBlockStart()
        {
            Token tkn = this.Output.LastOrDefault();

            if (tkn == null || tkn.Type == TokenType.NewLine || tkn.Type == TokenType.DoubleNewLine || tkn.Type == TokenType.Blockquote)
                return true;

            int index = this.Output.Count - 1;
            while (index >= 0)
            {
                tkn = this.Output[index--];

                // Skip Indentation, Escape and Empty strings
                if (tkn.Type == TokenType.Indentation || tkn.Type == TokenType.Escape || (tkn.Type == TokenType.Text && tkn.Value.Trim() == string.Empty))
                    continue;
                
                // Break to check for valid start blocks
                break;
            }

            return tkn == null || tkn.Type == TokenType.NewLine || tkn.Type == TokenType.DoubleNewLine || tkn.Type == TokenType.Blockquote;
        }
    }
}
