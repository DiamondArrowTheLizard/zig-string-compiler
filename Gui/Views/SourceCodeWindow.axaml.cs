using Avalonia.Controls;
using AvaloniaEdit;
using System.Collections.Generic;

namespace Gui.Views;

public partial class SourceCodeWindow : Window
{
    public SourceCodeWindow()
    {
        InitializeComponent();
        LoadSourceCodes();
    }

    private void LoadSourceCodes()
    {
        // Контент из вашего файла Parser.cs с исправленным экранированием
        ParserEditor.Text = @"using System;
using System.Collections.Generic;
using System.Linq;
using Core.Lexer;

namespace Core.Parser;

public class Parser
{
    private enum State
    {
        Start,
        AfterConst,
        AfterId,
        AfterColon,
        AfterOpenBracket,
        AfterCloseBracket,
        AfterSecondConst,
        AfterU8,
        AfterEquals,
        AfterOpenQuote,
        AfterContent,
        AfterCloseQuote
    }

    private static readonly Dictionary<State, HashSet<Token>> ExpectedTokens = new()
    {
        [State.Start] = new() { Token.Const },
        [State.AfterConst] = new() { Token.Id },
        [State.AfterId] = new() { Token.Colon },
        [State.AfterColon] = new() { Token.BracesOpen },
        [State.AfterOpenBracket] = new() { Token.BracesClose },
        [State.AfterCloseBracket] = new() { Token.ConstU8 },
        [State.AfterSecondConst] = new() { Token.U8 },
        [State.AfterU8] = new() { Token.Equals, Token.Semicolon },
        [State.AfterEquals] = new() { Token.Quote },
        [State.AfterOpenQuote] = new() { Token.Content, Token.Quote },
        [State.AfterContent] = new() { Token.Quote },
        [State.AfterCloseQuote] = new() { Token.Semicolon }
    };

    private static readonly Dictionary<State, Dictionary<Token, State>> Transitions = new()
    {
        [State.Start] = new() { [Token.Const] = State.AfterConst },
        [State.AfterConst] = new() { [Token.Id] = State.AfterId },
        [State.AfterId] = new() { [Token.Colon] = State.AfterColon },
        [State.AfterColon] = new() { [Token.BracesOpen] = State.AfterOpenBracket },
        [State.AfterOpenBracket] = new() { [Token.BracesClose] = State.AfterCloseBracket },
        [State.AfterCloseBracket] = new() { [Token.ConstU8] = State.AfterSecondConst },
        [State.AfterSecondConst] = new() { [Token.U8] = State.AfterU8 },
        [State.AfterU8] = new() { [Token.Equals] = State.AfterEquals, [Token.Semicolon] = State.Start },
        [State.AfterEquals] = new() { [Token.Quote] = State.AfterOpenQuote },
        [State.AfterOpenQuote] = new() { [Token.Content] = State.AfterContent, [Token.Quote] = State.AfterCloseQuote },
        [State.AfterContent] = new() { [Token.Quote] = State.AfterCloseQuote },
        [State.AfterCloseQuote] = new() { [Token.Semicolon] = State.Start }
    };

    private static bool AreTokensCompatible(Token expected, Token actual)
    {
        if (expected == actual) return true;
        if (expected == Token.Const && actual == Token.ConstU8) return true;
        if (expected == Token.ConstU8 && actual == Token.Const) return true;
        return false;
    }

    public static ParseResult Parse(IReadOnlyList<LexerNode> nodes, TokenDictionary dictionary)
    {
        var errors = new List<ParserError>();
        State currentState = State.Start;
        int index = 0;

        while (index < nodes.Count)
        {
            var token = nodes[index];
            var expectedSet = ExpectedTokens[currentState];

            Token matchedToken = Token.Unknown;
            bool matchFound = false;

            foreach (var expected in expectedSet)
            {
                if (AreTokensCompatible(expected, token.TokenCurrent))
                {
                    matchedToken = expected;
                    matchFound = true;
                    break;
                }
            }

            if (matchFound)
            {
                currentState = Transitions[currentState][matchedToken];
                index++;
                continue;
            }

            string? insertedTokenDesc = TryInsertSingleToken(currentState, token.TokenCurrent, dictionary);
            if (insertedTokenDesc != null)
            {
                errors.Add(new ParserError
                {
                    Fragment = token.WordCurrent ?? string.Empty,
                    Location = $""строка {token.Line}, позиция {token.WordStart + 1}"",
                    Description = $""Пропущен токен {insertedTokenDesc}""
                });

                Token insertedToken = GetTokenByDescription(insertedTokenDesc, dictionary);
                currentState = Transitions[currentState][insertedToken];
                continue;
            }

            string expectedDesc = string.Join("" или "", expectedSet.Select(t => $""\""{dictionary.GetDescription(t) ?? t.ToString()}\"" ""));
            errors.Add(new ParserError
            {
                Fragment = token.WordCurrent ?? string.Empty,
                Location = $""строка {token.Line}, позиция {token.WordStart + 1}"",
                Description = $""Неожиданный токен \""{dictionary.GetDescription(token.TokenCurrent) ?? token.TokenCurrent.ToString()}\"", ожидался {expectedDesc}""
            });

            index++;
        }

        return new ParseResult { Success = errors.Count == 0, Errors = errors };
    }

    private static string? TryInsertSingleToken(State state, Token actualToken, TokenDictionary dictionary)
    {
        foreach (var expectedTok in ExpectedTokens[state])
        {
            if (Transitions[state].TryGetValue(expectedTok, out var nextState))
            {
                if (ExpectedTokens[nextState].Any(t => AreTokensCompatible(t, actualToken)))
                {
                    return dictionary.GetDescription(expectedTok) ?? expectedTok.ToString();
                }
            }
        }
        return null;
    }

    private static Token GetTokenByDescription(string description, TokenDictionary dictionary)
    {
        foreach (Token tok in Enum.GetValues(typeof(Token)))
            if (dictionary.GetDescription(tok) == description) return tok;
        return Token.Unknown;
    }
}";

        // Контент из вашего файла Lexer.cs с исправленным экранированием
        LexerEditor.Text = @"using System;
using System.Collections.Generic;

namespace Core.Lexer;

public class Lexer
{
    private readonly List<LexerNode> _nodes = new();
    private readonly LexerFlags _flags = new();
    private readonly TokenDictionary _dictionary = new();

    private bool _declarationStarted = false;
    private bool _expectId = false;
    private bool _anyTokenSeen = false;
    private bool _afterBrackets = false;

    public IReadOnlyList<LexerNode> Nodes => _nodes;
    public TokenDictionary Dictionary => _dictionary;
    public LexerFlags Flags => _flags;
    public uint CurrentLine { get; private set; } = 1;

    private Token ParseToToken(string word)
    {
        Token token = _dictionary.GetKey(word);

        if (_flags.InsideQuotes && token != Token.Quote)
        {
            _dictionary.Insert(Token.Content, word, ""Строка"");
            return Token.Content;
        }

        if (token == Token.Quote)
        {
            _flags.InsideQuotes = !_flags.InsideQuotes;
            return Token.Quote;
        }

        if (token == Token.Const && _afterBrackets && _anyTokenSeen)
        {
            _afterBrackets = false;
            _expectId = false;
            return Token.ConstU8;
        }

        if (token == Token.Const)
        {
            if (!_declarationStarted)
            {
                _declarationStarted = true;
                _expectId = true;
            }
            return Token.Const;
        }

        if (token == Token.Unknown)
        {
            if (_expectId)
            {
                _dictionary.Insert(Token.Id, word, ""Идентификатор"");
                _expectId = false;
                _anyTokenSeen = true;
                return Token.Id;
            }
        }

        if (token == Token.BracesClose)
        {
            _afterBrackets = true;
        }

        return token;
    }

    private void AddToken(uint line, uint start, uint end, string word, Token token)
    {
        var node = new LexerNode
        {
            Line = line,
            WordStart = start,
            WordEnd = end,
            WordCurrent = word,
            TokenCurrent = token,
            TokenDesc = _dictionary.GetDescription(token)
        };

        if (_nodes.Count > 0)
        {
            var last = _nodes[^1];
            node.TokenPrev = last.TokenCurrent;
            node.WordPrev = last.WordCurrent;
            node.WordEndPrev = last.WordEnd;
        }

        _nodes.Add(node);
    }

    public void Tokenize(string text)
    {
        _nodes.Clear();
        _flags.InsideQuotes = false;
        CurrentLine = 1;
        _declarationStarted = false;
        _expectId = false;
        _anyTokenSeen = false;
        _afterBrackets = false;

        string[] lines = text.Split(new[] { ""\r\n"", ""\r"", ""\n"" }, StringSplitOptions.None);
        foreach (var line in lines)
        {
            ProcessLine(line);
        }
    }

    private void ProcessLine(string line)
    {
        int index = 0;
        int len = line.Length;
        string quoteStr = ""\"" "";

        while (index < len)
        {
            char c = line[index];

            if (char.IsWhiteSpace(c))
            {
                index++;
                continue;
            }

            switch (c)
            {
                case ':':
                    AddToken(CurrentLine, (uint)index, (uint)index, "":"", Token.Colon);
                    index++;
                    break;
                case '[':
                    AddToken(CurrentLine, (uint)index, (uint)index, ""["", Token.BracesOpen);
                    index++;
                    break;
                case ']':
                    AddToken(CurrentLine, (uint)index, (uint)index, ""]"", Token.BracesClose);
                    index++;
                    break;
                case '=':
                    AddToken(CurrentLine, (uint)index, (uint)index, ""="", Token.Equals);
                    index++;
                    break;
                case ';':
                    AddToken(CurrentLine, (uint)index, (uint)index, "";"", Token.Semicolon);
                    index++;
                    break;
                case '""':
                    {
                        AddToken(CurrentLine, (uint)index, (uint)index, quoteStr, Token.Quote);
                        _flags.InsideQuotes = !_flags.InsideQuotes;
                        index++;

                        int contentStart = index;
                        while (index < len && line[index] != '""')
                        {
                            index++;
                        }

                        if (index > contentStart)
                        {
                            string content = line.Substring(contentStart, index - contentStart);
                            Token contentTok = ParseToToken(content);
                            AddToken(CurrentLine, (uint)contentStart, (uint)(index - 1), content, contentTok);
                        }

                        if (index < len && line[index] == '""')
                        {
                            Token closeTok = ParseToToken(quoteStr);
                            AddToken(CurrentLine, (uint)index, (uint)index, quoteStr, closeTok);
                            index++;
                        }
                        break;
                    }
                default:
                    {
                        int start = index;
                        while (index < len && !char.IsWhiteSpace(line[index])
                            && line[index] != ':'
                            && line[index] != '['
                            && line[index] != ']'
                            && line[index] != '='
                            && line[index] != ';'
                            && line[index] != '""')
                        {
                            index++;
                        }
                        string word = line.Substring(start, index - start);
                        Token tok = ParseToToken(word);
                        AddToken(CurrentLine, (uint)start, (uint)(index - 1), word, tok);
                        break;
                    }
            }
        }

        CurrentLine++;
    }
}";
    }
}