// Copyright (c) Leonardo Brugnara
// Full copyright and license information in LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;

namespace DmlLib.Core
{
    public enum TokenizerState
    {
        Normal,
        Reference,
        Code
    }

    public class Tokenizer
    {
        private Stack<TokenizerState> state;
        // Input
        private string source;
        // Source ptr
        private int ptr;
        // Lookahead token
        private List<Token> buffer;
        private List<Token> output;

        // Markup tags
        public const String HEADER_1 = "=";
        public const String HEADER_2 = "~";
        public const String HEADER_3 = "-";
        public const String HEADER_4 = "`";
        private static readonly string[] HEADERS = new []{ HEADER_1, HEADER_2, HEADER_3, HEADER_4 };
        public const String ULIST_1 = "-";        
        public const String ULIST_2 = "+";
        public const String ULIST_3 = "*";
        public const String OLIST_1 = "#";
        private static readonly string[] LISTS = new []{ ULIST_1, ULIST_2, ULIST_3, OLIST_1 };
        public const String THEMATIC_BREAK = "- - -";
        public const String BLOCKQUOTE = ">";
        public const String CODEBLOCK = "```";

        public const String DML_CODEBLOCK = "!```";
        
        public const String PIPE = "|";
        public const String REFERENCE = "|";
        public const String COLON = ":";
        public const String INLINE_CODE = "`";
        public const String ESCAPE_BLOCK = "``";
        public const String ESCAPE = "\\";
        public const String INDENT = "\t";
        public const String NEW_LINE = "\n";
        public const String ITALIC = "//";
        public const String ITALIC2 = "´";
        public const String UNDERLINE = "__";
        public const String STRIKETHROUGH = "~~";
        public const String BOLD_OPEN = "[";
        public const String BOLD_CLOSE = "]";
        public const String LINK_OPEN = "[[";
        public const String LINK_CLOSE = "]]";
        public const String IMG_OPEN = "[{";
        public const String IMG_CLOSE = "}]";

        public const String LT = "<";

        public Tokenizer(string src)
        {
            System.Diagnostics.Debug.Assert(src != null, "Input source cannot be null");
            src = src.Replace("\r", "");
            source = src;
            ptr = 0;
            buffer = new List<Token>();
            output = new List<Token>();
            state = new Stack<TokenizerState>();
            state.Push(TokenizerState.Normal);
        }

        public List<Token> Tokenize()
        {
            List<Token> tokens = new List<Token>();
            Token tmp;
            while ((tmp = NextToken()) != null)
            {
                tokens.Add(tmp);
            }
            ptr = 0;
            buffer.Clear();
            output.Clear();
            return tokens;
        }

        public Token NextToken()
        {
            Token tkn = null;
            if (buffer.Count > 0)
            {
                tkn = buffer[0];
                buffer.RemoveAt(0);
                output.Add(tkn);
                return tkn;
            }
            if (PeekChar() == null)
            {
                return null;
            }
            tkn = NextMarkupToken(true) != null ? NextMarkupToken() : NextTextToken();
            output.Add(tkn);
            return tkn;
        }

        public void RestoreToken(Token token)
        {
            buffer.Insert(0, token);
        }

        public Token PeekToken()
        {
            if (buffer.Count > 0)
                return buffer[0];
            Token peek = NextToken();            
            buffer.Insert(0, peek);
            return peek;
        }

