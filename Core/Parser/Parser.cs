using Core.Lexer;
using System.Collections.Generic;

namespace Core.Parser;

public enum State
{
    S0, S1, S2, S3, S4, S5, S6, S7, S8, S9, S10, S11,
    Accept,
    Error
}

public class Parser
{
    
    private static readonly Dictionary<(State, Token), State> TransitionTable = new()
    {
        { (State.S0, Token.Const), State.S1 },
        { (State.S1, Token.Id), State.S2 },
        { (State.S2, Token.Colon), State.S3 },
        { (State.S3, Token.BracesOpen), State.S4 },
        { (State.S4, Token.BracesClose), State.S5 },
        { (State.S5, Token.Const), State.S6 },
        { (State.S6, Token.U8), State.S7 },
        { (State.S7, Token.Equals), State.S8 },
        { (State.S7, Token.Semicolon), State.Accept },
        { (State.S8, Token.Quote), State.S9 },
        { (State.S9, Token.Content), State.S10 },
        { (State.S9, Token.Quote), State.S11 },
        { (State.S10, Token.Quote), State.S11 },
        { (State.S11, Token.Semicolon), State.Accept },
    };


    public IReadOnlyList<ErrorInfo> Parse(IReadOnlyList<LexerNode> tokens)
    {
        var ctx = new ParserContext(tokens);
        var state = State.S0;

        while (!ctx.IsAtEnd && state != State.Accept)
        {
            var token = ctx.PeekToken();
            if (token == null) break;

            if (TransitionTable.TryGetValue((state, token.Value), out var nextState))
            {
                ctx.Advance();
                state = nextState;
            }
            else
            {
                ctx.Recover(state);
                if (ctx.IsAtEnd) break;
                state = DetermineStateAfterRecovery(state, ctx.PeekToken());
            }
        }

        if (state != State.Accept && !ctx.IsAtEnd)
        {
            ctx.ReportError("Незаконченное объявление.");
        }

        return ctx.Errors;
    }

    private State DetermineStateAfterRecovery(State fromState, Token? nextToken)
    {
        return fromState switch
        {
            State.S0 => State.S1,
            State.S1 => State.S2,
            State.S2 => State.S3,
            State.S3 => State.S4,
            State.S4 => State.S5,
            State.S5 => State.S6,
            State.S6 => State.S7,
            State.S7 => nextToken == Token.Semicolon ? State.Accept : State.S8,
            State.S8 => nextToken == Token.Quote ? State.S10 : State.S9,
            State.S9 => State.Accept,
            State.S10 => State.S11,
            State.S11 => State.Accept,
            _ => State.Error
        };
    }
}