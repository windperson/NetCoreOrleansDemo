using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Orleans;
using Orleans.Runtime;
using ShareInterface;

namespace MyReminderGrain
{
    public class MyReminder : Grain, IValueTaskDemo, IRemindable
    {
        private readonly IOutputMsg _outputMsg;

        private Dictionary<string, ReminderInfo> _registeredReminders = new Dictionary<string, ReminderInfo>();

        private const int UpperLimit = 3;
        private int _calledTimes;

        public MyReminder(IOutputMsg outputMsg)
        {
            _outputMsg = outputMsg;
        }

        public async ValueTask<HelloMyValue> Alarm()
        {
            _calledTimes++;
            var reminderName = $"myReminder{_calledTimes}";
            var reminder = await RegisterOrUpdateReminder(reminderName, TimeSpan.FromMinutes(0), TimeSpan.FromMinutes(1));
            _registeredReminders[reminderName] = new ReminderInfo{Reminder = reminder};
            var output = new HelloMyValue { Greeting = "Hello World!", YellTime = DateTime.Now };
            _outputMsg.Output(output.Greeting);

            return output;
        }

        public async Task ReceiveReminder(string reminderName, TickStatus status)
        {
            var output = new HelloMyValue { Greeting = $"get Reminder: {reminderName}", YellTime = DateTime.Now };
            _outputMsg.Output(output.ToString());

            var reminderInfo = _registeredReminders[reminderName];

            reminderInfo.CalledCount++;

            if (reminderInfo.CalledCount >= UpperLimit)
            {
                await UnregisterReminder(reminderInfo.Reminder);
                _outputMsg.Output($"Reminder: {reminderName} unregistered.");
                _registeredReminders.Remove(reminderName);
            }
        }
    }
}