        private Token NextMarkupToken(bool justPeek = false)
        {
            Token tmp = null;
            if (PeekChar() == null)
                return null;
            tmp = CheckNewline(justPeek)
                    // Block
                    ?? CheckIndentation(justPeek)
                    ?? CheckThematicBreak(justPeek)
                    ?? CheckListItem(justPeek)
                    ?? CheckTodoListItem(justPeek)
                    ?? CheckNumberedListItem(justPeek)
                    ?? CheckLabeledListItem(justPeek)
                    ?? CheckPreformatted(justPeek)
                    ?? CheckBlockquote(justPeek)
                    ?? CheckHeader(justPeek)
                    ?? CheckCodeBlock(justPeek)
                    ?? CheckDmlCodeBlock(justPeek)
                    ?? CheckCodeBlockLang(justPeek)
                    ?? CheckEscapeBlock(justPeek)
                    // Inline elements
                    ?? CheckLt(justPeek)
                    ?? CheckEscape(justPeek)
                    ?? CheckReference(justPeek)
                    ?? CheckPipe(justPeek)
                    ?? CheckLink(justPeek)
                    ?? CheckImage(justPeek)
                    ?? CheckBold(justPeek)
                    ?? CheckInlineCode(justPeek)
                    ?? CheckItalic(justPeek)
                    ?? CheckUnderlined(justPeek)
                    ?? CheckStrikethrough(justPeek);
            return tmp;
        }

        private Token NextTextToken(bool justPeek = false)
        {
            String txt = "";
            Token tmp;
            while ((tmp = NextMarkupToken(true)) == null && PeekChar() != null)
            {
                txt += ConsumeChar();
            }
            Token token = new Token { Type=TokenType.Text, Value = txt };
            if (justPeek && buffer.Contains(token))
            {
                buffer.Insert(0, token);
            }
            return token;
        }

        private string PeekChar(int length = 1)
        {
            if (ptr >= source.Length)
                return null;
            if (ptr + length >= source.Length)
                return source.Substring(ptr, source.Length - ptr);;
            return source.Substring(ptr, length);
        }

        private bool IsEndOfInput(int index=0)
        {
            return ptr + index >= source.Length;
        }

        private string ConsumeChar(int length = 1)
        {
            string tmp = PeekChar(length);
            ptr += length;
            return tmp;
        }

        private Token CheckHeader(bool justPeek = false)
        {
            //if (state.First() != TokenizerState.Normal)
            //    return null;
            string lookahead = PeekChar(1);
            if (!HEADERS.Contains(lookahead))
                return null;
            
            if (!IsValidBlockStart())
                return null;
            if (output.Count >= 2 && output[output.Count-1].Type == TokenType.NewLine && output[output.Count-2].Type == TokenType.NewLine)
            {
                return null;
            }

            string tokenval = "";
            string tmp;
            while ((tmp = PeekChar()) == lookahead || tmp == " ")
            {     
                tokenval += ConsumeChar();           
            }

            if ((tmp == NEW_LINE || IsEndOfInput()) && tokenval.Length >= 4)
            {
                if (justPeek)
                {
                    ptr -= tokenval.Length;
                }
                return new Token { Type = TokenType.HeaderStart, Value = tokenval };
            }
            ptr -= tokenval.Length;
            return null;
        }

        private Token CheckThematicBreak(bool justPeek = false)
        {
            if (!IsValidBlockStart())
                return null;
            string lookahead = PeekChar(THEMATIC_BREAK.Length);
            if (lookahead == THEMATIC_BREAK)
            {
                return new Token { Type = TokenType.ThematicBreak, Value = justPeek ? lookahead : ConsumeChar(THEMATIC_BREAK.Length) };
            }
            return null;
        }

        private Token CheckListItem(bool justPeek = false)
        {
            string lookahead = PeekChar(2);
            if (IsValidBlockStart() && lookahead.Length == 2 && LISTS.Contains(lookahead[0].ToString()) && lookahead[1] == ' ')
            {
                return new Token { Type = TokenType.ListItem, Value = justPeek ? lookahead : ConsumeChar(2) };
            }
            return null;
        }

        private Token CheckTodoListItem(bool justPeek = false)
        {
            string lookahead = PeekChar(4);
            if (IsValidBlockStart() && (lookahead == "[ ] " || lookahead == "[x] " || lookahead == "[X] "))
            {
                return new Token { Type = TokenType.ListItem, Value = justPeek ? lookahead : ConsumeChar(4) };
            }
            return null;
        }

