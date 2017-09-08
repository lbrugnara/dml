// Copyright (c) Leonardo Brugnara
// Full copyright and license information in LICENSE file

using System.Collections.Generic;
using System.Linq;
using DmlLib.Core.Nodes;

namespace DmlLib.Core
{
    public class Parser
    {
        private Lexer lexer;
        private delegate DmlElement ElementParser(ParsingContext ctx);

        public Lexer Lexer
        {
            get
            {
                return lexer;
            }
        }

        public DmlDocument Parse(string doc, ParsingContext ctx = null)
        {
            lexer = new Lexer(doc);
            return ParseDocument(ctx ?? new ParsingContext());
        }

        public DmlDocument Parse(List<Token> tokens, ParsingContext ctx = null)
        {
            lexer = new Lexer(tokens);
            return ParseDocument(ctx ?? new ParsingContext());
        }

        private DmlDocument ParseDocument(ParsingContext ctx)
        {
            DmlDocument doc = new DmlDocument();
            ConsumeWhiteSpaces();
            Token token;
            while ((token = lexer.PeekToken()) != null)
            {
                if (token.Type == TokenType.NewLine)
                {
                    // If next token is a NewLine token, check for another NewLine after it
                    Token firstnl = lexer.NextToken();
                    token = lexer.PeekToken();
                    // If the second token is null or different from NL, set the consumed NewLine to `token`
                    // again to process it. If next token is a NL, do not do anything, we already removed
                    // the first NL, continue in the blockquote 
                    if (token == null || token.Type != TokenType.NewLine)
                    {
                        // Remove whitespaces following the new line
                        ConsumeWhiteSpaces();
                        token = firstnl;
                        lexer.RestoreToken(token);
                    }
                }
                else if (token != null && token.IsNotRepresentable)
                {
                    lexer.NextToken();
                    continue;
                }
                ProcessChildElement(doc.Body, GetBlockNodeParser(ctx, token), ctx);
            }
            return doc;
        }

        private ElementParser GetBlockNodeParser(ParsingContext ctx, Token t)
        {
            switch (t.Type)
            {
                case TokenType.NewLine:
                    return ParseText;
                case TokenType.Blockquote:
                    return ParseBlockquote;
                case TokenType.HeaderStart:
                    return ParseHeader;
                case TokenType.ListItem:
                    return ParseList;
                case TokenType.Indentation:
                case TokenType.Preformatted:
                    return ParsePreformatted;
                case TokenType.ThematicBreak:
                    return ParseThematicBreak;
                case TokenType.CodeBlock:
                {
                    Token tkn = lexer.PeekToken(1);
                    if (tkn != null && tkn.Type == TokenType.CodeBlockLang && tkn.Value == "dml-source")
                    {
                        return ParseDmlSource;
                    }
                    return ParseCodeBlock;
                }
            }
            if (ctx.BlockquoteLevel > 0)
                return ParseBlockquoteParagraph;
            return ParseParagraph;
        }

        /// <summary>
        /// Returns an specific parser based on the token type
        /// </summary>
        /// <param name="t">Token to check the type to return the specific parser</param>
        /// <returns>Pointer to a method that can parse the specific node</returns>
        private ElementParser GetInlineNodeParser(ParsingContext ctx, Token t)
        {
            if (!ctx.IsMarkupProcessingEnabled())
            {
                /*if (t.Type == TokenType.Escape)
                {
                    Token nextToEscape = lexer.PeekToken(1);
                    if (nextToEscape != null && nextToEscape.Type != TokenType.Text)
                    {
                        lexer.NextToken();
                    }
                }*/
                return ParseText;
            }
            switch(t.Type)
            {
                case TokenType.Text:
                case TokenType.NewLine:
                    return ParseText;
                case TokenType.BoldOpen:
                    return ParseBold;
                case TokenType.Italic:
                    return ParseItalic;
                case TokenType.InlineCode:
                    return ParseInlineCode;
                case TokenType.Underlined:
                    return ParseUnderlined;
                case TokenType.Strikethrough:
                    return ParseStrikethrough;
                case TokenType.Blockquote:
                    return ParseBlockquote;
                case TokenType.Preformatted:
                    return ParsePreformatted;
                case TokenType.LinkStart:
                    return ParseLink;
                case TokenType.ImageStart:
                    return ParseImage;
                case TokenType.EscapeBlock:
                    return ParseEscapeBlock;
                case TokenType.ReferenceStart:
                    return ParseReference;
                case TokenType.Escape:
                    return ParseEscape;
                case TokenType.ListItem:
                    return ParseList;
            }
            return ParseText;
        }

