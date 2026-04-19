namespace Core.Parser;

public record ErrorInfo(
    string InvalidFragment,
    uint Line,
    uint Column,
    string Description
);