using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Miller_Craft_Tools
{
    //write a class named AppTalker that contains a public method named SendMessgage that takes a string parameter named message and returns a string result.  This class should also implement a singleton pattern with all the associated properties and a private constructor.
    public class AppTalker
    {
        private static AppTalker? instance;
        private static readonly HttpClient httpClient = new HttpClient();
        private const string serverUrl = "http://85.31.232.195:3000";
        private AppTalker() { }
        public static AppTalker Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new AppTalker();
                }
                return instance;
            }
        }
        public async Task<string> SendMessageAsync(string message)
        {
            var content = new StringContent(message, Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(serverUrl, content);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
    }

}
