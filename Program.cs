using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;

namespace SteamInfoGetter {
    class Program {
        static string _text = null;

        static async Task Main() {
            do {
                Console.Write("Game Name >");
                var inputGameName = Console.ReadLine();

                // uriにするためencodeを挟む
                var encodedgameName = HttpUtility.HtmlEncode(inputGameName);

                // クッキーつけて検索urlへget送信
                webWrapper.cc = new System.Net.CookieContainer();
                var html = await webWrapper.GetAsync("https://store.steampowered.com/search/?term=" + encodedgameName);

                // レスポンスをパースして中身を取り出す
                var parser = new HtmlParser();
                var doc = await parser.ParseDocumentAsync(html);
                var elements = doc.QuerySelectorAll(".search_result_row");

                string gameUrl = null;

                Console.WriteLine();

                // 結果からゲーム名だけ抽出して一致するまで回す
                foreach (var element in elements) {
                    var gameName = element.QuerySelector(".responsive_search_name_combined .title").InnerHtml;
                    Console.WriteLine(" * " + gameName);
                    if (inputGameName.ToUpper() == gameName.ToUpper()) {
                        gameUrl = element.GetAttribute("href");
                        break;
                    }
                    /**
                    Console.Write("\nCollect Game? (Y/N): ");
                    string ans = Console.ReadLine().ToUpper();
                    Console.WriteLine();
                    if (ans == "Y" || ans == "") {
                        gameUrl = element.GetAttribute("href");
                        break;
                    }
                    */
                }

                Console.WriteLine();

                // 見つからなかったら検索の一番上から順に聞いていく
                // ミスタイプだったりするとめんどくさいので[C]でbreakする
                if (gameUrl == null) {
                    Console.WriteLine("not found game name;");
                    foreach (var element in elements) {
                        var gameName = element.QuerySelector(".responsive_search_name_combined .title").InnerHtml;
                        Console.WriteLine(" * " + gameName);
                        Console.Write(Environment.NewLine + "Collect Game? ([Y]es/[N]o/[C]ancel): ");
                        string ans = Console.ReadLine().ToUpper();
                        Console.WriteLine();
                        if (ans.ToUpper() == "Y" || ans == "") {
                            gameUrl = element.GetAttribute("href");
                            break;
                        }
                        else if (ans.ToUpper() == "C") {
                            gameUrl = null;
                            break;
                        }
                    }
                    if (gameUrl == null) {
                        Console.WriteLine("not found game name;");
                        continue;
                    }
                }

                // ゲームが特定出来たらストアページをgetする
                html = await webWrapper.GetAsync(gameUrl);
                doc = await parser.ParseDocumentAsync(html);

                try {
                    // 年齢確認に引っかかった場合
                    if (doc.GetElementById("ageDay") != null) {
                        // ~$ curl 'https://store.steampowered.com/app/980830/Spirit_Hunter_Death_Mark/?snr=1_7_7_151_150_1'
                        var script = doc.QuerySelectorAll(".game_page_background > script")[1].InnerHtml;
                        // var g_sessionID = "14fbd310060d8409759facd0";
                        var sessionId = Regex.Match(script, @"var g_sessionID = "".+""").Value;
                        sessionId = Regex.Match(sessionId, @"(?<="").*?(?="")").Value;
                        var appId = Regex.Match(gameUrl, @"/app/\d+/").Value;

                        var postUrl = "https://store.steampowered.com/agecheckset/" + appId;

                        // 年齢確認に必要なクエリを作ってPOSTする
                        var query = new Dictionary<string, string>() {
                            { "sessionid", sessionId },
                            { "ageDay", "1" },
                            { "ageMonth", "1" },
                            { "ageYear", "1991" },
                        };

                        var post = await webWrapper.PostAsync(postUrl, query);
                        if (post.Contains("success")) {
                            // 通ったら改めてget
                            html = await webWrapper.GetAsync(gameUrl);
                            doc = await parser.ParseDocumentAsync(html);
                        }
                        else {
                            throw new Exception("cant post ");
                        }
                    }

                    // 各種情報の取得
                    var gameName = doc.QuerySelector("#appHubAppName").InnerHtml;
                    var reviews = doc.QuerySelectorAll(".game_review_summary");
                    var reviewRecently = reviews[0].InnerHtml;

                    string price;
                    var purchases = doc.QuerySelectorAll(".game_area_purchase_game_wrapper");
                    IElement purchase;

                    purchase = purchases.Where(p => p.QuerySelector("h1").InnerHtml == "Buy " + gameName).FirstOrDefault();

                    if (purchase == null) {
                        purchase = purchases.First();
                    }

                    try {
                        price = purchase.QuerySelector(".game_purchase_price").InnerHtml.Trim('\t', '\n');
                    }
                    catch (NullReferenceException) {
                        // 割引かかってる場合は割引前の金額を取得する
                        price = purchase.QuerySelector("div .discount_original_price").InnerHtml.Trim('\t', '\n');
                    }

                    // タグ名整形
                    var tags = doc.QuerySelectorAll(".popular_tags > .app_tag");
                    var tagNames = tags.Where(x => x.InnerHtml.Trim('\t', '\n') != "+").Select(x => x.InnerHtml.Trim('\t', '\n')).ToList();

                    // スプレッドシートのサムネ取得用
                    var headerUrl = "image(\"" + doc.QuerySelector(".game_header_image_full").GetAttribute("src") + "\")";

                    // スプレッドシートのストアページ転送用
                    var hyperLink = String.Format("=hyperlink(\"{0}\", {1})", gameUrl, headerUrl);

                    var buyDate = DateTime.Now.ToString("yyyy/MM/dd");

                    // show info
                    Console.WriteLine("Header: {0}", hyperLink);
                    Console.WriteLine("Game Name: {0}", gameName);
                    Console.WriteLine("Price: {0}", price);
                    Console.WriteLine("Review: {0}", reviewRecently);
                    Console.WriteLine("Tags: {0}", String.Join(", ", tagNames));
                    // Console.WriteLine("getSource: NULL");
                    // Console.WriteLine("buyDate: {0}", buyDate);
                    // Console.WriteLine("buyNum: 1");
                    // Console.WriteLine("outNum: 0");

                    var printLine = new List<string>() {
                        hyperLink, // Thumbnail
                        gameName, // gamename
                        price, // price
                        reviewRecently, // review
                        String.Join(",", tagNames), // tags
                        "", // get source
                        buyDate,// buy date
                        1.ToString(),// buy num
                        0.ToString(),// out num
                    };

                    _text = String.Join('\t', printLine);

                    // クリップボードにコピーする
                    Thread thread = new Thread(ClipBoardSetter);
                    thread.SetApartmentState(ApartmentState.STA);
                    thread.Start();

                }
                catch (NullReferenceException) {
                    Console.WriteLine("Sorry, Bundle Page");
                }
                Console.WriteLine();
                Console.WriteLine();

            } while (true);
        }

        /// <summary>
        /// クリップボードにデータをコピーする
        /// </summary>
        private static void ClipBoardSetter() {
            Clipboard.SetData(DataFormats.Text, _text);
        }
    }
}
