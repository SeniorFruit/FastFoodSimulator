using System.Windows;
using FastFoodSimulator.Resources;

namespace FastFoodSimulator;

public static class Program
{
    [STAThread]
    public static void Main()
    {
        var app = new Application();
        app.Resources.MergedDictionaries.Add(new Theme());

        app.Run(new MainWindow());
    }
}
