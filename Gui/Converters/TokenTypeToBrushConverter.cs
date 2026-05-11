using Avalonia.Data.Converters;
using Avalonia.Media;
using Core.Lexer;
using System;
using System.Globalization;

namespace Gui.Converters;

public class TokenTypeToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Token token)
        {
            return token switch
            {
                Token.Const or Token.U8 => Brushes.Blue,
                Token.Quote or Token.Content => Brushes.Brown,
                Token.Space => Brushes.Gray,
                Token.Colon or Token.Equals or Token.Semicolon => Brushes.DarkOrange,
                Token.BracesOpen or Token.BracesClose => Brushes.Purple,
                Token.Id => Brushes.Green,
                _ => Brushes.Red
            };
        }
        return Brushes.Black;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}