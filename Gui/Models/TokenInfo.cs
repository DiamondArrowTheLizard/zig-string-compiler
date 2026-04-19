using Core.Lexer;

namespace Gui.Models;

public record TokenInfo(
    Token Type,
    string Description,
    string Value,
    uint Line,
    uint StartColumn,
    uint EndColumn
);