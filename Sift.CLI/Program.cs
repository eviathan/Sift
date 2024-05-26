namespace Sift.CLI
{
    public class Program
    {
        public static App App;

        public static async Task Main(string[] args)
        {
            App = new App();
            await App.Init();

            while (App.IsRunning)
            {
                App.Update();
            }
        }
    }
}