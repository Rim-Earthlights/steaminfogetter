using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace SteamInfoGetter
{
    public static class webWrapper
    {
        public static CookieContainer cc = new CookieContainer();

        /// <summary>
        /// POST処理
        /// </summary>
        /// <param name="postURL">postするurl</param>
        /// <param name="postQuery">送信するクエリ</param>
        /// <returns>レスポンスのhtml</returns>
        public static async Task<string> PostAsync(string postURL, Dictionary<string, string> postQuery)
        {
            var postData = new FormUrlEncodedContent(postQuery);
            string getHTML = null;
            using (var handler = new HttpClientHandler())
            {
                handler.CookieContainer = cc;
                using (var client = new HttpClient(handler))
                {
                    client.Timeout = TimeSpan.FromSeconds(15);
                    var s = await client.PostAsync(postURL, postData);
                    getHTML = await s.Content.ReadAsStringAsync();
                }
                cc = handler.CookieContainer;
            }
            return getHTML;
        }

        /// <summary>
        /// get処理
        /// </summary>
        /// <param name="url">getするurl</param>
        /// <returns>レスポンスのhtml</returns>
        public static async Task<string> GetAsync(string url)
        {
            string getHTML;
            using (var handler = new HttpClientHandler())
            {
                handler.CookieContainer = cc;
                using (var client = new HttpClient(handler))
                {
                    client.Timeout = TimeSpan.FromSeconds(15);
                    var s = await client.GetAsync(url);
                    getHTML = await s.Content.ReadAsStringAsync();
                }
            }
            return getHTML;
        }

        public static void parseJson(string jsonString)
        {
            JObject jsonObj = JObject.Parse(jsonString);
            var appList = new List<AppList>();
            foreach(var obj in jsonObj)
            {
                
            }
        }

        /// <summary>
        /// HTMLデータをファイルに書き込みます。
        /// </summary>
        /// <param name="html">htmlデータ</param>
        /// <param name="outPath">保存するファイルパス</param>
        public static void WriteHtml(string html, string outPath)
        {
            using (FileStream fs = new FileStream(outPath, FileMode.Create, FileAccess.Write))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.Write(html);
                    sw.Close();
                }
            }
        }
    }
}
