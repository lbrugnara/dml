// Copyright (c) Leonardo Brugnara
// Full copyright and license information in LICENSE file

using System.Collections.Generic;
using System.Linq;

namespace DmlLib.Core
{
    public class Lexer
    {
        private LinkedList<Token> _tokens;
        private List<Token> _output;

        public Lexer(string src)
        {
            _tokens = new LinkedList<Token>();
            Tokenizer tknzr = new Tokenizer(src);
            List<Token> tokens = tknzr.Tokenize();
            List<Token> output = tokens;
            output = Phase1(output);
            output = Phase2(output);
            output.ForEach(t => _tokens.AddLast(t));
        }

        public Lexer(List<Token> tokens)
        {
            _tokens = new LinkedList<Token>();
            tokens.ForEach(t => _tokens.AddLast(t));
        }

        public List<Token> Tokenize(string src, int phases = 2)
        {
            Tokenizer tknzr = new Tokenizer(src);
            List<Token> tokens = tknzr.Tokenize();
            if (phases == 0)
                return tokens;

            List<Token> output = tokens;
            output = Phase1(output);
            if (phases == 1)
                return output;

            output = Phase2(output);
            return output;
        }

        /// <summary>
        /// Removes unnecesary blockquote tokens leaving just the first blockquote token, adds an end marker for each blockquote and handles blockquote levels
        /// to make easier the parsing of blockquotes and the elements inside it
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private List<Token> NormalizeBlockquote(List<Token> tokens, bool isTopLevel = false)
        {
            Token bq = tokens.First();

            int curLevel = bq.Value.Where(c => c != ' ').Select(c => c).Count();
            if (isTopLevel)
            {
                while (--curLevel > 0)
                {
                    tokens.Insert(0, new Token(){Type = TokenType.NewLine, Value = "\n"});
                    tokens.Insert(0, new Token() {
                        Type = TokenType.Blockquote,
                        Value = " ".PadLeft(curLevel+1,'>')
                    });
                }
                curLevel = 1;
            }

            bq = tokens.First();
            List<Token> output = new List<Token>() { bq };
            while (true)
            {
                Token tkn = tokens.FirstOrDefault();
                if (tkn == null)
                {
                    output.Add( new Token { Type = TokenType.BlockquoteEndMarker, IsNotRepresentable = true });
                    break;
                }
                else if (tkn.Type == TokenType.NewLine)
                {
                    Token nl = tkn;
                    tokens.RemoveAt(0);
                    tkn = tokens.FirstOrDefault();
                    if (tkn == null || tkn.Type != TokenType.NewLine)
                    {
                        output.Add(nl);
                        continue;
                    }
                    tokens.RemoveAt(0);
                    output.Add( new Token {Type = TokenType.BlockquoteEndMarker, IsNotRepresentable = true });
                    output.Add(nl);
                    output.Add(tkn);
                    break;
                }
                else if (tkn.Type == TokenType.Blockquote)
                {
                    int nextLevel = tkn.Value.Where(c => c != ' ').Select(c => c).Count();
                    if (nextLevel > curLevel)
                    {
                        int levelDiff = nextLevel - curLevel;
                        if (levelDiff > 1)
                        {
                            tokens.Insert(0, new Token(){Type = TokenType.NewLine, Value = "\n"});
                            tokens.Insert(0, new Token() {
                                Type = TokenType.Blockquote,
                                Value = " ".PadLeft(curLevel + 2,'>')
                            });
                        }
                        output.AddRange(NormalizeBlockquote(tokens));
                        continue;
                    }
                    else
                    {
                        if (nextLevel == curLevel)
                        {
                            // TODO: Flag it as "Ignored" and ignore it in the parser
                            tokens.RemoveAt(0);
                            continue;
                        }
                        // if nextLevel < topLevel, we need to close current Value.Length
                        output.Add( new Token {Type = TokenType.BlockquoteEndMarker, IsNotRepresentable = true });
                        break;
                    }
                }
                else
                {
                    tokens.RemoveAt(0);
                    output.Add(tkn);
                }
            }
            return output;
        }

