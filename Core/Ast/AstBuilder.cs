using System.Collections.Generic;
using System.Linq;
using Core.Lexer;

namespace Core.Ast;

public class AstBuilder
{
    public ProgramNode Build(IReadOnlyList<LexerNode> nodes)
    {
        var program = new ProgramNode();
        var significant = nodes.Where(n => n.TokenCurrent != Token.Space).ToList();
        int i = 0;

        while (i < significant.Count)
        {
            if (significant[i].TokenCurrent == Token.Const)
            {
                var decl = new ConstDeclNode();
                i++;

                if (i < significant.Count && significant[i].TokenCurrent == Token.Id)
                {
                    decl.Name = significant[i].WordCurrent ?? string.Empty;
                    decl.Line = significant[i].Line;
                    decl.Position = significant[i].WordStart + 1;
                    i++;
                }

                if (i < significant.Count && significant[i].TokenCurrent == Token.Colon)
                {
                    i++;
                }

                string typeStr = string.Empty;
                while (i < significant.Count && 
                       significant[i].TokenCurrent != Token.Equals && 
                       significant[i].TokenCurrent != Token.Semicolon)
                {
                    if (significant[i].TokenCurrent == Token.U8 && typeStr.EndsWith("const"))
                    {
                        typeStr += " ";
                    }
                    typeStr += significant[i].WordCurrent;
                    i++;
                }
                decl.Type = typeStr;

                if (i < significant.Count && significant[i].TokenCurrent == Token.Equals)
                {
                    i++;
                    if (i < significant.Count && significant[i].TokenCurrent == Token.Quote)
                    {
                        i++;
                    }
                    
                    if (i < significant.Count && significant[i].TokenCurrent == Token.Content)
                    {
                        decl.Value = significant[i].WordCurrent;
                        i++;
                    }
                    else
                    {
                        decl.Value = string.Empty;
                    }

                    if (i < significant.Count && significant[i].TokenCurrent == Token.Quote)
                    {
                        i++;
                    }
                }

                if (i < significant.Count && significant[i].TokenCurrent == Token.Semicolon)
                {
                    i++;
                }

                program.Declarations.Add(decl);
            }
            else
            {
                i++;
            }
        }

        return program;
    }
}