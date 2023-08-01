namespace SierraSam.Core;

public static class ColorConsole
{
    private const ConsoleColor SuccessColor = ConsoleColor.Green;

    private const ConsoleColor WarningColor = ConsoleColor.Yellow;

    private const ConsoleColor ErrorColor = ConsoleColor.Red;

    private static void WriteLine(string message, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    public static void WriteLine(string message) => Console.WriteLine(message);


    public static void SuccessLine(string message) => WriteLine(message, SuccessColor);

    public static void WarningLine(string message) => WriteLine(message, WarningColor);

    public static void ErrorLine(string message) => WriteLine(message, ErrorColor);

}