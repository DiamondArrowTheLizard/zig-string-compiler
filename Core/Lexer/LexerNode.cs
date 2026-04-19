namespace Core.Lexer;

public class LexerNode
{
    public Token TokenCurrent { get; set; } = Token.Unknown;
    public Token TokenPrev { get; set; } = Token.Unknown;
    public string? TokenDesc { get; set; }
    public string? WordCurrent { get; set; }
    public string? WordPrev { get; set; }
    public uint Line { get; set; }
    public uint WordStart { get; set; }
    public uint WordEnd { get; set; }
    public uint WordEndPrev { get; set; }
}