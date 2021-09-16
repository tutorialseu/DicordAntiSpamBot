using System;
using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Net.Http;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace DicordAntiSpamBot
{
    class Program
    {
        private static DiscordSocketClient client;
        static async Task Main(string[] args)
        {
            //crete a new config object and set the message cache size to 200
            var config = new DiscordSocketConfig { MessageCacheSize = 200 };
            //init our discord client with our config obj
            client = new DiscordSocketClient(config);
            //subscribe to the log event
            client.Log += Log;
            //login using our token
            await client.LoginAsync(TokenType.Bot, EnvVariables.discordBotToekn);
            //start the bot
            await client.StartAsync();
            //subscribe to the MessageReceived event
            client.MessageReceived += MessageReceived;

            //keep the app running
            await Task.Delay(-1);
        }

        private static async Task MessageReceived(SocketMessage arg)
        {
            //log our message into the console
            Console.WriteLine($"{arg.Content}");
            //if the message received was sent by the bot then ignore it
            if (arg.Author.Id == client.CurrentUser.Id)
                return;

            //just a test ping command
            if (arg.Content == "Ping")
            {
                await arg.Channel.SendMessageAsync("Pong");
                return;
            }

            //create a new regex that will search for a URL pattern in our messages
            var myRegex = new Regex(@"\b(?:https?://|www\.)\S+\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            //create a new http client to make a post request
            HttpClient httpClient = new HttpClient();
            //prepare the post request values
            var postReqvalues = new Dictionary<string, string> {
                {"stricktness","0" },
                {"fast","true" }
            };
            // create a URL encoded content from our values
            var content = new FormUrlEncodedContent(postReqvalues);


            //for each URL we found in our message 
            foreach (Match match in myRegex.Matches(arg.Content))
            {
                //fix the string format, since we are sending it as an embeded variable in our URL
                string url = match.Value.Replace(":","%3A").Replace("/","%2F");
                //make a post request to IPQualityScore
                HttpResponseMessage response = await httpClient.PostAsync("https://ipqualityscore.com/api/json/url/"+EnvVariables.ipScoreKey+"/" + url, content);
                //read the response as a string
                string responseString = await response.Content.ReadAsStringAsync();
                //parse the response into a C# object
                Root parsedResponse = JsonConvert.DeserializeObject<Root>(responseString);
                //check if the link is safe
                if (parsedResponse.@unsafe == true||parsedResponse.suspicious||parsedResponse.risk_score<50)
                {
                    //if it's not then :
                    //shame the sender
                    await arg.Channel.SendMessageAsync($"Spam Found !{arg.Author.Username} bad boi!");
                    //delete the message
                    await arg.DeleteAsync();
                    //no need to go through the rest of the urls if one of them is unsafe
                    break;
                }
                //log the response
                Console.WriteLine(responseString);
            }

        }

        private static Task Log(LogMessage arg)
        {

            Console.WriteLine(arg.ToString());
            return Task.CompletedTask;
        }
    }

}
