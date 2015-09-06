namespace MultiPing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.NetworkInformation;
    using System.Text;
    using System.Timers;

    class Pinger
    {
        public Pinger(int interval)
        {
            _timer.Interval = interval;
            _timer.Elapsed += TimerOnElapsed;
        }

        private readonly Dictionary<string, CustomPingReply> _customPingReplies = new Dictionary<string, CustomPingReply>();
        private readonly Timer _timer = new Timer();
        private readonly object _consoleLock = new object();
        private const int Column = 20;
        private const int RowOffset = 5;

        private void TimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            PingHosts();
        }

        private void PingHosts()
        {
            PingOptions options = new PingOptions();

            string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
            byte[] buffer = Encoding.ASCII.GetBytes(data);

            foreach (var customPingReply in _customPingReplies)
            {
                customPingReply.Value.Ping.SendAsync(customPingReply.Value.Host, customPingReply.Value.Timeout, buffer, options,
                    customPingReply.Value);
            }
        }

        public void Execute(List<string> hosts, int timeout = 120)
        {
            Console.Clear();

            Console.WriteLine($"MultiPing running with Timeout: {timeout} Interval: {_timer.Interval}");

            CreateHeader();

            int i = 0;

            foreach (var host in hosts)
            {
                _customPingReplies[host] = new CustomPingReply();
                var customPingReply = _customPingReplies[host];
                customPingReply.Ping.PingCompleted += PingOnPingCompleted;
                customPingReply.Timeout = timeout;
                customPingReply.Index = i++;
                customPingReply.Host = host;
            }

            PingHosts();
            _timer.Enabled = true;
        }

        private void UpdateConsole(CustomPingReply customPingReply)
        {
            lock (_consoleLock)
            {
                var consoleDefaultColor = Console.ForegroundColor;
                const int pad = 19;                

                customPingReply.Count++;
                var value = string.Empty;
                switch (customPingReply.Reply?.Status)
                {
                    case IPStatus.Success:
                        Console.ForegroundColor = ConsoleColor.DarkGreen;
                        value = "Ok".PadRight(pad);
                        break;
                    case IPStatus.DestinationNetworkUnreachable:
                    case IPStatus.DestinationHostUnreachable:
                    case IPStatus.DestinationProtocolUnreachable:
                    case IPStatus.DestinationPortUnreachable:
                    case IPStatus.NoResources:
                    case IPStatus.BadOption:
                    case IPStatus.HardwareError:
                    case IPStatus.BadRoute:
                    case IPStatus.TtlExpired:
                    case IPStatus.TtlReassemblyTimeExceeded:
                    case IPStatus.ParameterProblem:
                    case IPStatus.SourceQuench:
                    case IPStatus.BadDestination:
                    case IPStatus.DestinationUnreachable:
                    case IPStatus.BadHeader:
                    case IPStatus.UnrecognizedNextHeader:
                    case IPStatus.IcmpError:
                    case IPStatus.DestinationScopeMismatch:
                    case IPStatus.Unknown:
                    case IPStatus.PacketTooBig:
                        Console.ForegroundColor = ConsoleColor.Red;
                        customPingReply.FailCount++;
                        value = "Failed".PadRight(pad);
                        break;
                    case IPStatus.TimeExceeded:
                    case IPStatus.TimedOut:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        customPingReply.TimeoutCount++;
                        value = "Timeout".PadRight(pad);
                        break;
                    default:
                        Console.ForegroundColor = ConsoleColor.Red;
                        break;
                }

                var i = customPingReply.Index;

                var currentColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.CursorLeft = 0;
                Console.CursorTop = RowOffset + i;
                var output = _customPingReplies.Keys.ElementAt(i);
                Console.Write(output.PadRight(pad));
                Console.ForegroundColor = currentColor;

                Console.CursorLeft = Column;
                Console.CursorTop = RowOffset + i;
                output = string.Format($"{value}");
                Console.Write(output.PadRight(pad));

                Console.CursorLeft = Column * 2;
                Console.CursorTop = RowOffset + i;
                output = string.Format($"{customPingReply.Reply?.RoundtripTime}");
                Console.Write(output.PadRight(pad));

                Console.CursorLeft = Column * 3;
                Console.CursorTop = RowOffset + i;
                output = string.Format($"{customPingReply.Count - customPingReply.FailCount - customPingReply.TimeoutCount}");
                Console.Write(output.PadRight(pad));

                Console.CursorLeft = Column * 4;
                Console.CursorTop = RowOffset + i;
                currentColor = Console.ForegroundColor;
                if (customPingReply.TimeoutCount > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                }
                output = string.Format($"{customPingReply.TimeoutCount}");
                Console.Write(output.PadRight(pad));
                Console.ForegroundColor = currentColor;

                Console.CursorLeft = Column * 5;
                Console.CursorTop = RowOffset + i;
                currentColor = Console.ForegroundColor;
                if (customPingReply.FailCount > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                }
                output = string.Format($"{customPingReply.FailCount}");
                Console.Write(output.PadRight(pad));
                Console.ForegroundColor = currentColor;

                Console.ForegroundColor = consoleDefaultColor;
                Console.WriteLine("");
                Console.WriteLine("");
            }
        }

        private void CreateHeader()
        {
            var currentColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.White;
            Console.CursorLeft = 0;
            Console.CursorTop = RowOffset - 2;
            Console.Write("Address");

            Console.CursorLeft = Column;
            Console.CursorTop = RowOffset - 2;
            Console.Write("Status");

            Console.CursorLeft = Column * 2;
            Console.CursorTop = RowOffset - 2;
            Console.Write("Time (ms)");

            Console.CursorLeft = Column * 3;
            Console.CursorTop = RowOffset - 2;
            Console.Write("Success #");

            Console.CursorLeft = Column * 4;
            Console.CursorTop = RowOffset - 2;
            Console.Write("Timeout #");

            Console.CursorLeft = Column * 5;
            Console.CursorTop = RowOffset - 2;
            Console.Write("Fail #");
            Console.ForegroundColor = currentColor;
        }

        private void PingOnPingCompleted(object sender, PingCompletedEventArgs pingCompletedEventArgs)
        {
            var customPingReply = (CustomPingReply) pingCompletedEventArgs.UserState;
            customPingReply.Reply = pingCompletedEventArgs.Reply;

            UpdateConsole(customPingReply);
        }
    }

    internal class CustomPingReply
    {
        public PingReply Reply { get; set; }
        public int TimeoutCount { get; set; }
        public int FailCount { get; set; }
        public int Count { get; set; }
        public int Timeout { get; set; }
        public string Host { get; set; }
        public Ping Ping { get; set; } = new Ping();
        public int Index { get; set; }
    }
}
