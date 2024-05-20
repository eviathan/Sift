using Sift;

public class Program
{
    public static App App;

    public static async Task Main(string[] args)
    {
        App = new App(args);
        await App.Init();

        while (App.IsRunning)
        {
            App.Update();
        }
    }
}