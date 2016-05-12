using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebSocketSharp;
using Newtonsoft;

namespace DirectPollMonitor {

    public class Program {

        private static void HelpAndTerminate(string error) {
            if(!string.IsNullOrEmpty(error)) {
                var prevColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.Write("Error: ");
                Console.Error.WriteLine(error);
                Console.ForegroundColor = prevColor;
            }

            Console.Error.WriteLine("Usage: {0}.exe <DirectPoll URL>", System.Reflection.Assembly.GetExecutingAssembly().GetName().Name);

            Environment.Exit(1);
        }

        public static void Main(string[] args) {
            if(args.Length < 1) {
                HelpAndTerminate(null);
                return;
            }

            using (var s = new WebSocket(args[0],
                CancellationToken.None, 102392, null, OnClose, OnMessage, OnError)) {
                s.Origin = "http://directpoll.com";

                Console.WriteLine("Opening connection to {0}...", args[0]);

                var connection = s.Connect();
                connection.Wait();

                if (!connection.Result) {
                    HelpAndTerminate("Connect failed");
                    return;
                }

                using (Timer t = new Timer(OnTimerTick, s, 10000, 10000)) {
                    Console.WriteLine("Running (press ANY key to terminate)...");
                    Console.Read();
                }
            }
        }

        private static void OnTimerTick(object something) {
            WebSocket socket = (WebSocket)something;

            var sendTask = socket.Send("hi");
            sendTask.Wait();
            if(!sendTask.Result) {
                HelpAndTerminate("Periodic HI failed to send");
            }
        }

        private static Task OnClose(CloseEventArgs args) {
            Console.WriteLine("Socket closed");

            return Task.FromResult<object>(null);
        }

        private static async Task OnMessage(MessageEventArgs args) {
            if(args.Opcode == Opcode.Text) {
                var response = await args.Text.ReadToEndAsync();
                
            }
        }

        private static Task OnError(ErrorEventArgs args) {
            Console.WriteLine("Socket error: {0}", args.Message);
            Console.WriteLine("{0}", args.Exception);

            return Task.FromResult<object>(null);
        }

        private static int[] _lastVotes = null;

        private static void HandleVotesUpdate(string json) {

        }

    }
}