        private Token CheckNumberedListItem(bool justPeek = false)
        {
            if (!IsValidBlockStart())
            {
                return null;
            }
            int i=1;
            string lookahead = "";
            while ((lookahead = PeekChar(i)).Length > 0 && !IsEndOfInput(i) && lookahead.All(c => char.IsDigit(c)))
            {
                i++;
            }
            string tmp = PeekChar(++i); // The NOT digit that broke the previos while plus the needed space
            if (tmp == lookahead) // End of file
            {
                return null;
            }
            if (tmp.EndsWith(". ") || tmp.EndsWith(") "))
            {
                lookahead = "# ";
                if (!justPeek)
                {
                    ConsumeChar(i);
                }
                return new Token { Type = TokenType.ListItem, Value = lookahead, OriginalValue = tmp };
            }
            return null;
        }

        private Token CheckLabeledListItem(bool justPeek = false)
        {
            if (!IsValidBlockStart())
            {
                return null;
            }

            string lookahead = PeekChar(3);
            if (lookahead != null && char.IsLetter(lookahead[0]) && (lookahead.EndsWith(". ") || lookahead.EndsWith(") ")))
            {
                string originalValue = lookahead;
                lookahead = "- ";
                if (!justPeek)
                {
                    ConsumeChar(3);
                }
                return new Token { Type = TokenType.ListItem, Value = lookahead, OriginalValue = originalValue };
            }
            return null;
        }

        private Token CheckIndentation(bool justPeek = false)
        {
            bool isValidBlockStart = IsValidBlockStart();
            string lookahead = PeekChar();
            if (isValidBlockStart && lookahead == INDENT)
            {
                if (!justPeek)
                {
                    ConsumeChar();
                }
                string value = "    ";
                return new Token { Type = TokenType.Indentation, Value = value };
            }
            lookahead = PeekChar(4);
            if (isValidBlockStart && lookahead != null && lookahead.Length >= 4 && lookahead.Trim() == string.Empty)
            {
                return new Token { Type = TokenType.Indentation, Value = justPeek ? lookahead : ConsumeChar(4) };
            }
            return null;
        }

        private Token CheckPreformatted(bool justPeek = false)
        {
            Token last = output.LastOrDefault();
            if (last != null && last.Type == TokenType.Indentation && CheckListItem(true) == null)
            {
                return new Token { Type = TokenType.Preformatted, Value = "" };
            }
            return null;
        }

        private Token CheckNewline(bool justPeek = false)
        {
            string lookahead = PeekChar();
            if (lookahead == NEW_LINE)
            {
                if (!justPeek)
                {
                    if (state.First() == TokenizerState.Reference)
                    {
                        state.Pop();
                    }
                }
                return new Token { Type = TokenType.NewLine, Value = justPeek ? lookahead : ConsumeChar() };
            }
            return null;
        }

        private Token CheckLt(bool justPeek = false)
        {
            string lookahead = PeekChar();
            if (lookahead == LT)
            {
                return new Token { Type = TokenType.Lt, Value = justPeek ? lookahead : ConsumeChar() };
            }
            return null;
        }

        private Token CheckBold(bool justPeek = false)
        {
            string lookahead = PeekChar();
            if (lookahead == BOLD_OPEN)
            {                
                return new Token { Type = TokenType.BoldOpen, Value = justPeek ? lookahead : ConsumeChar() };
            }
            else if (lookahead == BOLD_CLOSE)
            {                
                return new Token { Type = TokenType.BoldClose, Value = justPeek ? lookahead : ConsumeChar() };
            }
            return null;
        }

        private Token CheckItalic(bool justPeek = false)
        {
            string lookahead = PeekChar(ITALIC.Length);
            if (lookahead == ITALIC)
            {                
                return new Token { Type = TokenType.Italic, Value = justPeek ? lookahead : ConsumeChar(ITALIC.Length) };
            }
            lookahead = PeekChar(ITALIC2.Length);
            if (lookahead == ITALIC2)
            {                
                return new Token { Type = TokenType.Italic, Value = justPeek ? lookahead : ConsumeChar(ITALIC2.Length) };
            }
            return null;
        }

