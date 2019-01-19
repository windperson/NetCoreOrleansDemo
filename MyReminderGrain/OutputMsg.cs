using Microsoft.Extensions.Logging;

namespace MyReminderGrain
{
    public class OutputMsg : IOutputMsg
    {
        private readonly ILogger<MyReminder> _logger;

        public OutputMsg(ILogger<MyReminder> logger)
        {
            _logger = logger;
        }

        public void Output(string msg)
        {
            _logger.LogInformation(msg);
        }
    }

    public interface IOutputMsg
    {
        void Output(string msg);
    }
}