using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebSocketSharp;
using Newtonsoft;
using System.Text.RegularExpressions;
using WindowsInput.Native;

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

            Console.Error.WriteLine("Usage: {0} <DirectPoll URL>",
                System.Reflection.Assembly.GetExecutingAssembly().GetName().Name);

            Environment.Exit(1);
        }

        private static Regex _urlRegex = new Regex(@"(https?://)?directpoll.com/r\?([^$]*)", RegexOptions.Singleline);

        public static void Main(string[] args) {
            if(args.Length < 1) {
                HelpAndTerminate(null);
            }

            var urlMatch = _urlRegex.Match(args[0]);
            if(!urlMatch.Success || urlMatch.Groups.Count < 3) {
                HelpAndTerminate("Input URL does not look like a correct DirectPoll address");
            }

            string pollId = urlMatch.Groups[2].Value;

            using (var s = new WebSocket(string.Format("ws://directpoll.com/wsr?{0}", pollId),
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

        private static WindowsInput.InputSimulator _simulator = new WindowsInput.InputSimulator();

        private static void ProcessVotes(int questionId, int answerId, int totalCount, int delta) {
            while (delta-- > 0) {
                _simulator.Keyboard.KeyPress(ConvertAnswerIdToKeyCode(answerId));
            }
        }

        private static VirtualKeyCode ConvertAnswerIdToKeyCode(int answerId) {
            if(answerId >= 1 && answerId <= 10) {
                //Numeric range
                return VirtualKeyCode.VK_0 + answerId - 1;
            }
            else if(answerId > 10 && answerId <= 10 + 26) {
                //Alphabetic range
                return VirtualKeyCode.VK_A + answerId - 1;
            }
            else {
                Console.Error.WriteLine("Defaulting to SPACE character generation for answer #{0}", answerId);
                return VirtualKeyCode.SPACE;
            }
        }

    }
}