        private Token CheckInlineCode(bool justPeek = false)
        {
            string lookahead = PeekChar();
            if (lookahead == INLINE_CODE)
            {                
                return new Token { Type = TokenType.InlineCode, Value = justPeek ? lookahead : ConsumeChar() };
            }
            return null;
        }        

        private Token CheckUnderlined(bool justPeek = false)
        {
            string lookahead = PeekChar(UNDERLINE.Length);
            if (lookahead == UNDERLINE)
            {                
                return new Token { Type = TokenType.Underlined, Value = justPeek ? lookahead : ConsumeChar(UNDERLINE.Length) };
            }
            return null;
        }

        private Token CheckPipe(bool justPeek = false)
        {
            string lookahead = PeekChar();
            if (lookahead == PIPE)
            {                
                return new Token { Type = TokenType.Pipe, Value = justPeek ? lookahead : ConsumeChar() };
            }
            return null;
        }

        private Token CheckReference(bool justPeek = false)
        {
            string lookahead = PeekChar();
            if (IsValidBlockStart() && lookahead == REFERENCE)
            {
                if (!justPeek)
                {
                    state.Push(TokenizerState.Reference);
                }
                return new Token { Type = TokenType.ReferenceStart, Value = justPeek ? lookahead : ConsumeChar() };
            }
            else if (state.First() == TokenizerState.Reference && lookahead == COLON)
            {
                if (!justPeek)
                {
                    state.Pop();
                }
                return new Token { Type = TokenType.ReferenceEnd, Value = justPeek ? lookahead : ConsumeChar() };
            }
            return null;
        }

        private Token CheckLink(bool justPeek = false)
        {
            string lookahead = PeekChar(LINK_OPEN.Length);
            bool lastIsEscape = LastIs(TokenType.Escape);
            if (lookahead == LINK_OPEN && !lastIsEscape)
            {                
                return new Token { Type = TokenType.LinkStart, Value = justPeek ? lookahead : ConsumeChar(LINK_OPEN.Length) };
            }
            else if (lookahead == LINK_CLOSE && !lastIsEscape)
            {                
                return new Token { Type = TokenType.LinkEnd, Value = justPeek ? lookahead : ConsumeChar(LINK_CLOSE.Length) };
            }
            return null;
        }

        private Token CheckImage(bool justPeek = false)
        {
            string lookahead = PeekChar(IMG_OPEN.Length);
            bool lastIsEscape = LastIs(TokenType.Escape);
            if (lookahead == IMG_OPEN && !lastIsEscape)
            {                
                return new Token { Type = TokenType.ImageStart, Value = justPeek ? lookahead : ConsumeChar(IMG_OPEN.Length) };
            }
            else if (lookahead == IMG_CLOSE && !lastIsEscape)
            {                
                return new Token { Type = TokenType.ImageEnd, Value = justPeek ? lookahead : ConsumeChar(IMG_CLOSE.Length) };
            }
            return null;
        }

        private Token CheckStrikethrough(bool justPeek = false)
        {
            string lookahead = PeekChar(STRIKETHROUGH.Length);
            if (lookahead == STRIKETHROUGH)
            {                
                return new Token { Type = TokenType.Strikethrough, Value = justPeek ? lookahead : ConsumeChar(STRIKETHROUGH.Length) };
            }
            return null;
        }

        private Token CheckBlockquote(bool justPeek = false)
        {
            if (output.Count > 0 && output.Last().Type != TokenType.NewLine)
                return null;
            string lookahead = PeekChar();
            if (lookahead != BLOCKQUOTE)
                return null;
            int q = 1;
            string tmp = null;
            lookahead = "";
            do
            {
                if (IsEndOfInput(q))
                    break;
                tmp = PeekChar(q);
                if (tmp[q-1] == '>' || tmp[q-1] == ' ')
                {
                    q++;
                    lookahead = tmp;
                    continue;
                }
                break;
            } while (true);

            if (!lookahead.EndsWith(" "))
                return null;

            return new Token { 
                Type = TokenType.Blockquote,
                Value = justPeek ? lookahead : ConsumeChar(q-1) 
            };
        }

