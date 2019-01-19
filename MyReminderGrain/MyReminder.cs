using System;
using System.Threading.Tasks;
using ShareInterface;

namespace MyReminderGrain
{
    public class MyReminder : IValueTaskDemo
    {
        private readonly IOutputMsg _outputMsg;

        public MyReminder(IOutputMsg outputMsg)
        {
            _outputMsg = outputMsg;
        }

        public ValueTask<HelloMyValue> Alarm()
        {
            var output = new HelloMyValue {Greeting = "Times UP!", YellTime = DateTime.Now};
            _outputMsg.Output(output.Greeting);
            return new ValueTask<HelloMyValue>(output);
        }
    }
}
