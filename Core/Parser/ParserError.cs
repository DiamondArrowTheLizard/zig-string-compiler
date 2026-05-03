namespace Core.Parser;

public class ParserError
{
    public string Fragment { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}