using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using WebSocketSharp;
using WindowsInput;
using WindowsInput.Native;

namespace DirectPollMonitor {

    public class Program {

        private static void HelpAndTerminate(string error) {
            if (!string.IsNullOrEmpty(error)) {
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
            if (args.Length < 1) {
                HelpAndTerminate(null);
            }

            var urlMatch = _urlRegex.Match(args[0]);
            if (!urlMatch.Success || urlMatch.Groups.Count < 3) {
                HelpAndTerminate("Input URL does not look like a correct DirectPoll address");
            }

            string pollId = urlMatch.Groups[2].Value;
            string wsUrl = string.Format("ws://directpoll.com/wsr?{0}", pollId);

            using (var s = new WebSocket(wsUrl,
                CancellationToken.None, 102392, null, OnClose, OnMessage, OnError)) {
                s.Origin = "http://directpoll.com";

                Console.WriteLine("Opening connection to {0}...", wsUrl);

                var connection = s.Connect();
                connection.Wait();

                if (!connection.Result) {
                    HelpAndTerminate("Connect failed");
                    return;
                }

                Console.WriteLine("Running (press ANY key to terminate)...");
                Console.Read();
            }
        }

        private static Task OnClose(CloseEventArgs args) {
            Console.WriteLine("Socket closed");

            return Task.FromResult<object>(null);
        }

        private static Regex _answerRegex = new Regex(@"q([0-9]*)a([0-9]*)", RegexOptions.Singleline | RegexOptions.Compiled);

        private static async Task OnMessage(MessageEventArgs args) {
            if (args.Opcode == Opcode.Text) {
                var response = await args.Text.ReadToEndAsync();

                if ("\"hi\"".Equals(response, StringComparison.InvariantCultureIgnoreCase)) {
                    //This is a keepalive message
                    return;
                }

#if DEBUG
                Console.WriteLine(response);
#endif

                try {
                    ProcessPayload(JObject.Parse(response));
                }
                catch (Exception ex) {
                    Console.Error.WriteLine("Message contains invalid JSON ({0})", ex);
                    return;
                }
            }
        }

        private static int _lastStatus = -1;

        private static void ProcessPayload(JObject payload) {
            //Process answers
            var answers = payload["a"] as JObject;
            if (answers != null) {
                var newVotes = ExtractVotes(answers);

                MatchVotes(_lastVotes, newVotes);
                
                _lastVotes = newVotes;
            }

            //Process status change
            var status = payload["s"] as JValue;
            if (status != null) {
                var newStatus = status.Value<int>();
                if (newStatus != _lastStatus) {
                    Console.WriteLine("New poll status {0}", newStatus);
                    _lastStatus = newStatus;
                }
            }
        }

        private static Dictionary<string, int> ExtractVotes(JToken payload) {
            return payload.Children().Select(
                a => a as JProperty).ToDictionary(p => p.Name, p => (int)p.Value);
        }

        private static void MatchVotes(IDictionary<string, int> old, IDictionary<string, int> update) {
            if(old == null) {
                //First batch of votes we get, ignore
                return;
            }

            foreach(var answer in update) {
                int prevCount = 0;
                if(old.ContainsKey(answer.Key)) {
                    prevCount = old[answer.Key];
                }

                int delta = answer.Value - prevCount;

                var answerMatch = _answerRegex.Match(answer.Key);
                if(!answerMatch.Success) {
                    throw new ArgumentException($"Cannot parse answer {answer.Key}");
                }

                int questionIndex = Convert.ToInt32(answerMatch.Groups[1].Value);
                int answerIndex = Convert.ToInt32(answerMatch.Groups[2].Value);

                ProcessVotes(questionIndex, answerIndex, answer.Value, delta);
            }
        }

        private static Task OnError(ErrorEventArgs args) {
            Console.WriteLine("Socket error: {0}", args.Message);
            Console.WriteLine("{0}", args.Exception);

            return Task.FromResult<object>(null);
        }

        private static Dictionary<string, int> _lastVotes = null;

        private static InputSimulator _simulator = new InputSimulator();

        private static void ProcessVotes(int questionId, int answerId, int totalCount, int delta) {
            var keyCode = ConvertAnswerIdToKeyCode(answerId);

            if (delta > 0) {
                Console.WriteLine("Q{0} A{1} Votes: {2} (+{3}) => {4}",
                    questionId, answerId, totalCount, delta, keyCode);
            }

            while (delta-- > 0) {
                _simulator.Keyboard.KeyPress(keyCode);
            }
        }

        private static VirtualKeyCode ConvertAnswerIdToKeyCode(int answerId) {
            if (answerId >= 1 && answerId <= 10) {
                //Numeric range
                return VirtualKeyCode.VK_0 + answerId - 1;
            }
            else if (answerId > 10 && answerId <= 10 + 26) {
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
