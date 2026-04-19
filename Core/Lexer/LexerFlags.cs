namespace Core.Lexer;

public class LexerFlags
{
    public bool FirstConst { get; set; }
    public bool IdParsed { get; set; }
    public bool InsideQuotes { get; set; }
}