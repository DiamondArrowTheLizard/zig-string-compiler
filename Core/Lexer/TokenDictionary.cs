using System.Collections.Generic;

namespace Core.Lexer;

public class TokenDictionary
{
    private readonly Dictionary<Token, (string Value, string Description)> _forward = new();
    private readonly Dictionary<string, Token> _reverse = new();

    public TokenDictionary()
    {
        Insert(Token.Const, "const", "Ключевое слово const");
        Insert(Token.Space, " ", "(Пробел)");
        Insert(Token.Colon, ":", "Оператор-двоеточие");
        Insert(Token.BracesOpen, "[", "Оператор-начало массива");
        Insert(Token.BracesClose, "]", "Оператор-конец массива");
        Insert(Token.U8, "u8", "Ключевое слово u8");
        Insert(Token.Equals, "=", "Оператор присваивания");
        Insert(Token.Quote, "\"", "Кавычки");
        Insert(Token.Semicolon, ";", "Конец оператора");
        Insert(Token.UnknownNoConst, "", "Пропущено ключевое слово const");
    }

    public void Insert(Token key, string value, string description)
    {
        _forward[key] = (value, description);
        _reverse[value] = key;
    }

    public string? GetValue(Token key) =>
        _forward.TryGetValue(key, out var pair) ? pair.Value : null;

    public string? GetDescription(Token key) =>
        _forward.TryGetValue(key, out var pair) ? pair.Description : null;

    public Token GetKey(string value) =>
        _reverse.TryGetValue(value, out var token) ? token : Token.Unknown;

    public void PrintAll(System.IO.TextWriter writer)
    {
        foreach (var kvp in _forward)
            writer.WriteLine($"{kvp.Key:D}: {kvp.Value.Value} - {kvp.Value.Description}");
    }
}