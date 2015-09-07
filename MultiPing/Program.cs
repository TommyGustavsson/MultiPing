namespace MultiPing
{
    using System;
    using System.Collections.Generic;

    internal class Program
    {
        private static void Main(string[] args) {
            List<string> hosts = new List<string>();
            int interval = 2000;

            var key = string.Empty;

            int timeout = 150;

            foreach (var arg in args) {
                string value;
                if (arg.Contains("--")) {
                    key = arg;
                    value = string.Empty;
                }
                else {
                    value = arg;
                }

                switch (key) {
                    case "--hosts":
                        if (value.Length > 0) {
                            hosts.Add(value);
                        }
                        break;
                    case "--interval":
                        if (value.Length > 0) {
                            interval = int.Parse(value);
                        }
                        break;
                    case "--timeout":
                        if (value.Length > 0) {
                            timeout = int.Parse(value);
                        }
                        break;
                }
            }

            var pinger = new Pinger(interval);

            if (timeout > interval) {
                timeout = interval - 50;
            }

            if (hosts.Count == 0) {
                Console.WriteLine("No hosts defined.");
            }
            else {
                pinger.Execute(hosts, timeout);
            }
            
            bool terminated = false;
            while (!terminated)
            {
                var k = Console.ReadKey();
                if (k.Key == ConsoleKey.Escape) {
                    terminated = true;
                }
            }
        }
    }
}
