using System;
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
        [State.AfterConst] = new() { Token.Id, Token.UnknownNoConst, Token.Unknown },
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
        [State.AfterConst] = new()
        {
            [Token.Id] = State.AfterId,
            [Token.UnknownNoConst] = State.AfterId,
            [Token.Unknown] = State.AfterId
        },
        [State.AfterId] = new() { [Token.Colon] = State.AfterColon },
        [State.AfterColon] = new() { [Token.BracesOpen] = State.AfterOpenBracket },
        [State.AfterOpenBracket] = new() { [Token.BracesClose] = State.AfterCloseBracket },
        [State.AfterCloseBracket] = new() { [Token.ConstU8] = State.AfterSecondConst },
        [State.AfterSecondConst] = new() { [Token.U8] = State.AfterU8 },
        [State.AfterU8] = new()
        {
            [Token.Equals] = State.AfterEquals,
            [Token.Semicolon] = State.Start
        },
        [State.AfterEquals] = new() { [Token.Quote] = State.AfterOpenQuote },
        [State.AfterOpenQuote] = new()
        {
            [Token.Content] = State.AfterContent,
            [Token.Quote] = State.AfterCloseQuote
        },
        [State.AfterContent] = new() { [Token.Quote] = State.AfterCloseQuote },
        [State.AfterCloseQuote] = new() { [Token.Semicolon] = State.Start }
    };

    private static readonly HashSet<Token> SynchronizingTokens = new()
    {
        Token.Semicolon
    };

    private static bool AreTokensCompatible(Token expected, Token actual)
    {
        if (expected == actual) return true;
        if ((expected == Token.Const || expected == Token.ConstU8) &&
            (actual == Token.Const || actual == Token.ConstU8))
            return true;
        if (expected == Token.Id && actual == Token.UnknownNoConst)
            return true;
        return false;
    }

    public ParseResult Parse(IReadOnlyList<LexerNode> nodes, TokenDictionary dictionary)
    {
        var errors = new List<ParserError>();
        var significant = nodes.Where(n => n.TokenCurrent != Token.Space).ToList();

        int index = 0;
        State state = State.Start;
        string? lastExpectedDescription = null;

        while (index < significant.Count)
        {
            var token = significant[index];

            if (state == State.Start && token.TokenCurrent != Token.Const)
            {
                errors.Add(new ParserError
                {
                    Fragment = GetTokenText(token),
                    Location = FormatLocation(token),
                    Description = "Ожидался токен \"Ключевое слово const\""
                });

                state = State.AfterConst;
                if (token.TokenCurrent == Token.UnknownNoConst || token.TokenCurrent == Token.Unknown)
                {
                    index++;
                }
                lastExpectedDescription = null;
                continue;
            }

            var expectedSet = ExpectedTokens[state];
            var matchedToken = expectedSet.FirstOrDefault(t => AreTokensCompatible(t, token.TokenCurrent), Token.Unknown);

            if (matchedToken != Token.Unknown)
            {
                state = Transitions[state][matchedToken];
                index++;
                lastExpectedDescription = null;
                continue;
            }

            string? insertedDescription = TryInsertSingleToken(state, token.TokenCurrent, dictionary);
            if (insertedDescription != null)
            {
                if (insertedDescription != lastExpectedDescription)
                {
                    errors.Add(new ParserError
                    {
                        Fragment = GetTokenText(token),
                        Location = FormatLocation(token),
                        Description = $"Ожидался токен \"{insertedDescription}\""
                    });
                    lastExpectedDescription = insertedDescription;
                }
                Token insertedToken = GetTokenByDescription(insertedDescription, dictionary);
                if (Transitions[state].ContainsKey(insertedToken))
                {
                    state = Transitions[state][insertedToken];
                }
                continue;
            }

            string expectedDesc = string.Join(" или ", expectedSet.Select(t =>
                $"\"{dictionary.GetDescription(t) ?? t.ToString()}\""));
            string tokenDesc = GetTokenDescription(token, dictionary);

            errors.Add(new ParserError
            {
                Fragment = GetTokenText(token),
                Location = FormatLocation(token),
                Description = $"Неожиданный токен \"{tokenDesc}\", ожидался {expectedDesc}"
            });

            index++;
            while (index < significant.Count)
            {
                var next = significant[index];
                if (SynchronizingTokens.Contains(next.TokenCurrent)) break;
                if (ExpectedTokens[state].Any(t => AreTokensCompatible(t, next.TokenCurrent))) break;
                if (TryInsertSingleToken(state, next.TokenCurrent, dictionary) != null) break;
                index++;
            }
        }

        while (state != State.Start)
        {
            var missingSet = ExpectedTokens[state];
            Token recoverToken = missingSet.Contains(Token.Semicolon) ? Token.Semicolon : missingSet.First();
            
            string desc = dictionary.GetDescription(recoverToken) ?? recoverToken.ToString();
            string location = significant.Count > 0
                ? $"строка {significant[^1].Line}, позиция {significant[^1].WordEnd + 2}"
                : "строка 1, позиция 1";

            errors.Add(new ParserError
            {
                Fragment = string.Empty,
                Location = location,
                Description = $"Ожидался токен \"{desc}\" в конце объявления"
            });

            if (Transitions[state].TryGetValue(recoverToken, out var nextState))
                state = nextState;
            else
                break;
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
        {
            if (dictionary.GetDescription(tok) == description) return tok;
        }
        return Token.Unknown;
    }

    private static string FormatLocation(LexerNode token) => $"строка {token.Line}, позиция {token.WordStart + 1}";
    private static string GetTokenText(LexerNode token) => token.WordCurrent ?? string.Empty;
    private static string GetTokenDescription(LexerNode token, TokenDictionary dictionary) => 
        dictionary.GetDescription(token.TokenCurrent) ?? token.TokenCurrent.ToString();
}