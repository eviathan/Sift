using Sift;

public class Program
{
    public static App App = new App();

    public static async Task Main()
    {
        while(App.IsRunning)
        {
            App.Update();
        }
    }
}