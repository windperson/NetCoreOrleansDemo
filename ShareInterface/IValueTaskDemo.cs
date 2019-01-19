using System;
using System.Threading.Tasks;

namespace ShareInterface
{
    public struct HelloMyValue
    {
        public string Greeting;
        public DateTime YellTime;
    }

    public interface IValueTaskDemo
    {
        ValueTask<HelloMyValue> Alarm();
    }
}