        private DmlElement ParseEscape(ParsingContext ctx)
        {
            lexer.NextToken();
            Token nextToEscape = lexer.PeekToken();
            if (nextToEscape != null && nextToEscape.Type != TokenType.Text)
            {
                return ParseText(ctx);
            }
            return new TextNode("\\");
        }

        private DmlElement ParseThematicBreak(ParsingContext ctx)
        {
            lexer.NextToken();            
            return new ThematicBreakNode();
        }

        private void ConsumeWhiteSpaces()
        {
            Token token;            
            while ((token = lexer.PeekToken()) !=  null)
            {
                if (token.Type != TokenType.Text || token.Value.Trim() != string.Empty)
                    break;
                lexer.NextToken();
            }
        }

        private int GetNextBlockquoteLevel()
        {
            int level = 0;
            Stack<Token> consumed = new Stack<Token>();
            Token token;
            while ((token = lexer.PeekToken()) !=  null)
            {
                if (token.Type != TokenType.Blockquote && (token.Type != TokenType.Text || token.Value.Trim() != string.Empty))
                    break;
                if (token.Type == TokenType.Blockquote)
                    level++;
                consumed.Push(lexer.NextToken());
            }
            while (consumed.Count > 0 && (token = consumed.Pop()) != null)
            {
                lexer.RestoreToken(token);
            }
            return level;
        }

        private void TryConsumeBlockquoteTokens(int c)
        {
            Token token;
            while (c > 0 && (token = lexer.PeekToken()) != null)
            {
                if (token.Type != TokenType.Blockquote)
                    break;
                if (token.Type == TokenType.Blockquote)
                    c--;
                lexer.NextToken();
            }
        }

        private DmlElement ParseBlockquote(ParsingContext ctx)
        {
            // Consume >
            Token bq = lexer.NextToken();
            int increment = bq.Value.Where(c => c != ' ').Select(c => c).Count() - ctx.BlockquoteLevel;
            ctx.IncrementBlockquoteLevel(increment);

            BlockquoteNode quote = new BlockquoteNode();
            quote.Properties["index"] = ctx.BlockquoteLevel;
            DmlElement childElement = new ParagraphNode();
            Token token;

            while ((token = lexer.PeekToken()) != null)
            {
                if (token.Type == TokenType.BlockquoteEndMarker)
                {
                    lexer.NextToken();
                    break;
                }
                else if (token.Type == TokenType.NewLine)
                {
                    Token nl = lexer.NextToken();
                    token = lexer.PeekToken();
                    if (token == null)
                    {
                        break;
                    }
                    if (token.Value != null && !token.Value.StartsWith(" "))
                    {
                        Token prev = lexer.Output.Count > 1 ? lexer.Output.ElementAtOrDefault(lexer.Output.Count - 2) : null;
                        if (prev == null || prev.Value == null || !prev.Value.EndsWith(" "))
                            childElement.AddChild(new TextNode(" "));
                    }
                    if (token.Type == TokenType.NewLine)
                    {
                        lexer.NextToken();
                        if (childElement.HasChildren())
                        {
                            quote.AddChild(childElement);
                        }
                        quote.AddChild(new TextNode("\n"));
                        childElement = new ParagraphNode();
                        continue;
                    }
                    continue;
                }

                // Find a parser method for the token type and get the generated HtmlElement
                var parser = GetBlockNodeParser(ctx, token);
                // Block elements goes at <blockquote> level, rest of elements in a paragraph
                if (parser == ParseBlockquote 
                    || parser == ParseList 
                    || parser == ParseHeader 
                    || parser == ParsePreformatted 
                    || parser == ParseCodeBlock
                    || parser == ParseDmlSource)
                {
                    if (childElement.HasChildren())
                    {
                        quote.AddChild(childElement);
                        quote.AddChild(new TextNode("\n"));
                        childElement = new ParagraphNode();
                    }
                    ProcessChildElement(quote, parser, ctx);
                }
                else
                {
                    ProcessChildElement(childElement, parser, ctx);
                }
            }
            if (childElement.HasChildren())
            {
                quote.AddChild(childElement);
                quote.AddChild(new TextNode("\n"));
            }

            ctx.DecrementBlockquoteLevel(increment);
            return quote;
        }

