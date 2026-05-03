namespace Core.Lexer;

public enum Token
{
    UnknownNoConst = -1,
    Unknown = 0,
    Const = 1,
    Space = 2,
    Id = 3,
    Colon = 4,
    BracesOpen = 5,
    BracesClose = 6,
    U8 = 7,
    Equals = 8,
    Quote = 9,
    Content = 10,
    Semicolon = 11,
}