        private List<Token> Phase1(List<Token> tokens)
        {
            List<Token> output = new List<Token>();
            while (true)
            {
                Token tkn = tokens.FirstOrDefault();
                if (tkn == null)
                {
                    break;
                }
                if (tkn.Type == TokenType.Blockquote)
                {
                    var nbout = NormalizeBlockquote(tokens, true);
                    output.AddRange(nbout);
                }
                else
                {
                    tokens.RemoveAt(0);
                    output.Add(tkn);
                }
            }

            return output;
        }

        private List<Token> Phase2(List<Token> tokens)
        {
            List<Token> output = new List<Token>();
            int index = 0;
            while (true)
            {
                Token tkn = tokens.ElementAtOrDefault(index++);
                if (tkn == null)
                {
                    break;
                }

                if (TokenType.HeaderStart  == tkn.Type && output.Count > 0)
                {
                    tkn.Type = TokenType.HeaderEnd;
                    // Wrap headers
                    int i = output.Count-1;
                    for (; i >= 0; i--)
                    {
                        if (output[i].Type == TokenType.NewLine)
                        {
                            if (i-1 >= 0)
                            {
                                if (output[i-1].Type == TokenType.NewLine)
                                {
                                    break;
                                }
                            }
                        }
                        else if (output[i].Type == TokenType.Blockquote)
                        {
                            break;
                        }
                    }
                    output.Insert(i+1, new Token() { Type = TokenType.HeaderStart, Value = tkn.Value });
                    //output.RemoveAt(output.Count-1);
                    output.Add(tkn);
                }
                else if (tkn.Type == TokenType.LinkStart || tkn.Type == TokenType.ImageStart)
                {
                    TokenType endType = tkn.Type == TokenType.LinkStart ? TokenType.LinkEnd : TokenType.ImageEnd;
                    Token start = tkn;
                    output.Add(start);
                    Token t = null;
                    Token tmp = tokens.Skip(index).SkipWhile(tok => tok.Type == TokenType.Text && tok.Value.Trim() == string.Empty).FirstOrDefault();
                    if (tmp != null && tmp.Type == endType)
                    {
                        start.Type = TokenType.Text;
                        continue;
                    }
                    while (true)
                    {
                        t = tokens.ElementAtOrDefault(index++);
                        if (t == null)
                        {
                            start.Type = TokenType.Text;
                            break;
                        }
                        output.Add(t);
                        if (t.Type == TokenType.NewLine)
                        {
                            Token prev = output.ElementAtOrDefault(output.Count - 2);
                            if (prev != null && prev.Type == TokenType.NewLine)
                            {
                                start.Type = TokenType.Text;
                                break;
                            }                            
                        }
                        else if (t.Type == endType)
                        {
                            break;
                        }
                    }
                }
                else 
                {
                    if (output.Count > 0 && output.Last().Type == TokenType.ReferenceEnd)
                    {
                        tkn.Value = tkn.Value.TrimStart();
                    }
                    output.Add(tkn);
                }
            }
            return output;
        }

        public List<Token> GetTokens()
        {
            return _tokens.ToList();
        }

        public Token PeekToken(int i = 0)
        {
            return _tokens.ElementAtOrDefault(i);
        }

        public Token NextToken()
        {
            Token t = PeekToken();
            if (t != null)
            {
                _tokens.RemoveFirst();
                Output.Add(t);
            }
            return t;
        }

        public void ConsumeTokens(int q = 1)
        {
            while (q-- > 0)
                NextToken();
        }

        public void RestoreToken(Token t)
        {
            _tokens.AddFirst(t);
            Output.RemoveAt(Output.Count - 1);
        }

        public List<Token> Output
        {
            get
            {
                if (_output == null)
                {
                    _output = new List<Token>();
                }
                return _output;
            }
        }
    }
}