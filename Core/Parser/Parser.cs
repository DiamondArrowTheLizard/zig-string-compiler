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
    private string lastExpected = "unchanged";

    private static readonly Dictionary<State, HashSet<Token>> ExpectedTokens = new()
    {
        [State.Start] = new() { Token.Const },
        [State.AfterConst] = new() { Token.Id },
        [State.AfterId] = new() { Token.Colon },
        [State.AfterColon] = new() { Token.BracesOpen },
        [State.AfterOpenBracket] = new() { Token.BracesClose, Token.ConstU8 },
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
        [State.AfterConst] = new() { [Token.Id] = State.AfterId },
        [State.AfterId] = new() { [Token.Colon] = State.AfterColon },
        [State.AfterColon] = new() { [Token.BracesOpen] = State.AfterOpenBracket },
        [State.AfterOpenBracket] = new() { [Token.BracesClose] = State.AfterCloseBracket, [Token.ConstU8] = State.AfterSecondConst },
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

    private static bool AreTokensCompatible(Token expected, Token actual)
    {
        if (expected == actual) return true;
        if ((expected == Token.Const || expected == Token.ConstU8) &&
            (actual == Token.Const || actual == Token.ConstU8))
            return true;
        return false;
    }

    public ParseResult Parse(IReadOnlyList<LexerNode> nodes, TokenDictionary dictionary)
    {
        var errors = new List<ParserError>();
        var significant = nodes.Where(n => n.TokenCurrent != Token.Space).ToList();
        int index = 0;
        State state = State.Start;
        lastExpected = "unchanged";

        while (index < significant.Count)
        {
            var token = significant[index];

            if (token.TokenCurrent == Token.Const && state != State.Start && state != State.AfterOpenBracket && state != State.AfterCloseBracket)
            {
                state = State.Start;
            }

            var expectedSet = ExpectedTokens[state];
            Token match = expectedSet.FirstOrDefault(t => AreTokensCompatible(t, token.TokenCurrent), Token.Unknown);

            if (match != Token.Unknown)
            {
                state = Transitions[state][match];
                index++;
                lastExpected = "unchanged";
                continue;
            }

            if (token.TokenCurrent == Token.Unknown || token.TokenCurrent == Token.UnknownNoConst)
            {
                string expectedDesc = string.Join(" или ", expectedSet.Select(t => $"\"{dictionary.GetDescription(t) ?? t.ToString()}\""));
                errors.Add(new ParserError
                {
                    Fragment = token.WordCurrent ?? string.Empty,
                    Location = $"строка {token.Line}, позиция {token.WordStart + 1}",
                    Description = $"Недопустимый символ \"{token.WordCurrent}\". Ожидался {expectedDesc}"
                });

                if (token.TokenCurrent == Token.UnknownNoConst && state == State.Start)
                    state = State.AfterId;

                lastExpected = expectedDesc;
                index++;
                continue;
            }

            string? insertedDescription = TryInsertSingleToken(state, token.TokenCurrent, dictionary);
            if (insertedDescription != null)
            {
                if (!lastExpected.Contains(insertedDescription.Replace("\"", "")))
                {
                    errors.Add(new ParserError
                    {
                        Fragment = string.Empty,
                        Location = $"строка {token.Line}, позиция {token.WordStart + 1}",
                        Description = $"Ожидался токен \"{insertedDescription}\""
                    });
                    lastExpected = insertedDescription;
                }
                state = Transitions[state][GetTokenByDescription(insertedDescription, dictionary)];
                continue;
            }

            string errExpectedDesc = string.Join(" или ", expectedSet.Select(t => $"\"{dictionary.GetDescription(t) ?? t.ToString()}\""));
            errors.Add(new ParserError
            {
                Fragment = token.WordCurrent ?? string.Empty,
                Location = $"строка {token.Line}, позиция {token.WordStart + 1}",
                Description = $"Неожиданный токен \"{dictionary.GetDescription(token.TokenCurrent) ?? token.TokenCurrent.ToString()}\", ожидался {errExpectedDesc}"
            });
            lastExpected = dictionary.GetDescription(token.TokenCurrent) ?? "null";
            index++;
        }

        while (state != State.Start && significant.Count > 0)
        {
            var lastToken = significant.Last();
            var expectedSet = ExpectedTokens[state];
            Token primaryExpected = expectedSet.First();
            string desc = dictionary.GetDescription(primaryExpected) ?? primaryExpected.ToString();

            if (!lastExpected.Contains(desc.Replace("\"", "")))
            {
                errors.Add(new ParserError
                {
                    Fragment = string.Empty,
                    Location = $"строка {lastToken.Line}, позиция {lastToken.WordEnd + 2}",
                    Description = $"Ожидался токен \"{desc}\""
                });
                lastExpected = desc;
            }

            if (Transitions[state].TryGetValue(primaryExpected, out var nextState))
                state = nextState;
            else
                break;
        }

        return new ParseResult { Success = errors.Count == 0, Errors = errors };
    }

    private static string? TryInsertSingleToken(State state, Token actualToken, TokenDictionary dictionary)
    {
        if (!Transitions.ContainsKey(state)) return null;
        foreach (var expectedTok in ExpectedTokens[state])
        {
            if (Transitions[state].TryGetValue(expectedTok, out var nextState))
            {
                if (ExpectedTokens[nextState].Any(t => AreTokensCompatible(t, actualToken)))
                    return dictionary.GetDescription(expectedTok) ?? expectedTok.ToString();
            }
        }
        return null;
    }

    private static Token GetTokenByDescription(string description, TokenDictionary dictionary)
    {
        foreach (Token tok in Enum.GetValues(typeof(Token)))
            if (dictionary.GetDescription(tok) == description) return tok;
        return Token.Unknown;
    }
}