        private bool LastIs(TokenType type)
        {
            var last = output.LastOrDefault();
            return last != null && last.Type == type;
        }

        private Token CheckEscapeBlock(bool justPeek = false)
        {
            string lookahead = PeekChar(2);
            if (lookahead == ESCAPE_BLOCK && !LastIs(TokenType.Escape))
            {                
                return new Token { Type = TokenType.EscapeBlock, Value = justPeek ? lookahead : ConsumeChar(2) };
            }
            return null;
        }

        private Token CheckEscape(bool justPeek = false)
        {
            string lookahead = PeekChar();
            if (lookahead == ESCAPE)
            {                
                return new Token { Type = TokenType.Escape, Value = justPeek ? lookahead : ConsumeChar() };
            }
            return null;
        }

        private Token CheckCodeBlock(bool justPeek = false)
        {
            if (!IsValidBlockStart())
                return null;
                
            string lookahead = PeekChar(CODEBLOCK.Length);
            if (lookahead == CODEBLOCK)
            {
                string str = PeekChar(CODEBLOCK.Length+1);
                if (str.Length == CODEBLOCK.Length+1 && str.Last() == CODEBLOCK[0])
                {
                    return null;
                }
                if (!justPeek)
                {
                    if (state.First() == TokenizerState.Code)
                    {
                        state.Pop();
                    }
                    else
                    {
                        state.Push(TokenizerState.Code);
                    }
                }
                return new Token { Type = TokenType.CodeBlock, Value = justPeek ? lookahead : ConsumeChar(CODEBLOCK.Length) };
            }
            return null;
        }
        
        private Token CheckDmlCodeBlock(bool justPeek = false)
        {
            if (!IsValidBlockStart())
                return null;
                
            string lookahead = PeekChar(DML_CODEBLOCK.Length);
            if (lookahead == DML_CODEBLOCK)
            {
                string str = PeekChar(DML_CODEBLOCK.Length+1);
                if (str.Length == DML_CODEBLOCK.Length+1 && str.Last() == DML_CODEBLOCK[0])
                {
                    return null;
                }

                if (!justPeek)
                {
                    state.Push(TokenizerState.Code);
                }
                return new Token { Type = TokenType.CodeBlock, Value = justPeek ? lookahead : ConsumeChar(DML_CODEBLOCK.Length) };
            }
            return null;
        }

        private Token CheckCodeBlockLang(bool justPeek = false)
        {
            string lookahead = PeekChar();
            Token last = output.LastOrDefault();
            if (last == null || last.Type != TokenType.CodeBlock || lookahead == null || lookahead == "\n")
                return null;
            
            int q = 1;
            string tmp = null;
            lookahead = "";
            while ((tmp = PeekChar(q)) != lookahead && !tmp.EndsWith("\n"))
            {
                lookahead = tmp;
                q++;
            }

            if (!justPeek)
            {
                ConsumeChar(q-1);
            }

            return new Token () { Type = TokenType.CodeBlockLang, Value = lookahead };
        }

        private bool IsValidBlockStart()
        {
            Token tkn = output.LastOrDefault();
            if (tkn == null || tkn.Type == TokenType.NewLine || tkn.Type == TokenType.Blockquote)
                return true;

            int index = output.Count - 1;
            while (index >= 0)
            {
                tkn = output[index--];

                // Skip Indentation, Escape and Empty strings
                if (tkn.Type == TokenType.Indentation || tkn.Type == TokenType.Escape || (tkn.Type == TokenType.Text && tkn.Value.Trim() == string.Empty))
                    continue;
                
                // Break to check for valid start blocks
                break;
            }
            return tkn == null || tkn.Type == TokenType.NewLine || tkn.Type == TokenType.Blockquote;
        }
    }
}
