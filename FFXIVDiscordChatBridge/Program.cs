using FFXIVDiscordChatBridge.Consumer;
using FFXIVDiscordChatBridge.Producer;
using NLog;
using NLog.Fluent;

namespace FFXIVDiscordChatBridge
{
    static class Program
    {
        private static string DiscordChannelID;
        private static string DiscordToken;

        private static Logger logger;

        [STAThread]
        static async Task Main()
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(AppDomain_CurrentDomain_UnhandledException);

            logger = LogManager.GetCurrentClassLogger();
            logger.Info("Starting FFXIVDiscordChatBridge");
        
            var args = Environment.GetCommandLineArgs();
            var parameters = args
                .Where(arg => arg.StartsWith("--"))
                .Select(arg => arg.Split('='))
                .ToDictionary(arg => arg[0], arg => arg[1]);
            
            if (!parameters.ContainsKey("--discordChannelID"))
            {
                throw new Exception("Missing --discordChannelID parameter");
            }
            if (!parameters.ContainsKey("--discordToken"))
            {
                throw new Exception("Missing --discordToken parameter");
            }
            DiscordChannelID = parameters["--discordChannelID"];
            DiscordToken = parameters["--discordToken"];

            // setup discord singleton
            var discordWrapper = new DiscordClientWrapper(DiscordToken, DiscordChannelID);
            await discordWrapper.Initialize();
            
            // setup producers
            var ffxivProducer = new Producer.FFXIV();
            var discordProducer = new Producer.Discord(discordWrapper);
        
            // setup consumers            
            var ffxivConsumer = new Consumer.FFXIV(async (message) =>
            {
                await discordProducer.Send(message);
            });
            var discordConsumer = new Consumer.Discord(discordWrapper);
        
            // start consuming messages from FFXIV
            var ffxivConsumerTask = ffxivConsumer.Start();

            await Task.Delay(-1);
        }
    
        static void AppDomain_CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            logger.Error(e.ExceptionObject);
        }
    }
}