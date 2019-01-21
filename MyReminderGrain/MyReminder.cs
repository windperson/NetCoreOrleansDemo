using System;
using System.Threading.Tasks;
using Orleans;
using ShareInterface;

namespace MyReminderGrain
{
    public class MyReminder : Grain, IValueTaskDemo
    {
        private readonly IOutputMsg _outputMsg;

        public MyReminder(IOutputMsg outputMsg)
        {
            _outputMsg = outputMsg;
        }

        public ValueTask<HelloMyValue> Alarm()
        {
            var output = new HelloMyValue { Greeting = "Hello World!", YellTime = DateTime.Now };
            _outputMsg.Output(output.Greeting);
            return new ValueTask<HelloMyValue>(output);
        }
    }
}
