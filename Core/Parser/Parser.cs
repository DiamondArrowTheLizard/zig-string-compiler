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

                if (token.TokenCurrent == Token.UnknownNoConst || token.TokenCurrent == Token.Id || token.TokenCurrent == Token.Unknown)
                {
                    state = State.AfterId;
                    index++;
                }
                else
                {
                    index++;
                    state = State.AfterConst;
                }
                lastExpectedDescription = null;
                continue;
            }

            if (state == State.AfterConst && token.TokenCurrent == Token.Unknown)
            {
                errors.Add(new ParserError
                {
                    Fragment = GetTokenText(token),
                    Location = FormatLocation(token),
                    Description = "Неожиданный токен \"Unknown\", ожидался \"Идентификатор\""
                });
                state = State.AfterId;
                index++;
                lastExpectedDescription = null;
                continue;
            }

            var expectedSet = ExpectedTokens[state];
            if (expectedSet.Contains(token.TokenCurrent))
            {
                state = Transitions[state][token.TokenCurrent];
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
            lastExpectedDescription = dictionary.GetDescription(expectedSet.First()) ?? expectedSet.First().ToString();
            index++;
            while (index < significant.Count)
            {
                var next = significant[index];
                if (SynchronizingTokens.Contains(next.TokenCurrent))
                    break;
                if (ExpectedTokens[state].Contains(next.TokenCurrent) ||
                    TryInsertSingleToken(state, next.TokenCurrent, dictionary) != null)
                    break;
                index++;
            }
        }

        while (state != State.Start)
        {
            var missingSet = ExpectedTokens[state];
            if (missingSet.Contains(Token.Semicolon))
            {
                string desc = dictionary.GetDescription(Token.Semicolon) ?? ";";
                string location = significant.Count > 0
                    ? $"строка {significant[^1].Line}, позиция {significant[^1].WordEnd + 2}"
                    : "строка 1, позиция 1";
                errors.Add(new ParserError
                {
                    Fragment = string.Empty,
                    Location = location,
                    Description = $"Ожидался токен \"{desc}\" в конце объявления"
                });
                state = Transitions[state][Token.Semicolon];
            }
            else
            {
                var firstExpected = missingSet.First();
                string desc = dictionary.GetDescription(firstExpected) ?? firstExpected.ToString();
                string location = significant.Count > 0
                    ? $"строка {significant[^1].Line}, позиция {significant[^1].WordEnd + 2}"
                    : "строка 1, позиция 1";
                errors.Add(new ParserError
                {
                    Fragment = string.Empty,
                    Location = location,
                    Description = $"Ожидался токен \"{desc}\" в конце объявления"
                });
                state = Transitions[state][firstExpected];
            }
        }

        return new ParseResult
        {
            Success = errors.Count == 0,
            Errors = errors
        };
    }

    private static string? TryInsertSingleToken(State state, Token actualToken, TokenDictionary dictionary)
    {
        foreach (var expectedTok in ExpectedTokens[state])
        {
            if (expectedTok == actualToken)
                return null;
            if (Transitions[state].ContainsKey(expectedTok))
            {
                var nextState = Transitions[state][expectedTok];
                if (ExpectedTokens[nextState].Contains(actualToken))
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
            if (dictionary.GetDescription(tok) == description)
                return tok;
        }
        return Token.Unknown;
    }

    private static string FormatLocation(LexerNode token)
    {
        return $"строка {token.Line}, позиция {token.WordStart + 1}";
    }

    private static string GetTokenText(LexerNode token)
    {
        return token.WordCurrent ?? string.Empty;
    }

    private static string GetTokenDescription(LexerNode token, TokenDictionary dictionary)
    {
        return dictionary.GetDescription(token.TokenCurrent) ?? token.TokenCurrent.ToString();
    }
}