        private DmlElement ParseHeader(ParsingContext ctx)
        {
            Token t = lexer.NextToken();
            HeaderType headerType = HeaderType.H1;
            switch (t.Value[0])
            {
                case '~':
                    headerType = HeaderType.H2;
                    break;
                case '-':
                    headerType = HeaderType.H3;
                    break;
                case '`':
                    headerType = HeaderType.H4;
                    break;
            }
            HeaderNode header = new HeaderNode(headerType);

            Token et = null;
            while ((et = lexer.PeekToken()) != null && et.Type != TokenType.HeaderEnd)
            {
                if (et.Type == TokenType.NewLine && lexer.PeekToken(1).Type == TokenType.HeaderEnd)
                {
                    lexer.NextToken();
                    continue;
                }
                ProcessChildElement(header, GetInlineNodeParser(ctx, et), ctx);
            }
            lexer.NextToken(); // Consume HeaderEnd token
            return header;
        }

        private int GetNextListLevel()
        {
            int levels = 0;
            Token tmp;
            while ((tmp = lexer.PeekToken(levels)) != null && tmp.Type == TokenType.Indentation)
            {
                levels++;
            }
            return levels;
        }

        private DmlElement ParseList(ParsingContext ctx)
        {
            Token tkn = lexer.PeekToken();
            
            ListType listType = ListType.Unordered;
            if (tkn.Value == "# ")
            {
                listType = ListType.Ordered;
            }
            else if (tkn.Value.StartsWith("["))
            {
                listType = ListType.Todo;
            }

            GroupNode group = new GroupNode();
            ListNode list = new ListNode(listType);
            list.Properties["level"] = ctx.ListLevel;
            int childindex = 1;

            int? lastIndex = null;
            if (listType == ListType.Ordered && tkn.OriginalValue != null)
            {
                //lastIndex = int.Parse(tkn.OriginalValue.Substring(0, tkn.OriginalValue.Length-2));
                //childindex = lastIndex.Value;
                list.Attributes["start"] = tkn.OriginalValue.Substring(0, tkn.OriginalValue.Length-2);
                childindex = int.Parse(list.Attributes["start"]);
            }

            Token t = null;
            while ((t = lexer.PeekToken()) != null)
            {
                if (t.Type == TokenType.ListItem)
                {
                    int? curIndex = null;
                    if (t.Value == "# " && t.OriginalValue != null)
                    {
                        curIndex = int.Parse(t.OriginalValue.Substring(0, t.OriginalValue.Length-2));
                    }
                    bool breakNumberedList = (lastIndex.HasValue && (!curIndex.HasValue || curIndex <= lastIndex || curIndex > lastIndex+1));
                    if (t.Value[0] != tkn.Value[0] || breakNumberedList)
                    {                        
                        group.AddChild(list);
                        list = new ListNode(listType);
                        list.Properties["level"] = ctx.ListLevel;
                        if (curIndex.HasValue)
                        {
                            childindex = curIndex.Value;
                            list.Attributes["start"] = curIndex.Value.ToString();
                        }
                    }
                    lastIndex = curIndex;
                    lexer.NextToken();
                    DmlElement li = null;
                    if (t.Value[0] == '[')
                    {
                        li = new TodoListItemNode(t.Value.ToLower()[1] == 'x');
                    }
                    else
                    {
                        li = new ListItemNode();
                        li.Properties["index"] = childindex++;
                    }
                    list.AddChild(li);
                    Token nt = lexer.PeekToken();
                    if (nt == null)
                        break;
                    ProcessChildElement(li, GetInlineNodeParser(ctx, nt), ctx);
                }
                else if (t.Type == TokenType.NewLine)
                {
                    Token tmp = lexer.NextToken();
                    t = lexer.PeekToken();
                    
                    if (t == null || t.Type == TokenType.NewLine)
                    {
                        lexer.RestoreToken(tmp);
                        break;
                    }
                    else if (t.Type == TokenType.ListItem)
                    {
                        if (t.Value[0] != tkn.Value[0])
                        {
                            if (ctx.ListLevel > 0)
                            {
                                // Handle the NewLine in the previous call
                                lexer.RestoreToken(tmp);
                            }
                            break;
                        }
                        if (ctx.ListLevel > 0)
                        {
                            // Handle the NewLine in the previous call
                            lexer.RestoreToken(tmp);
                            break;
                        }
                    }
                    else
                    {
                        // Add an space between works in line continuations
                        var li = list.LastChild();
                        if (li == null) break;
                        li.AddChild(new TextNode(" "));
                    }
                }
                else if (t.Type == TokenType.Indentation)
                {
                    int curLevel = ctx.ListLevel;
                    int nextLevel = GetNextListLevel();
                    if (nextLevel < curLevel)
                    {
                        break;
                    }
                    else if (nextLevel > curLevel)
                    {
                        lexer.ConsumeTokens(nextLevel);
                        ParsingContext ctx2 = new ParsingContext(ctx);
                        ctx2.IncrementListLevel(nextLevel - curLevel);
                        DmlElement item = ParseList(ctx2);
                        var li = list.LastChild();
                        if (li == null)
                        {
                            continue;
                        }
                        li.AddChild(item);
                    }
                    else
                    {
                        lexer.ConsumeTokens(nextLevel);
                    }
                }
                else
                {
                    var li = list.LastChild();
                    if (li == null)
                    {
                        break;
                    }
                    ProcessChildElement(li, GetInlineNodeParser(ctx, t), ctx);
                }
            }
            group.AddChild(list);
            return group;
        }
        private DmlElement ParseParagraph(ParsingContext ctx)
        {
            ParagraphNode paragraph = new ParagraphNode();
            ConsumeWhiteSpaces();
            Token token;
            while ((token = lexer.PeekToken()) != null)
            {
                if (token.Type == TokenType.Blockquote)
                {
                    break;
                }
                // We need to handle new lines in paragraph. If token is TokenType.NewLine,
                // child is a TextNode with a new line character on it.
                else if (token.Type == TokenType.NewLine)
                {
                    // If next token is a NewLine token, check for another NewLine after it
                    Token firstnl = lexer.NextToken();
                    token = lexer.PeekToken();
                    // If the second token is null, set the consumed NewLine to `token` again to process
                    // it and in the next iteration, break the execution of this method.
                    if (token == null || token.Type != TokenType.NewLine)
                    {
                        if (token != null && token.Value != null)
                        {
                            // Take nth-2 because nth-1 is the consumed NL
                            Token prev = lexer.Output.Count > 1 ? lexer.Output.ElementAtOrDefault(lexer.Output.Count - 2) : null;
                            if (prev == null || prev.Value == null)
                                continue;
                            if (prev.Value.Trim().EndsWith("."))
                            {
                                paragraph.AddChild(new LineBreakNode());
                            }
                            else if (!token.Value.StartsWith(" ") && !prev.Value.EndsWith(" "))
                            {
                                paragraph.AddChild(new TextNode(" "));
                            }
                        }
                        continue;
                    }
                    else if (token.Type == TokenType.NewLine)
                    {
                        break;
                    }
                }

                // Find a parser method for the token type and get the generated HtmlElement
                ProcessChildElement(paragraph, GetInlineNodeParser(ctx, token), ctx);
            }
            return paragraph;
        }

