using System.Collections.Generic;

namespace Core.Parser;

public class ParseResult
{
    public bool Success { get; set; }
    public List<ParserError> Errors { get; set; } = new();
}