using Core.Lexer;
using Core.Parser;
using Core.Ast;
using Core.Semantic;
using System;
using System.IO;

string? line;
var lexer = new Lexer();

while ((line = Console.ReadLine()) != null)
{
    lexer.ParseLine(line);
}

var csv = new LexerCsv();
csv.Build(lexer);
csv.WriteToStream(Console.Out);

Console.WriteLine();

var parser = new Parser();
var result = parser.Parse(lexer.Nodes, lexer.Dictionary);

Console.WriteLine("--- Parser Results ---");
var csvP = new ParserCsv();
csvP.Build(result);
using var writerP = new StringWriter();
csvP.WriteToStream(writerP);
Console.WriteLine(writerP.ToString());

if (result.Success)
{
    var astBuilder = new AstBuilder();
    var programAst = astBuilder.Build(lexer.Nodes);

    var semanticAnalyzer = new SemanticAnalyzer();
    var semanticResult = semanticAnalyzer.Analyze(programAst);

    Console.WriteLine("--- AST Tree ---");
    Console.Write(programAst.ToTreeString());
    Console.WriteLine();

    Console.WriteLine("--- Semantic Results ---");
    if (semanticResult.Success)
    {
        Console.WriteLine("Семантический анализ завершен успешно. Ошибок не обнаружено.");
    }
    else
    {
        foreach (var error in semanticResult.Errors)
        {
            Console.WriteLine($"{error.Location}: {error.Description}");
        }
    }
}