        /// <summary>
        /// Replaces ParseParagraph method when the current Block element is a Blockquote. Instead of using a ParagraphNode,
        /// this method uses a GroupNode, letting ParseBlockquote to decide when to close the current Paragraph based on the
        /// NewLines, to keep the 2-NL rule on paragraphs
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        private DmlElement ParseBlockquoteParagraph(ParsingContext ctx)
        {
            GroupNode paragraph = new GroupNode();
            Token token;
            while ((token = lexer.PeekToken()) != null)
            {
                // We need to handle new lines in paragraph. If token is TokenType.NewLine,
                // child is a TextNode with a new line character on it.
                if (token.Type == TokenType.NewLine || token.Type == TokenType.BlockquoteEndMarker)
                {
                    break;
                }
                // Find a parser method for the token type and get the generated HtmlElement
                ProcessChildElement(paragraph, GetInlineNodeParser(ctx, token), ctx);
            }
            return paragraph;
        }

        private DmlElement ParsePreformatted(ParsingContext ctx)
        {
            lexer.NextToken();
            bool tmp = ctx.IsMarkupProcessingEnabled();
            ctx.MarkupProcessingEnabled(false);
            PreformattedNode pre = new PreformattedNode();
            CodeNode code = new CodeNode(true);
            Token token;
            while ((token = lexer.PeekToken()) != null)
            {
                if (token.Type == TokenType.NewLine)
                {
                    Token nl = lexer.NextToken();
                    token = lexer.PeekToken();
                    if (token == null || token.Type == TokenType.NewLine)
                    {
                        lexer.RestoreToken(nl);
                        break;
                    }
                    else if (token != null && token.Type == TokenType.Indentation)
                    {
                        lexer.NextToken();
                    }
                    code.AddChild(new TextNode(nl.Value));
                    continue;
                }
                else if (token != null && token.Type == TokenType.Preformatted)
                {
                    lexer.NextToken();
                    continue;
                }
                code.AddChild(ParseText(ctx));
                //ProcessChildElement(code, GetInlineNodeParser(ctx, token), ctx);
            }
            ctx.MarkupProcessingEnabled(tmp);
            pre.AddChild(code);
            return pre;
        }

