using Core.Lexer;
using Core.Parser;
using System;
using System.IO;
using System.Linq;

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