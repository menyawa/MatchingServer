using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MatchingServer.Source {
    /// <summary>
    /// マッチングサーバのクラス
    /// サーバは2つ以上立てないので静的クラスとして作成
    /// </summary>
    static class Server {
        /// <summary>
        /// 参考URL:https://qiita.com/Zumwalt/items/53797b0156ebbdcdbfb1
        /// </summary>
        /// <returns></returns>
        private static async Task Run() {
            //Httpリスナーを立ち上げ、クライアントからの接続を待つ
            var httpListener = new HttpListener();
            httpListener.Prefixes.Add("http://localhost:8000/ws/");
            httpListener.Start();
            var httpListenerContext = await httpListener.GetContextAsync();

            //クライアントからのリクエストがWebSocketでないなら閉じてエラーを返す
            if (httpListenerContext.Request.IsWebSocketRequest == false) {
                httpListenerContext.Response.StatusCode = 400;
                httpListenerContext.Response.Close();
                return;
            }

            //WebSocketでレスポンスを返却
            var httpListenerWebSocketContext = await httpListenerContext.AcceptWebSocketAsync(null);
            var webSocket = httpListenerWebSocketContext.WebSocket;

            //１０回のレスポンスを返却
            for (int index = 0; index < 10; index++) {
                //1回のレスポンスごとに2秒のウエイトを設定
                await Task.Delay(2000);

                //レスポンスのテストメッセージとして、現在時刻の文字列を取得
                var time = DateTime.Now.ToLongTimeString();

                //文字列をByte型に変換
                var buffer = Encoding.UTF8.GetBytes(time);
                var segment = new ArraySegment<byte>(buffer);

                //クライアント側に文字列を送信
                await webSocket.SendAsync(segment, WebSocketMessageType.Text,
                  true, CancellationToken.None);
            }

            //接続を閉じる
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure,
              "Done", CancellationToken.None);
        }

        private static async Task basicRun() {
            var tcpListener = new TcpListener(IPAddress.Loopback, 12345);
            tcpListener.Start();

            while (true) {
                using (var tcpClient = await tcpListener.AcceptTcpClientAsync())
                using (var stream = tcpClient.GetStream())
                using (var reader = new StreamReader(stream))
                using (var writer = new StreamWriter(stream)) {
                    //接続元を出力
                    Console.WriteLine(tcpClient.Client.RemoteEndPoint);

                    // ヘッダー部分を全部読んで捨てる
                    string line = await reader.ReadLineAsync();
                    while (string.IsNullOrWhiteSpace(line) == false) {
                        line = await reader.ReadLineAsync();
                        // 読んだ行を出力しておく
                        Console.WriteLine(line);
                    }

                    // レスポンスを返す
                    // ステータスライン
                    await writer.WriteLineAsync("HTTP/1.1 200 OK");
                    // ヘッダー部分
                    await writer.WriteLineAsync("Content-Type: text/plain; charset=UTF-8");
                    await writer.WriteLineAsync(); // 終わり
                                                   // これ以降ボディ
                    await writer.WriteLineAsync($"Hello Server!  ");
                }
            }
        }
    }
}