        private DmlElement ParseCodeBlock(ParsingContext ctx)
        {
            Token codeBlockTkn = lexer.NextToken();
            bool tmp = ctx.IsMarkupProcessingEnabled();
            ctx.MarkupProcessingEnabled(codeBlockTkn.Value.StartsWith("!"));
            PreformattedNode pre = new PreformattedNode();
            CodeNode code = new CodeNode(true);
            
            Token token = lexer.NextToken(); // Consume the NL or the CodeBlockLang

            if (token == null)
            {
                return code;
            }

            if (token.Type == TokenType.CodeBlockLang)
            {
                    lexer.NextToken(); // NL
                    code.Attributes["class"] = token.Value;
            }

            while ((token = lexer.PeekToken()) != null)
            {
                if (token.Type == TokenType.CodeBlock)
                {
                    lexer.NextToken();
                    break;
                }
                else if (token.Type == TokenType.Escape)
                {
                    Token nextToEscape = lexer.PeekToken(1);
                    if (nextToEscape != null && nextToEscape.Value.StartsWith(Tokenizer.CODEBLOCK))
                    {
                        lexer.NextToken();
                        token = nextToEscape;
                    }
                }
                else if (token.Type == TokenType.Indentation)
                {
                    lexer.NextToken();
                    code.AddChild(new TextNode(token.Value));
                    continue;
                }
                else if (token.Type == TokenType.Preformatted)
                {
                    lexer.NextToken();
                    continue;
                }
                // Find a parser method for the token type and get the generated HtmlElement
                //code.AddChild(ParseText(ctx));
                ProcessChildElement(code, GetInlineNodeParser(ctx, token), ctx);
            }
            ctx.MarkupProcessingEnabled(tmp);
            pre.AddChild(code);
            return pre;
        }

