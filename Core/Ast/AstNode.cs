using System.Collections.Generic;
using System.Text;

namespace Core.Ast;

public abstract class AstNode
{
    public abstract string ToTreeString(string indent = "", bool isLast = true);
}

public class ProgramNode : AstNode
{
    public List<ConstDeclNode> Declarations { get; set; } = new();

    public override string ToTreeString(string indent = "", bool isLast = true)
    {
        var sb = new StringBuilder();
        sb.AppendLine("ProgramNode");
        for (int i = 0; i < Declarations.Count; i++)
        {
            bool lastDecl = (i == Declarations.Count - 1);
            sb.Append(Declarations[i].ToTreeString(indent, lastDecl));
        }
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

    public override string ToTreeString(string indent = "", bool isLast = true)
    {
        var sb = new StringBuilder();
        var marker = isLast ? "└── " : "├── ";
        sb.AppendLine($"{indent}{marker}ConstDeclNode");

        var childIndent = indent + (isLast ? "    " : "│   ");

        if (Value != null)
        {
            sb.AppendLine($"{childIndent}├── name: \"{Name}\"");
            sb.AppendLine($"{childIndent}├── type: \"{Type}\"");
            sb.AppendLine($"{childIndent}└── value: \"{Value}\"");
        }
        else
        {
            sb.AppendLine($"{childIndent}├── name: \"{Name}\"");
            sb.AppendLine($"{childIndent}└── type: \"{Type}\"");
        }

        return sb.ToString();
    }
}