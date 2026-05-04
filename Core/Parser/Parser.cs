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
        [State.AfterConst] = new() { Token.Id, Token.UnknownNoConst },
        [State.AfterId] = new() { Token.Colon },
        [State.AfterColon] = new() { Token.BracesOpen },
        [State.AfterOpenBracket] = new() { Token.BracesClose },
        [State.AfterCloseBracket] = new() { Token.Const },
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
            [Token.UnknownNoConst] = State.AfterId
        },
        [State.AfterId] = new() { [Token.Colon] = State.AfterColon },
        [State.AfterColon] = new() { [Token.BracesOpen] = State.AfterOpenBracket },
        [State.AfterOpenBracket] = new() { [Token.BracesClose] = State.AfterCloseBracket },
        [State.AfterCloseBracket] = new() { [Token.Const] = State.AfterSecondConst },
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

            var expectedSet = ExpectedTokens[state];
            if (expectedSet.Contains(token.TokenCurrent))
            {
                state = Transitions[state][token.TokenCurrent];
                index++;
                lastExpectedDescription = null;
                continue;
            }

            var recovery = FindRecoveryPath(state, token.TokenCurrent, dictionary);
            if (recovery != null)
            {
                foreach (var inserted in recovery.InsertedTokens)
                {
                    if (inserted != lastExpectedDescription)
                    {
                        errors.Add(new ParserError
                        {
                            Fragment = GetTokenText(token),
                            Location = FormatLocation(token),
                            Description = $"Ожидался токен \"{inserted}\""
                        });
                        lastExpectedDescription = inserted;
                    }
                }
                state = recovery.TargetState;
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

            string primaryExpected = expectedSet.First().ToString();
            lastExpectedDescription = dictionary.GetDescription(expectedSet.First()) ?? primaryExpected;

            index++;
            while (index < significant.Count)
            {
                var next = significant[index];
                if (SynchronizingTokens.Contains(next.TokenCurrent))
                    break;
                if (ExpectedTokens[state].Contains(next.TokenCurrent) ||
                    FindRecoveryPath(state, next.TokenCurrent, dictionary) != null)
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

    private static RecoveryInfo? FindRecoveryPath(State fromState, Token targetToken, TokenDictionary dictionary)
    {
        var queue = new Queue<(State state, List<string> inserted)>();
        var visited = new HashSet<State>();
        queue.Enqueue((fromState, new List<string>()));
        visited.Add(fromState);

        while (queue.Count > 0)
        {
            var (current, inserted) = queue.Dequeue();
            if (ExpectedTokens[current].Contains(targetToken))
            {
                return new RecoveryInfo
                {
                    TargetState = current,
                    InsertedTokens = inserted
                };
            }

            foreach (var expectedTok in ExpectedTokens[current])
            {
                if (Transitions[current].TryGetValue(expectedTok, out var next))
                {
                    if (!visited.Contains(next))
                    {
                        visited.Add(next);
                        var newInserted = new List<string>(inserted)
                        {
                            dictionary.GetDescription(expectedTok) ?? expectedTok.ToString()
                        };
                        queue.Enqueue((next, newInserted));
                    }
                }
            }
        }
        return null;
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

    private class RecoveryInfo
    {
        public State TargetState { get; set; }
        public List<string> InsertedTokens { get; set; } = new();
    }
}