        private DmlElement ParseDmlSource(ParsingContext ctx)
        {
            lexer.NextToken(); // Consume CodeBlock
            lexer.NextToken(); // Consume CodeBlockLang
            lexer.NextToken(); // Consume NewLine
            List<Token> source = new List<Token>();
            Token token;
            while ((token = lexer.PeekToken()) != null)
            {
                lexer.NextToken();
                if (token.Type == TokenType.CodeBlock)
                {
                    break;
                }
                else if (token.Type == TokenType.Escape)
                {
                    Token nextToEscape = lexer.PeekToken();
                    if (nextToEscape != null && (nextToEscape.Value.StartsWith(Tokenizer.CODEBLOCK) || nextToEscape.Value.StartsWith("!"+Tokenizer.CODEBLOCK)))
                    {
                        lexer.NextToken();
                        token = nextToEscape;
                    }
                }
                source.Add(token);
            }

            GroupNode outgroup = new GroupNode();
            // Source
            CodeNode code = new CodeNode(true);
            int bqlevel = 0;
            bool lastNl = false;
            foreach (Token src in source)
            {
                if (src.Type == TokenType.HeaderStart)
                {
                    if (lastNl)
                    {
                        code.AddChild(new TextNode(" ".PadLeft(bqlevel+1, '>')));
                        lastNl = false;
                    }
                    code.AddChild(new TextNode(""));
                }
                else if (src.Type == TokenType.Blockquote)
                {
                    lastNl = false;
                    bqlevel++;
                    code.AddChild(new TextNode(src.OriginalValue ?? src.Value));
                }
                else if (src.Type == TokenType.BlockquoteEndMarker)
                {
                    bqlevel--;
                    
                }
                else if (src.Type == TokenType.NewLine && bqlevel > 0)
                {
                    lastNl = true;
                    code.AddChild(new TextNode((src.OriginalValue ?? src.Value)));
                }
                else
                {
                    if (lastNl)
                    {
                        code.AddChild(new TextNode(" ".PadLeft(bqlevel+1, '>')));
                        lastNl = false;
                    }
                    code.AddChild(new TextNode((src.OriginalValue ?? src.Value).Replace("<", "&lt;")));
                }
            }
            outgroup.AddChild(code);

            // Output
            Parser parser = new Parser();
            DmlDocument doc = parser.Parse(source);
            outgroup.MergeChildren(doc.Body);
            ThematicBreakNode hr = new ThematicBreakNode();
            hr.Attributes["class"] = "short";
            outgroup.AddChild(hr);

            return outgroup;
        }

        private DmlElement ParseText(ParsingContext ctx)
        {
            bool lastIsEscapeSeq = lexer.Output.Count > 0 && lexer.Output.Last().Type == TokenType.Escape;
            Token t = lexer.NextToken();
            string val = t.Value;
            if (!ctx.IsMarkupProcessingEnabled() || (lastIsEscapeSeq && t.Type == TokenType.Lt))
            {
                val = (val ?? "").Replace("<", "&lt;");
            }
            return new TextNode(val);
        }

        private DmlElement ParseBold(ParsingContext ctx)
        {
            var result = ParseInline(new StrongNode(), TokenType.BoldClose, ctx);
            return result;
        }

        private DmlElement ParseItalic(ParsingContext ctx)
        {
            var result = ParseInline(new ItalicNode(), TokenType.Italic, ctx);
            return result;
        }

        private DmlElement ParseInlineCode(ParsingContext ctx)
        {
            bool markupProcessingMode = ctx.IsMarkupProcessingEnabled();
            ctx.MarkupProcessingEnabled(false);
            var result = ParseInline(new CodeNode(false), TokenType.InlineCode, ctx);
            ctx.MarkupProcessingEnabled(markupProcessingMode);
            return result;
        }

        private DmlElement ParseUnderlined(ParsingContext ctx)
        {
            var node = new UnderlineNode();
            var result = ParseInline(node, TokenType.Underlined, ctx);
            return result;
        }

        private DmlElement ParseStrikethrough(ParsingContext ctx)
        {
            var node = new StrikeNode();
            var result = ParseInline(node, TokenType.Strikethrough, ctx);
            return result;
        }

