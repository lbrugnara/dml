using DmlLib.Nodes;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DmlLib.Semantic
{
    public class Parser
    {
        private delegate void ElementParser(ParsingContext ctx);

        // Contains all the parser for block elements like headers, paragraphs, list items, etc
        private Dictionary<TokenType, ElementParser> BlockElementParsers;

        // Contains all the parser for inline elements like bold, italic, etc
        private Dictionary<TokenType, ElementParser> InlineElementParsers;

        private List<Token> Tokens { get; set; }

        private int Pointer { get; set; }

        // The ElementParsers manipulate the list while creating new nodes
        private LinkedList<DmlElement> Output { get; set; }

        #region Constructors

        public Parser()
        {
            // Initialize the stack
            this.Output = new LinkedList<DmlElement>();

            // Register the block elements parser
            this.BlockElementParsers = new Dictionary<TokenType, ElementParser>()
            {
                { TokenType.HeaderStart,    this.ParseHeader        },
                { TokenType.Blockquote,     this.ParseBlockquote    },
                { TokenType.CodeBlock,      this.ParseCodeBlock     },
                { TokenType.Preformatted,   this.ParsePreformatted  },
                { TokenType.Indentation,    this.ParsePreformatted  },
                { TokenType.Reference,      this.ParseReference     },
                { TokenType.ListItem,       this.ParseList          },
                { TokenType.ThematicBreak,  this.ParseThematicBreak },
                { TokenType.NewLine,        this.ParseNewLine       }
            };

            // Register the ineline elements parsers
            this.InlineElementParsers = new Dictionary<TokenType, ElementParser>()
            {
                { TokenType.Text,           this.ParseText          },
                { TokenType.EscapeBlock,    this.ParseEscapeBlock   },
                { TokenType.Escape,         this.ParseEscape        },
                { TokenType.LinkStart,      this.ParseLink          },
                { TokenType.ImageStart,     this.ParseImage         },
                { TokenType.BoldOpen,       this.ParseBold          },
                { TokenType.Italic,         this.ParseItalic        },
                { TokenType.Underlined,     this.ParseUnderline     },
                { TokenType.Strikethrough,  this.ParseStrikethrough },
                { TokenType.InlineCode,     this.ParseInlineCode    },
                { TokenType.NewLine,        this.ParseNewLine       },

            };
        }

        #endregion

        #region Public API

        public DmlDocument Parse(string source, ParsingContext ctx = null)
        {
            // Create a new Lexer for this parsing session
            var lexer = new Lexer(source);

            this.Tokens = lexer.Tokenize();
            this.Pointer = 0;

            // Call the root parser
            var document = this.ParseDocument(ctx ?? new ParsingContext());

            this.Tokens = null;

            return document;
        }

        public DmlDocument Parse(List<Token> source, ParsingContext ctx = null)
        {
            this.Tokens = source;
            this.Pointer = 0;

            // Call the root parser
            var document = this.ParseDocument(ctx ?? new ParsingContext());

            this.Tokens = null;

            return document;
        }

        #endregion

        #region Private helpers

        private bool HasTokens => this.Pointer < this.Tokens.Count;

        private Token PeekToken(int offset = 0) => this.Tokens.ElementAtOrDefault(this.Pointer + offset);

        private Token ConsumeToken() => this.Tokens.ElementAtOrDefault(this.Pointer++);

        private DmlElement PopElement()
        {
            var last = this.Output.Last();

            this.Output.RemoveLast();

            return last;
        }

        private LinkedListNode<DmlElement> AddAfterElement(LinkedListNode<DmlElement> lastNode, DmlElement element)
        {
            if (lastNode == null)
                this.Output.AddFirst(element);
            else
                this.Output.AddAfter(lastNode, element);

            return this.Output.FindLast(element);
        }

        private ElementParser GetBlockParser(ParsingContext ctx, Token token)
        {
            return this.BlockElementParsers.ContainsKey(token.Type) ? this.BlockElementParsers[token.Type] : this.ParseParagraph;
        }

        private ElementParser GetInlineParser(ParsingContext ctx, Token token, bool escape = true)
        {
            if (escape && token.Type == TokenType.Escape)
                return this.ParseEscape;

            return ctx.MarkupProcessingEnabled && this.InlineElementParsers.ContainsKey(token.Type) ? this.InlineElementParsers[token.Type] : this.ParseText;
        }

        #endregion

        #region Block parsers

        /// <summary>
        /// Creates a new DmlDocument using the current input 
        /// held by the Lexer property
        /// Caller must ensure Lexer is instantiated
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        private DmlDocument ParseDocument(ParsingContext ctx)
        {
            // Get the block element parser and invoke the method
            // with the provided ParsingContext
            while (this.HasTokens)
            {
                if (this.PeekToken().Type == TokenType.DoubleNewLine)
                {
                    this.ConsumeToken();
                    continue;
                }

                this.GetBlockParser(ctx, this.PeekToken())(ctx);
            }

            DmlDocument doc = new DmlDocument();

            // Consume all the items that remain in the NodeStack
            // all of them are children of the DmlDocument object
            while (this.Output.Count > 0)
                doc.Body.InsertChild(0, this.PopElement());

            return doc;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ctx"></param>
        private void ParseParagraph(ParsingContext ctx)
        {
            while (this.HasTokens)
            {

                // Break on 2NL
                if (this.PeekToken().Type == TokenType.DoubleNewLine)
                {
                    this.ConsumeToken();
                    break;
                }

                var token = this.PeekToken();

                // If it is not an inline element we need to check
                // if we can process it with ParseText or just break the
                // loop.
                // If the token type cannot be parsed by a block parser, we
                // just add it as plain text, if not, we break the loop.
                if (!this.InlineElementParsers.ContainsKey(token.Type))
                {
                    if (!this.BlockElementParsers.ContainsKey(token.Type))
                    {
                        this.ParseText(ctx);
                        continue;
                    }
                    break;
                }

                this.InlineElementParsers[token.Type].Invoke(ctx);

                // If the token value ends with a dot and the next token we have to process is a NL
                // we need to add a line break to honor the grammatical paragraph
                if (this.Output.Last.Value.InnerText.Trim()?.EndsWith(".") == true && this.PeekToken()?.Type == TokenType.NewLine)
                    this.Output.AddLast(new LineBreakNode());
            }

            // Create new paragraph, add childs and add it to
            // the temporal output
            ParagraphNode paragraph = new ParagraphNode();

            // Because paragraphs can be divided by NewLines we need
            // to run nested loops to process them
            while (this.Output.Any())
            {
                // If next is a block element, we don't need to add a paragraph
                if (this.Output.Last().ElementType.IsBlockElement())
                    break;

                // Add a child to the current paragrapg
                paragraph.InsertChild(0, this.PopElement());
            }

            this.Output.AddLast(paragraph);
        }

        private int GetBlockquoteLevel(Token tkn)
        {
            if (tkn?.Type != TokenType.Blockquote)
                return -1;

            return tkn.Value?.Length ?? -1;
        }

        private void ConsumeWhiteSpaces()
        {
            while (this.HasTokens)
            {
                var token = this.PeekToken();

                if (token.Type != TokenType.Text || token.Value.Trim() != string.Empty)
                    break;

                this.ConsumeToken();
            }
        }

        private void ParseBlockquote(ParsingContext ctx)
        {
            // Our ParseBlockquote needs to know the previos blockquote level and the target
            // level to work as expected
            this.ParseBlockquote(ctx, this.GetBlockquoteLevel(this.PeekToken()), 0);
        }

        private void ParseBlockquote(ParsingContext ctx, int targetLevel, int previousLevel)
        {
            BlockquoteNode bq = new BlockquoteNode();

            // We can add the blockquote here, because of how the parsing method
            // is designed, we will not modify Output inside ParseBlockquote except
            // by current blockquote (this very line)
            this.Output.AddLast(bq);

            // While the target level is not the immediate next
            // level, resolve the next level first
            if (targetLevel > previousLevel + 1)
            {
                // This will parse the next level recursively until reach
                // previousLevel == targetLevel -1
                this.ParseBlockquote(ctx, targetLevel, previousLevel + 1);

                // Populate the current blockquote with the parsed child
                bq.Children.Add(this.PopElement());
            }

            // If next token is not a blockquote, it means this blockquote
            // is finished, not need to parse anything else
            if (this.PeekToken()?.Type != TokenType.Blockquote)
                return;

            // Next token is a Blockquote, but we need to compute the current
            // level before continue
            var currentLevel = this.GetBlockquoteLevel(this.PeekToken());

            // If the current level is less or equals than previous level
            // we don't need to make anything else here
            if (currentLevel <= previousLevel)
                return;

            // Here we start to parse the current block quote
            // Consume the blockquote token and clean the whitespaces
            this.ConsumeToken();
            this.ConsumeWhiteSpaces();

            // We compute the source of the blockquote, taking
            // the Token.OriginalValue or Token.Value, removing the
            // Blockquote tokens with the same nesting level, and
            // processing the children blockquote, that way we give
            // support to blockquotes to contain any type of markup
            // element
            StringBuilder blockquoteSourceCode = new StringBuilder();

            // We will need a Parser
            Parser parser = new Parser();            

            while (this.HasTokens)
            {
                var token = this.PeekToken();

                // If we find a 2NL, we break the blockquote
                if (token.Type == TokenType.DoubleNewLine)
                    break;

                // When the token is not a blockquote, we just need
                // to append the token's value to the StringBuilder
                if (token.Type != TokenType.Blockquote)
                {
                    token = this.ConsumeToken();
                    blockquoteSourceCode.Append(token.OriginalValue ?? token.Value);
                    continue;
                }

                // If it is a blockquote, we need to get the nesting level
                // of the blockquote
                int newLevel = this.GetBlockquoteLevel(token);

                // If the next level is lesser than the current one (its parent)
                // it means we need to close the current blockquote
                if (newLevel < currentLevel)
                    break;

                // If the levels are equals, we just ignore
                // the Blockquote token and consume the whitespace
                // between the Blockquote token and the next one
                if (newLevel == currentLevel)
                {
                    this.ConsumeToken();
                    this.ConsumeWhiteSpaces();
                    continue;
                }

                // Finally, if the next level is greater than the current one,
                // it means we found a child blockquote, we need to parse all the source
                // we found until this moment, add the processed nodes to the current
                // blockquote, process the child blockquote, and finally add it to the
                // current one
                if (blockquoteSourceCode.Length > 0)
                    parser.Parse(blockquoteSourceCode.ToString()).Body.Children.ForEach(c => bq.AddChild(c));

                // Clear the SB as we already parsed the content
                blockquoteSourceCode.Clear();

                // Process the child BQ
                this.ParseBlockquote(ctx, newLevel, currentLevel);

                // Add the child BQ to the current one
                bq.Children.Add(this.PopElement());
            }

            // If there is source code available, parse the remaining source andd
            // add the children to the current blockquote
            if (blockquoteSourceCode.Length > 0)
                parser.Parse(blockquoteSourceCode.ToString()).Body.Children.ForEach(c => bq.AddChild(c));
        }

        private void ParseThematicBreak(ParsingContext ctx)
        {
            this.ConsumeToken();
            this.Output.AddLast(new ThematicBreakNode());

            if (!this.HasTokens)
                return;

            // If next is a 2NL let caller handle it
            if (this.PeekToken().Type == TokenType.DoubleNewLine)
                return;

            // If just one new line is used, consume it
            if (this.PeekToken()?.Type == TokenType.NewLine)
                this.ConsumeToken();
        }

        private void ParseHeader(ParsingContext ctx)
        {
            Token token = this.ConsumeToken();

            HeaderType headerType = HeaderType.H1;

            switch (token.Value[0])
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

            // Consume last parsed element as it has to be
            // the header's content
            header.MergeChildren(this.PopElement());
            
            this.Output.AddLast(header);
        }

        private void ParseCodeBlock(ParsingContext ctx)
        {
            // If the CodeBlock is a DmlSource code block, leave this basic code block
            if (this.PeekToken(1)?.Type == TokenType.CodeBlockLang && this.PeekToken(1)?.Value == "dml-source")
            {
                this.ParseDmlSource(ctx);
                return;
            }

            // Get a reference to the last node in the linked list
            // before doing anything related to this inline element
            var lastNode = this.Output.Last;

            // Save the starting token
            Token startToken = this.ConsumeToken();

            // If the code block starts with !```, it is a code block that
            // allows markup processing, if not, it is a basic block code.
            // Save the current state of the markup processing
            bool oldMarkupProcessingState = ctx.SetMarkupProcessingEnabled(startToken.Value.StartsWith("!"));

            Token token = this.ConsumeToken(); // Consume the NL or the CodeBlockLang

            // If next token is the CodeBlockLang, consume it and save
            // the value to be used in the class attribute
            string lang = null;
            if (token?.Type == TokenType.CodeBlockLang)
            {
                this.ConsumeToken(); // NL
                lang = token.Value;
            }

            while (this.HasTokens)
            {
                token = this.PeekToken();

                // End the loop if the closing token is found
                if (token.Type == TokenType.CodeBlock)
                {
                    this.ConsumeToken();
                    break;
                }

                // If the escaped token is a CodeBlock, ignore the Escape token and consume the CodeBlock
                if (token.Type == TokenType.Escape && this.PeekToken(1)?.Type == TokenType.CodeBlock)
                {
                    this.ConsumeToken(); // Ignore Escape
                    token = this.ConsumeToken(); // Consume CodeBlock
                }

                // If the markup is not enabled or there's no inline element parser for the token type, use ParseText.
                this.GetInlineParser(ctx, token, false)(ctx);
            }

            // Create a CodeNode that will be rendered as a block
            CodeNode codeNode = new CodeNode(true);

            // Set the class if lang is available
            if (lang != null)
                codeNode.Attributes["class"] = lang;

            while (this.Output.Any())
            {
                // If the last node is equals to our saved lastNode, it means we reached
                // the starting point so we need to stop consuming Output's elements
                if (this.Output.Last == lastNode)
                    break;

                // Add a child to the current code node
                codeNode.InsertChild(0, this.PopElement());
            }

            // Wrap the CodeNode with a PreformattedNode
            PreformattedNode pre = new PreformattedNode();
            pre.AddChild(codeNode);

            // Add the node to the Output
            this.Output.AddLast(pre);

            // Restore the previous state
            ctx.SetMarkupProcessingEnabled(oldMarkupProcessingState);
        }

        private void ParseDmlSource(ParsingContext ctx)
        {
            this.ConsumeToken(); // Consume CodeBlock
            this.ConsumeToken(); // Consume CodeBlockLang
            this.ConsumeToken(); // Consume NewLine

            // This will contain all the tokens withing the code block
            List<Token> source = new List<Token>();

            while (this.HasTokens)
            {
                var token = this.ConsumeToken();

                // Break if we found the closing token
                if (token.Type == TokenType.CodeBlock)
                    break;

                // If the escaped token is a CodeBlock, ignore the Escape and take the CodeBlock
                if (token.Type == TokenType.Escape && this.PeekToken()?.Type == TokenType.CodeBlock)
                    token = this.ConsumeToken();

                source.Add(token);
            }

            // Source
            CodeNode code = new CodeNode(true);

            // Process the source as plain text
            source.ForEach(src => code.AddChild(new TextNode((src.OriginalValue ?? src.Value).Replace("<", "&lt;"))));

            // Add the CodeBlock with the source
            this.Output.AddLast(code);

            // Process previous source to get the rendered version
            Parser parser = new Parser();
            DmlDocument doc = parser.Parse(source);

            // Get body's children of the parsed document
            doc.Body.Children.ForEach(c => this.Output.AddLast(c));

            // Add a <hr/> after the DmlSource
            ThematicBreakNode hr = new ThematicBreakNode();
            hr.Attributes["class"] = "short";

            this.Output.AddLast(hr);
        }

        private void ParsePreformatted(ParsingContext ctx)
        {
            // Get a reference to the last node in the linked list
            // before doing anything related to this preformatted block
            var lastNode = this.Output.Last;

            // Consume the Preformatted token
            this.ConsumeToken();

            // Disable markup processing. Save the current markup processing state
            bool oldMarkupProcessingState = ctx.SetMarkupProcessingEnabled(false);

            while (this.HasTokens)
            {
                var token = this.PeekToken();

                // Break on 2NL
                if (token.Type == TokenType.DoubleNewLine)
                    break;

                // Consume the indentation after a new line
                if (token.Type == TokenType.NewLine && this.PeekToken(1)?.Type == TokenType.Indentation)
                {
                    this.ConsumeToken();
                    this.ConsumeToken();
                    this.Output.AddLast(new TextNode("\n"));
                    continue;
                }

                // It is always ParseText
                this.ParseText(ctx);
            }

            // Restore markup processing
            ctx.SetMarkupProcessingEnabled(oldMarkupProcessingState);
            
            CodeNode code = new CodeNode(true);

            // Populate the CodeNode
            while (this.Output.Any() & this.Output.Last != lastNode)
                code.InsertChild(0, this.PopElement());

            // Wrap the code node into the pref node
            PreformattedNode pre = new PreformattedNode();

            pre.AddChild(code);

            // Add the pre node into the output
            this.Output.AddLast(pre);
        }

        private ListType GetListType(Token listToken)
        {
            return listToken.Value == "# " ? ListType.Ordered
                : listToken.Value.StartsWith("[") ? ListType.Todo
                : ListType.Unordered;
        }

        private int? GetOrderedListStartIndex(Token tkn)
        {
            if (tkn.OriginalValue == null || tkn.OriginalValue.Length <= 2)
                return null;

            var value = tkn.OriginalValue.Substring(0, tkn.OriginalValue.Length - 2);

            if (int.TryParse(value, out int index))
                return index;

            return null;
        }

        private bool IsSameListType(Token a, Token b) => a.Type == b.Type && a.Value[0] == b.Value[0] && (a.OriginalValue == b.OriginalValue || a.OriginalValue != null);

        private void ParseListItem(ParsingContext ctx)
        {
            // Get a reference to the last node in the linked list
            // before doing anything related to this list
            var lastNode = this.Output.Last;

            // Each list item is responsible of removeing the indentation
            while (this.PeekToken()?.Type == TokenType.Indentation)
                this.ConsumeToken();

            // Retrieve the token that contains the list type info
            var listToken = this.ConsumeToken();

            while (this.HasTokens)
            {
                var token = this.PeekToken();

                // Break on ListItem or Indentation to let ParseList parse the new items
                // Break on DoubleNewLine to let some caller in the chain to handle it
                if (token.Type == TokenType.ListItem || token.Type == TokenType.DoubleNewLine || token.Type == TokenType.Indentation)
                    break;

                // Just one new line means the content is still part of the current item, consume it and continue
                if (token.Type == TokenType.NewLine)
                {
                    this.ConsumeToken();
                    continue;
                }

                // Parse the inline element
                this.GetInlineParser(ctx, token)(ctx);
            }

            // Todo items use a different implementation
            var listItem = this.GetListType(listToken) != ListType.Todo ? new ListItemNode()
                            : (DmlElement)new TodoListItemNode(listToken.Value.ToLower().StartsWith("[x"));

            // Todos already have children, we need to get the base index to insert new nodes
            var baseIndex = listItem.Children.Count;

            // Populate the ListItem
            while (this.Output.Any() & this.Output.Last != lastNode)
                listItem.InsertChild(baseIndex, this.PopElement());

            // Add the ListItem to the Output
            this.Output.AddLast(listItem);
        }

        private void ParseList(ParsingContext ctx)
        {
            // Get a reference to the last node in the linked list
            // before doing anything related to this list
            var lastNode = this.Output.Last;

            // Track the indents for nested lists
            // indents contains the current list's indentation
            int indents = 0;
            
            while (this.PeekToken(indents)?.Type == TokenType.Indentation)
                indents++;

            // This tokens contains the type of list (we step over 'indents' tokens)
            var listTypeToken = this.PeekToken(indents);

            // Compute the ListType
            ListType listType = this.GetListType(listTypeToken);

            // Compute the start index if the list is an Ordered list
            int? listStartIndex = listType == ListType.Ordered ? this.GetOrderedListStartIndex(listTypeToken) : (int?)null;

            // Use a flag to know if this current list is an enumerated list
            bool isEnumeratedList = listStartIndex.HasValue;

            // Keep track of the last index (enumerated lists)
            int? lastIndex = null;

            while (this.HasTokens)
            {
                // Check the current indentation level
                int currentIndents = 0;
                while (this.PeekToken(currentIndents)?.Type == TokenType.Indentation)
                    currentIndents++;

                var token = this.PeekToken(currentIndents);

                // This tokens must be a list item
                if (token.Type != TokenType.ListItem)
                    break;

                // If currentIndents is lesser than the original indents, we need to go
                // back and close this list
                if (currentIndents < indents)
                    break;

                // Get the current token's list type
                ListType currentType = this.GetListType(token);

                // If the list type changes, and we are on the same indentation level, we need to
                // close this list to start a new one that will be sibling of this one
                if (!this.IsSameListType(listTypeToken, token) && currentIndents == indents)
                    break;

                // Ordered lists might break if they are "numerated"
                if (isEnumeratedList && currentIndents == indents)
                {
                    // Check if lastIndex is poupulted, first time it is null
                    // Check if currentType is ordered too
                    if (lastIndex.HasValue && currentType == ListType.Ordered)
                    {
                        // Compute the currentIndex
                        var currentIndex = this.GetOrderedListStartIndex(token);

                        // If the currentIndex exists is not lastIndex + 1, break this list
                        if (!currentIndex.HasValue || currentIndex <= lastIndex || currentIndex > lastIndex + 1)
                            break;
                    }

                    // Update last index
                    lastIndex = this.GetOrderedListStartIndex(token);
                }

                // If the indent level is the same, just add a new <li> to the list
                if (currentIndents == indents)
                {
                    this.ParseListItem(ctx);
                    continue;
                }

                // If not, it means the currentIndents is greater than the original indents.
                // In that case we need to parse a new list that will be child of the last
                // saved <li> element
                this.ParseList(ctx);

                // Retrieve the parsed list
                var innerList = this.PopElement();

                // Append the inner list to the last <li>
                this.Output.Last().AddChild(innerList);
            }

            // Create the ListNode and populate with the ListItemNodes
            ListNode list = new ListNode(listType);

            // If the list is enumerated, set the start index
            if (listStartIndex.HasValue)
            {
                list.Attributes["start"] = listStartIndex.Value.ToString();
                list.Properties["index"] = listStartIndex.Value;
            }

            list.Properties["indents"] = indents;

            // Process list's children
            while (this.Output.Any() & this.Output.Last != lastNode)
                list.InsertChild(0, this.PopElement());

            // Add the list to the output
            this.Output.AddLast(list);
        }

        private void ParseEscapeBlock(ParsingContext ctx)
        {
            // Consume the starting token ``
            this.ConsumeToken();

            // // Disable the markup processing. Save the markup processing state
            bool oldMarkupProcessingState = ctx.SetMarkupProcessingEnabled(false);            
            

            while (this.HasTokens)
            {
                var token = this.PeekToken();

                // Break when we found the closing token ``
                if (token.Type == TokenType.EscapeBlock)
                {
                    this.ConsumeToken();
                    break;
                }

                // Always process the content as plain text
                this.ParseText(ctx);
            }

            // Restore the markup processing state
            ctx.SetMarkupProcessingEnabled(oldMarkupProcessingState);
        }

        #endregion

        #region Inline parsers

        private void ParseNewLine(ParsingContext ctx) => this.Output.AddLast(new TextNode(this.ConsumeToken().Value));

        private void ParseBold(ParsingContext ctx) => this.ParseInline(ctx, new StrongNode(), TokenType.BoldClose);

        private void ParseItalic(ParsingContext ctx) => this.ParseInline(ctx, new ItalicNode(), TokenType.Italic);

        private void ParseUnderline(ParsingContext ctx) => this.ParseInline(ctx, new UnderlineNode(), TokenType.Underlined);

        private void ParseStrikethrough(ParsingContext ctx) => this.ParseInline(ctx, new StrikeNode(), TokenType.Strikethrough);

        private void ParseInlineCode(ParsingContext ctx)
        {
            // Lookahead to search for the "ending" token, if it is not present, the "InlineCode" we found
            // is [not] an inline code tag
            Token tmp = null;
            int i = 0;
            while ((tmp = this.PeekToken(++i)) != null)
            {
                if (tmp.Type == TokenType.InlineCode || tmp.Type == TokenType.DoubleNewLine)
                    break;
            }

            if (tmp == null || tmp.Type == TokenType.DoubleNewLine)
            {
                this.ParseText(ctx);
                return;
            }

            // At this point we made sure it is a valid inline code tag
            bool oldMarkupProcessingMode = ctx.SetMarkupProcessingEnabled(false);

            this.ParseInline(ctx, new CodeNode(false), TokenType.InlineCode);

            ctx.SetMarkupProcessingEnabled(oldMarkupProcessingMode);
        }

        // This method is used for almost all the inline elements as all of them have a similar parsing process
        private void ParseInline(ParsingContext ctx, DmlElement element, TokenType close)
        {
            // Get a reference to the last node in the linked list
            // before doing anything related to this inline element
            var lastNode = this.Output.Last;

            // Keep the start token, we could need it 
            var startToken = this.ConsumeToken();

            while (true)
            {
                // If we run out of tokens, we need to place the starting token after lastNode (starting point).
                // If lastNode is null, it means we don't have elements in the Output, so place the start token
                if (!this.HasTokens)
                {
                    if (lastNode != null)
                        this.Output.AddAfter(lastNode, new TextNode(startToken.Value));
                    else
                        this.Output.AddFirst(new TextNode(startToken.Value));
                    return;
                }

                // Keep parsing more inline elements
                Token token = this.PeekToken();

                // The 2-NL rule is handled at block elements, so if we find two new lines
                // we need to return to the caller, but because the 2-NL will end the current
                // element, it means it is not a valid "inline" element, just plain text.
                // We insert a new TextNode with the starting token's value after our
                // lastNode
                if (token.Type == TokenType.DoubleNewLine)
                {
                    this.Output.AddAfter(lastNode, new TextNode(startToken.Value));
                    return;
                }

                // If next token is the one that closes this element, consume the token
                // and break the loop
                if (token.Type == close)
                {
                    this.ConsumeToken();
                    break;
                }

                this.GetInlineParser(ctx, token)(ctx);
            }

            // If the last node is equals to our saved lastNode, it means we reached
            // the starting point so we need to stop consuming Output's elements
            while (this.Output.Any() && this.Output.Last != lastNode)
            {
                // Add a child to the current paragrapg
                element.InsertChild(0, this.PopElement());
            }

            // Add the parsed element
            this.Output.AddLast(element);
        }

        private void ParseLink(ParsingContext ctx)
        {
            // Get a reference to the first token before any link's token
            var lastNode = this.Output.Last;

            // Consume the start token, we might need it if this is not
            // a valid link
            var startToken = this.ConsumeToken();

            // We need to parse the link's content, but if it is empty
            // we won't create a LinkNode
            bool isValidLink = false;

            while (this.HasTokens)
            {
                var token = this.PeekToken();

                // Pipe divide link's sections
                if (token.Type == TokenType.Pipe)
                {
                    this.ConsumeToken();
                    break;
                }

                // If we find the LinkEnd, we need to break
                if (token.Type == TokenType.LinkEnd || token.Type == TokenType.DoubleNewLine)
                    break;

                this.GetInlineParser(ctx, token)(ctx);

                // Check if the link's content is not empty
                isValidLink |= token.Type != TokenType.Text || !string.IsNullOrWhiteSpace(token.Value);
            }

            // We have content, we have a link. Now we need to parse (if available)
            // the href and title attributes
            string href = "";
            string title = "";

            // Href
            while (this.HasTokens)
            {
                var token = this.PeekToken();

                if (token.Type == TokenType.Pipe)
                {
                    this.ConsumeToken();
                    break;
                }

                if (token.Type == TokenType.LinkEnd || token.Type == TokenType.DoubleNewLine)
                    break;

                href += this.ConsumeToken().Value;
            }

            // Title
            while (this.HasTokens)
            {
                var token = this.PeekToken();

                if (token.Type == TokenType.LinkEnd || token.Type == TokenType.DoubleNewLine)
                    break;

                title += this.ConsumeToken().Value;
            }


            // If next token is not a LinkEnd, it is not a valid link
            isValidLink &= this.PeekToken()?.Type == TokenType.LinkEnd;


            // If the final token is not a LinkEnd, it is not
            // a link, we need to return the parsed content
            if (!isValidLink)
            {
                // Add the starting token as plain text
                this.AddAfterElement(lastNode, new TextNode(startToken.Value));

                // We need to parse the href and title attributes, because
                // we consumed them as plain text before
                Parser parser = new Parser();

                // Parse the href
                var hrefdoc = parser.Parse(href);
                
                if (hrefdoc.Body.Children.Any())
                {
                    this.Output.AddLast(new TextNode("|"));
                    hrefdoc.Body.Children[0].Children.ForEach(c => this.Output.AddLast(c));
                }

                // Parse the title
                var titledoc = parser.Parse(title);

                if (titledoc.Body.Children.Any())
                {
                    this.Output.AddLast(new TextNode("|"));
                    titledoc.Body.Children[0].Children.ForEach(c => this.Output.AddLast(c));
                }                    

                return;
            }

            // Consume the LinkEnd token
            this.ConsumeToken();

            // Check if the link is a link to a reference
            href = href.Trim();
            if (href.StartsWith(":"))
            {
                List<string> titles = title.Split(',').ToList();
                List<string> hrefs = href.Substring(1).Split(',').ToList();

                CustomNode links = new CustomNode("span");

                while (this.Output.Any() & this.Output.Last != lastNode)
                    links.InsertChild(0, this.PopElement());

                for (int i = 0; i < hrefs.Count; i++)
                {
                    ReferenceLinkNode refLink = new ReferenceLinkNode(hrefs.ElementAt(i), titles.ElementAtOrDefault(i));
                    links.AddChild(refLink);
                };

                this.Output.AddLast(links);

                return;
            }

            // If it is not a link to a reference, it is a simple
            // anchor
            LinkNode a = new LinkNode(href, title);

            while (this.Output.Any() & this.Output.Last != lastNode)
                a.InsertChild(0, this.PopElement());

            this.Output.AddLast(a);
        }

        private void ParseImage(ParsingContext ctx)
        {
            // Get a reference to the first token before any img's token
            var lastNode = this.Output.Last;

            // Consume the start token, we might need it if this is not
            // a valid image
            var startToken = this.ConsumeToken();

            var isValidImage = true;

            // src
            string source = "";

            while (this.HasTokens)
            {
                var token = this.PeekToken();

                // Pipe close the title section, consume it and break
                if (token.Type == TokenType.Pipe)
                {
                    this.ConsumeToken();
                    break;
                }

                // Image end or 2NL break, because of end of image or invalid image
                if (token.Type == TokenType.ImageEnd || token.Type == TokenType.DoubleNewLine)
                    break;

                source += this.ConsumeToken().Value;
            }

            // If src is empty, it is not a valid img tag
            if (string.IsNullOrWhiteSpace(source))
                isValidImage = false;

            // title
            string title = "";

            while (this.HasTokens)
            {
                var token = this.PeekToken();

                // Pipe close the title section, consume it and break
                if (token.Type == TokenType.Pipe)
                {
                    this.ConsumeToken();
                    break;
                }

                // Image end or 2NL break, because of end of image or invalid image
                if (token.Type == TokenType.ImageEnd || token.Type == TokenType.DoubleNewLine)
                    break;

                title += this.ConsumeToken().Value;
            }

            // alt
            string altTitle = "";
            
            while (this.HasTokens)
            {
                var token = this.PeekToken();

                // Image end or 2NL break, because of end of image or invalid image
                if (token.Type == TokenType.ImageEnd || token.Type == TokenType.DoubleNewLine)
                    break;

                altTitle += this.ConsumeToken().Value;
            }


            // If next token is not the ImageEnd, it is an invalid img tag
            isValidImage &= this.PeekToken()?.Type == TokenType.ImageEnd;


            // If the final token is not a ImageEnd, it is not
            // an image, we need to return the parsed content
            if (!isValidImage)
            {
                // Add the starting token as plain text
                this.AddAfterElement(lastNode, new TextNode(startToken.Value));

                // We need to parse the srouce, title, and alt title attributes, because
                // we consumed them as plain text before
                Parser parser = new Parser();

                // Parse the source
                var srcdoc = parser.Parse(source);

                if (srcdoc.Body.Children.Any())
                    srcdoc.Body.Children[0].Children.ForEach(c => this.Output.AddLast(c));

                // Parse the title
                var titledoc = parser.Parse(title);

                if (titledoc.Body.Children.Any())
                {
                    this.Output.AddLast(new TextNode("|"));
                    titledoc.Body.Children[0].Children.ForEach(c => this.Output.AddLast(c));
                }

                // Parse the alt title
                var alttitledoc = parser.Parse(altTitle);

                if (alttitledoc.Body.Children.Any())
                {
                    this.Output.AddLast(new TextNode("|"));
                    alttitledoc.Body.Children[0].Children.ForEach(c => this.Output.AddLast(c));
                }

                return;
            }

            // Consume }]
            this.ConsumeToken();

            ImageNode img = new ImageNode(title, source, altTitle);

            // Add the imate to the output
            this.Output.AddLast(img);
        }

        private void ParseReference(ParsingContext ctx)
        {
            // Save the last inserted node before any
            // Reference processing
            var lastNode = this.Output.Last;

            // Consume and save the startToken, we might need it later
            var startToken = this.ConsumeToken();

            // We might need th colon token
            Token colonToken = null;

            bool validReference = true;

            // Parse the content between | and : (href)
            string href = "";

            while (this.HasTokens)
            {
                var token = this.PeekToken();

                // If it is a DoubleNewLine, it is an invalid reference
                if (token.Type == TokenType.DoubleNewLine)
                {
                    validReference = false;
                    break;
                }

                // Break at colon
                if (token.Type == TokenType.Colon)                    
                    break;

                // Concatenate all tokens as plain text (it is a HTML attribute's value)
                href += this.ConsumeToken().Value;
            }

            if (this.PeekToken()?.Type != TokenType.Colon)
                validReference = false;
            else
                colonToken = this.ConsumeToken();

            // If there is no more input, it is not a valid reference
            validReference &= this.HasTokens;
            
            // We need to parse the Reference's title
            while (validReference && this.HasTokens)
            {
                var token = this.PeekToken();

                // Break on 2NL rule and mark the parsing as invalid
                if (token.Type == TokenType.DoubleNewLine)
                {
                    validReference = false;
                    break;
                }

                // Break on reference end
                if (token.Type == TokenType.Pipe)                    
                    break;

                this.GetInlineParser(ctx, token)(ctx);
            }

            // If the next token is not a ReferenceEnd, it is not a valid reference
            if (this.PeekToken()?.Type != TokenType.Pipe)
                validReference = false;

            // We need to rollback the parsing
            if (!validReference)
            {
                // Insert the pipe as plain text
                var startTokenElement = this.AddAfterElement(lastNode, new TextNode(startToken.Value));

                // Parse the href, it might contain markup elements
                Parser parser = new Parser();
                var hrefdoc = parser.Parse(href);

                if (hrefdoc.Body.Children.Any())
                {
                    hrefdoc.Body.Children[0].Children.ForEach(c => this.Output.AddAfter(startTokenElement, c));

                    // Find the new "last" node
                    startTokenElement = this.Output.FindLast(hrefdoc.Body.Children[0].Children.Last());
                }

                // If colonToken has been found, we need to insert it in the right position
                if (colonToken != null)
                    this.Output.AddAfter(startTokenElement, new TextNode(colonToken.Value));

                return;
            }

            // Consume the ReferenceEnd
            this.ConsumeToken();

            // Create the new ReferenceNode
            ReferenceNode reference = new ReferenceNode(href.Trim());

            while (this.Output.Any() & this.Output.Last != lastNode)
                reference.InsertChild(0, this.PopElement());

            this.Output.AddLast(reference);
        }

        private void ParseEscape(ParsingContext ctx)
        {
            this.ConsumeToken();

            // If the escaped token is not an especial token
            // we just add a backslash
            if (this.PeekToken()?.Type == TokenType.Text)
            {
                this.Output.AddLast(new TextNode("\\"));
            }
            else if (this.PeekToken()?.Type == TokenType.Lt)
            {
                this.ConsumeToken();
                this.Output.AddLast(new TextNode("&lt;"));
            }
            else
            {
                // If the token's type is different from Text, we process
                // the token's value as plain text
                this.ParseText(ctx);
            }
        }

        private void ParseText(ParsingContext ctx)
        {
            Token token = this.ConsumeToken();

            string value = token.Value;

            // Sanitize the '<'
            if (!ctx.MarkupProcessingEnabled)
                value = (value ?? "").Replace("<", "&lt;");

            this.Output.AddLast(new TextNode(value));
        }

        #endregion
    }
}
