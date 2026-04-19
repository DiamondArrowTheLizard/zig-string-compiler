using Core.Lexer;
using System.Collections.Generic;
using System.Linq;

namespace Core.Parser;

public class ParserContext
{
    private readonly IReadOnlyList<LexerNode> _tokens;
    private int _position;
    private readonly List<ErrorInfo> _errors = new();

    public ParserContext(IReadOnlyList<LexerNode> tokens)
    {
        _tokens = tokens;
        _position = 0;
    }

    private void SkipSpaces()
    {
        while (_position < _tokens.Count && _tokens[_position].TokenCurrent == Token.Space)
            _position++;
    }

    public LexerNode? Current
    {
        get
        {
            SkipSpaces();
            return _position < _tokens.Count ? _tokens[_position] : null;
        }
    }

    public bool IsAtEnd
    {
        get
        {
            SkipSpaces();
            return _position >= _tokens.Count;
        }
    }

    public IReadOnlyList<ErrorInfo> Errors => _errors;

    public void Advance()
    {
        SkipSpaces();
        if (_position < _tokens.Count)
            _position++;
    }

    public Token? PeekToken() => Current?.TokenCurrent;

    public void Recover(State currentState)
    {
        var expected = GetExpectedTokens(currentState);

        while (Current != null && !expected.Contains(Current.TokenCurrent))
        {
            _errors.Add(new ErrorInfo(
                Current.WordCurrent ?? "",
                Current.Line,
                Current.WordStart,
                $"Unexpected token '{Current.WordCurrent}'. Expected one of: {string.Join(", ", expected)}"
            ));
            Advance();
        }

        if (Current != null && expected.Contains(Current.TokenCurrent))
        {
            var missingChain = GetMissingChain(currentState);
        }
    }

    private Token[] GetExpectedTokens(State state) => state switch
    {
        State.S0 => new[] { Token.Const },
        State.S1 => new[] { Token.Id },
        State.S2 => new[] { Token.Colon },
        State.S3 => new[] { Token.BracesOpen },
        State.S4 => new[] { Token.BracesClose },
        State.S5 => new[] { Token.Const },
        State.S6 => new[] { Token.U8 },
        State.S7 => new[] { Token.Equals, Token.Semicolon },
        State.S8 => new[] { Token.Quote },
        State.S9 => new[] { Token.Content, Token.Quote },
        State.S10 => new[] { Token.Quote },
        State.S11 => new[] { Token.Semicolon },
        _ => new Token[0]
    };

    private string GetMissingChain(State state) => state switch
    {
        State.S0 => "const",
        State.S1 => "id",
        State.S2 => ":",
        State.S3 => "[",
        State.S4 => "]",
        State.S5 => "const",
        State.S6 => "u8",
        State.S7 => "= \"\";",
        State.S8 => "\"",
        State.S9 => "content or \"",
        State.S10 => "\"",
        State.S11 => ";",
        _ => ""
    };

    public void ReportError(string message)
    {
        var token = Current ?? _tokens.LastOrDefault();
        _errors.Add(new ErrorInfo(
            token?.WordCurrent ?? "",
            token?.Line ?? 0,
            token?.WordStart ?? 0,
            message
        ));
    }
}