        private DmlElement ParseInline(DmlElement el, TokenType closeToken, ParsingContext ctx)
        {
            // Consume the starting token to remove the token that triggered this parser
            // (it could be '[', '/', '`', '~', etc)
            Token startToken = lexer.NextToken();

            GroupNode nodes = new GroupNode();
            Token token;
            while ((token = lexer.PeekToken()) != null)
            {
                if (token.Type == TokenType.Escape && !ctx.IsMarkupProcessingEnabled())
                {
                    Token firstnl = lexer.NextToken();
                    token = lexer.PeekToken();
                    if (token != null && (token.Type == closeToken || token.Type == TokenType.Escape))
                    {
                        lexer.NextToken();
                        nodes.AddChild(new TextNode(token.Value));
                        continue;
                    }
                    else
                    {
                        lexer.RestoreToken(firstnl);
                    }
                }
                else if (token.Type == closeToken)
                {
                    // If next token is the closing token, consume it and return the
                    // provided HtmlElement with the parsed children
                    lexer.NextToken();
                    el.MergeChildren(nodes);
                    return el;
                }
                else if (token.Type == TokenType.NewLine)
                {
                    // If next token is a NewLine token, check for another NewLine after it
                    Token firstnl = lexer.NextToken();
                    token = lexer.PeekToken();
                    // If the second token is null, set the consumed NewLine to `token` againt to process
                    // it and in the next iteration, break the execution of this method.
                    if (token == null || token.Type != TokenType.NewLine)
                    {
                        token = firstnl;
                        lexer.RestoreToken(token);
                    }
                    else if (token.Type == TokenType.NewLine)
                    {
                        // If next token is another NewLine, restore the previous consumed NewLine,                        
                        // break and let the caller to resolve this
                        lexer.RestoreToken(firstnl);
                        break;
                    }
                }

                ProcessChildElement(nodes, GetInlineNodeParser(ctx, token), ctx);
            }
            // If we reached this path, there are no more tokens or this is not 
            // a balanced token, resulting in a group of tokens that caller should
            // merge with its children.
            GroupNode group = new GroupNode();
            group.AddChild(new TextNode(startToken.Value));
            group.MergeChildren(nodes);
            return group;
        }

        private DmlElement ParseLink(ParsingContext ctx)
        {
            lexer.NextToken(); // Consume [[
            GroupNode text = new GroupNode();
            string href = "";
            string title = "";

            Token t = null;
            // Text
            while ((t = lexer.PeekToken()) != null)
            {
                if (t.Type == TokenType.Pipe || t.Type == TokenType.LinkEnd)
                {
                    if (t.Type == TokenType.Pipe)
                        lexer.NextToken();
                    break;
                }
                ProcessChildElement(text, GetInlineNodeParser(ctx, t), ctx);
            }

            // Href
            while ((t = lexer.PeekToken()) != null)
            {
                if (t.Type == TokenType.Pipe || t.Type == TokenType.LinkEnd)
                {
                    if (t.Type == TokenType.Pipe)
                        lexer.NextToken();
                    break;
                }
                lexer.NextToken();
                href += t.Value;
            }

            // Title
            while ((t = lexer.PeekToken()) != null)
            {
                if (t.Type == TokenType.LinkEnd)
                {
                    break;
                }
                lexer.NextToken();
                title += t.Value;
            }

            lexer.NextToken(); // Consume ]]

            title.Trim();
            href = href.Trim();
            if (href.StartsWith(":"))
            {
                List<string> titles = title.Split(',').ToList();
                List<string> hrefs = href.Substring(1).Split(',').ToList();
                CustomNode links = new CustomNode("span");
                links.MergeChildren(text);
                for (int i=0; i < hrefs.Count; i++) {
                    ReferenceLinkNode refLink = new ReferenceLinkNode(hrefs.ElementAt(i), titles.ElementAtOrDefault(i));
                    links.AddChild(refLink);
                };
                return links;
            }

            LinkNode a = new LinkNode(href, title);
            a.MergeChildren(text);
            return a;
        }

