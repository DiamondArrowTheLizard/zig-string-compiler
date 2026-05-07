namespace Core.Lexer;

public enum Token
{
    UnknownNoConst = 0,
    Unknown = 1,
    Const = 2,
    Space = 3,
    Id = 4,
    Colon = 5,
    BracesOpen = 6,
    BracesClose = 7,
    U8 = 8,
    Equals = 9,
    Quote = 10,
    Content = 11,
    Semicolon = 12,
    ConstU8 = 13
}