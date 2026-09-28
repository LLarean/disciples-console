using System.Text;
using Spectre.Console;

Console.OutputEncoding = Encoding.UTF8;

AnsiConsole.Write(new FigletText("Disciples").Color(Color.Gold1));
AnsiConsole.Write(new Panel("[grey]Console prototype. Scaffold only (M0).[/]")
    .Header("[gold1]Disciples Console[/]")
    .Border(BoxBorder.Double)
    .BorderColor(Color.Gold3));
AnsiConsole.MarkupLine("[grey]Press any key to exit...[/]");
Console.ReadKey(true);