        private DmlElement ParseImage(ParsingContext ctx)
        {
            lexer.NextToken(); // Consume [{
            string title = "";
            string source = "";
            string altTitle = "";

            Token t = null;
            // title
            while ((t = lexer.PeekToken()) != null)
            {
                if (t.Type == TokenType.Pipe || t.Type == TokenType.ImageEnd)
                {
                    if (t.Type == TokenType.Pipe)
                        lexer.NextToken();
                    break;
                }
                lexer.NextToken();
                title += t.Value;
            }

            // src
            while ((t = lexer.PeekToken()) != null)
            {
                if (t.Type == TokenType.Pipe || t.Type == TokenType.ImageEnd)
                {
                    if (t.Type == TokenType.Pipe)
                        lexer.NextToken();
                    break;
                }
                lexer.NextToken();
                source += t.Value;
            }

            // alt
            while ((t = lexer.PeekToken()) != null)
            {
                if (t.Type == TokenType.ImageEnd)
                {
                    break;
                }
                lexer.NextToken();
                altTitle += t.Value;
            }

            lexer.NextToken(); // Consume }]

            ImageNode img = new ImageNode(title, source, altTitle);
            return img;
        }

        private DmlElement ParseReference(ParsingContext ctx)
        {
            Token start = lexer.NextToken();
            Token token = null;
            List<Token> hrefTokens = new List<Token>();
            while ((token = lexer.PeekToken()) != null && token.Type != TokenType.ReferenceEnd && token.Type != TokenType.NewLine)
            {
                hrefTokens.Add(lexer.NextToken());
            }
            if (token == null || token.Type == TokenType.NewLine)
            {
                hrefTokens.Reverse();
                hrefTokens.ForEach(t => lexer.RestoreToken(t));
                TextNode text = new TextNode(start.Value);
                return text;
            }
            lexer.NextToken(); // Consume ReferenceEnd

            List<Token> titleTokens = new List<Token>();
            while ((token = lexer.PeekToken()) != null && token.Type != TokenType.Pipe && token.Type != TokenType.NewLine)
            {
                titleTokens.Add(lexer.NextToken());
            }
            if (token == null || token.Type == TokenType.NewLine)
            {
                hrefTokens.Reverse();
                hrefTokens.ForEach(t => lexer.RestoreToken(t));
                titleTokens.Reverse();
                titleTokens.ForEach(t => lexer.RestoreToken(t));
                TextNode text = new TextNode(start.Value);
                return text;
            }
            titleTokens.Add(lexer.NextToken()); // Consume Pipe

            string id = string.Join("", hrefTokens.Select(s => s.Value).ToList()).Trim();
            ReferenceNode reference = new ReferenceNode(id);
            
            titleTokens.Reverse();
            titleTokens.ForEach(t => lexer.RestoreToken(t));
            while ((token = lexer.PeekToken()) != null && token.Type != TokenType.Pipe && token.Type != TokenType.NewLine)
            {
                ProcessChildElement(reference, GetInlineNodeParser(ctx, token), ctx);
            }
            lexer.NextToken(); // Consume Pipe
            return reference;
        }

        private DmlElement ParseEscapeBlock(ParsingContext ctx)
        {
            lexer.NextToken();
            Token token = null;
            GroupNode group = new GroupNode();
            bool tmp = ctx.IsMarkupProcessingEnabled();
            ctx.MarkupProcessingEnabled(false);
            while ((token = lexer.PeekToken()) != null && token.Type != TokenType.EscapeBlock)
            {
                ProcessChildElement(group, GetInlineNodeParser(ctx, token), ctx);
            }
            lexer.NextToken();
            ctx.MarkupProcessingEnabled(tmp);
            return group;
        }

        private void ProcessChildElement(DmlElement node, ElementParser nodeParser, ParsingContext ctx)
        {
            DmlElement child = nodeParser.Invoke(ctx);
            if (child == null)
                return;
            if (child is GroupNode)
            {
                node.MergeChildren(child);
            }
            else
            {
                node.AddChild(child);
            }
        }

        public List<Token> TokenizeDocument(string doc)
        {
            Lexer lexer = new Lexer(doc);
            List<Token> tokens = new List<Token>();
            Token tmp;
            while ((tmp = lexer.NextToken()) != null)
            {
                tokens.Add(tmp);
            }
            return tokens;
        }
    }
}
