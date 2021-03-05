namespace ProBot
{
    class Program
    {
        // Main
        static void Main()
        {
            var bot = new ProBot();
            bot.RunAsync().GetAwaiter().GetResult();
        }
    }
}
