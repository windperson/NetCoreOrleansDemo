using System;
using System.Threading.Tasks;
using Orleans;

namespace ShareInterface
{
    public struct HelloMyValue
    {
        public string Greeting;
        public DateTime YellTime;

        public override string ToString()
        {
            return $"{{Greeting={Greeting}, YellTime={YellTime.ToString("O")}}}";
        }
    }

    public interface IValueTaskDemo : IGrainWithGuidKey
    {
        ValueTask<HelloMyValue> Alarm();
    }
}