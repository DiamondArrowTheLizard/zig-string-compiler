using System.Collections.Generic;
using Core.Ast;

namespace Core.Semantic;

public class SemanticAnalyzer
{
    public SemanticResult Analyze(ProgramNode program)
    {
        var result = new SemanticResult { Success = true };
        var declaredIdentifiers = new Dictionary<string, ConstDeclNode>();
        var validDeclarations = new List<ConstDeclNode>();

        foreach (var decl in program.Declarations)
        {
            bool hasError = false;

            if (declaredIdentifiers.ContainsKey(decl.Name))
            {
                result.Errors.Add(new SemanticError
                {
                    Location = $"строка {decl.Line}, позиция {decl.Position}",
                    Description = $"Семантическая ошибка: Переобъявление идентификатора '{decl.Name}'"
                });
                hasError = true;
            }
            else
            {
                declaredIdentifiers[decl.Name] = decl;
            }

            if (decl.Value == null)
            {
                result.Errors.Add(new SemanticError
                {
                    Location = $"строка {decl.Line}, позиция {decl.Position}",
                    Description = $"Семантическая ошибка: Константа '{decl.Name}' должна быть инициализирована"
                });
                hasError = true;
            }

            if (!hasError)
            {
                validDeclarations.Add(decl);
            }
        }

        program.Declarations = validDeclarations;
        result.Success = result.Errors.Count == 0;
        return result;
    }
}