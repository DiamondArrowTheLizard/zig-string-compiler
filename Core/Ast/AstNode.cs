using System.Collections.Generic;
using System.Text;

namespace Core.Ast;

public abstract class AstNode
{
    public abstract string ToJsonString(int indentLevel = 0);
}

public class ProgramNode : AstNode
{
    public List<ConstDeclNode> Declarations { get; set; } = new();

    public override string ToJsonString(int indentLevel = 0)
    {
        var indent = new string(' ', indentLevel * 2);
        var nextIndent = new string(' ', (indentLevel + 1) * 2);
        var sb = new StringBuilder();
        sb.AppendLine($"{indent}{{");
        sb.AppendLine($"{nextIndent}\"Node\": \"ProgramNode\",");
        sb.AppendLine($"{nextIndent}\"Declarations\": [");
        for (int i = 0; i < Declarations.Count; i++)
        {
            sb.Append(Declarations[i].ToJsonString(indentLevel + 2));
            if (i < Declarations.Count - 1)
            {
                sb.AppendLine(",");
            }
            else
            {
                sb.AppendLine();
            }
        }
        sb.AppendLine($"{nextIndent}]");
        sb.Append($"{indent}}}");
        return sb.ToString();
    }
}

public class ConstDeclNode : AstNode
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Value { get; set; }
    public uint Line { get; set; }
    public uint Position { get; set; }

    public override string ToJsonString(int indentLevel = 0)
    {
        var indent = new string(' ', indentLevel * 2);
        var nextIndent = new string(' ', (indentLevel + 1) * 2);
        var sb = new StringBuilder();
        sb.AppendLine($"{indent}{{");
        sb.AppendLine($"{nextIndent}\"Node\": \"ConstDeclNode\",");
        sb.AppendLine($"{nextIndent}\"Name\": \"{Name}\",");
        sb.AppendLine($"{nextIndent}\"Type\": \"{Type}\",");
        sb.Append($"{nextIndent}\"Value\": \"{Value ?? "null"}\"");
        sb.AppendLine();
        sb.Append($"{indent}}}");
        return sb.ToString();
    }
}