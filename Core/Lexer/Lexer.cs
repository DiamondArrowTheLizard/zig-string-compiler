using System;
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

    public IReadOnlyList<LexerNode> Nodes => _nodes;
    public TokenDictionary Dictionary => _dictionary;
    public LexerFlags Flags => _flags;
    public uint CurrentLine { get; private set; } = 1;

    private Token ParseToToken(string word)
    {
        Token token = _dictionary.GetKey(word);

        if (_flags.InsideQuotes && token != Token.Quote)
        {
            _dictionary.Insert(Token.Content, word, "Строка");
            return Token.Content;
        }

        if (token == Token.Quote)
        {
            _flags.InsideQuotes = !_flags.InsideQuotes;
            return Token.Quote;
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
                _expectId = false;
                if (!_dictionary.GetKey(word).Equals(Token.Id))
                    _dictionary.Insert(Token.Id, word, "Идентификатор");
                return Token.Id;
            }

            if (!_anyTokenSeen && !_declarationStarted)
                return Token.UnknownNoConst;

            return Token.Unknown;
        }

        return token;
    }

    private void AddToken(uint line, uint start, uint end, string lexeme, Token type)
    {
        var node = new LexerNode
        {
            Line = line,
            WordStart = start,
            WordEnd = end,
            WordCurrent = lexeme,
            TokenCurrent = type,
            TokenDesc = _dictionary.GetDescription(type) ?? "UNKNOWN"
        };

        if (_nodes.Count > 0)
        {
            var prev = _nodes[^1];
            node.TokenPrev = prev.TokenCurrent;
            node.WordPrev = prev.WordCurrent;
            node.WordEndPrev = prev.WordEnd;
        }

        _nodes.Add(node);

        if (type != Token.Space)
            _anyTokenSeen = true;
    }

    public void ParseLine(string line)
    {
        int index = 0;
        int len = line.Length;

        while (index < len)
        {
            char ch = line[index];

            if (char.IsWhiteSpace(ch))
            {
                AddToken(CurrentLine, (uint)index, (uint)index, " ", Token.Space);
                index++;
                continue;
            }

            switch (ch)
            {
                case ':':
                    AddToken(CurrentLine, (uint)index, (uint)index, ":", Token.Colon);
                    index++;
                    break;
                case '[':
                    AddToken(CurrentLine, (uint)index, (uint)index, "[", Token.BracesOpen);
                    index++;
                    break;
                case ']':
                    AddToken(CurrentLine, (uint)index, (uint)index, "]", Token.BracesClose);
                    index++;
                    break;
                case '=':
                    AddToken(CurrentLine, (uint)index, (uint)index, "=", Token.Equals);
                    index++;
                    break;
                case ';':
                    AddToken(CurrentLine, (uint)index, (uint)index, ";", Token.Semicolon);
                    index++;
                    _flags.FirstConst = false;
                    _flags.IdParsed = false;
                    _flags.InsideQuotes = false;
                    _declarationStarted = false;
                    _expectId = false;
                    break;
                case '"':
                {
                    const string quoteStr = "\"";
                    Token openTok = ParseToToken(quoteStr);
                    AddToken(CurrentLine, (uint)index, (uint)index, quoteStr, openTok);
                    index++;

                    int contentStart = index;
                    while (index < len && line[index] != '"')
                        index++;

                    if (index > contentStart)
                    {
                        string content = line.Substring(contentStart, index - contentStart);
                        Token contentTok = ParseToToken(content);
                        AddToken(CurrentLine, (uint)contentStart, (uint)(index - 1), content, contentTok);
                    }

                    if (index < len && line[index] == '"')
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
                        && line[index] != '"')
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
}