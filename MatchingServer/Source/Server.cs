using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MatchingServer {
    /// <summary>
    /// マッチングサーバのクラス
    /// サーバは2つ以上立てないので静的クラスとして作成
    /// </summary>
    static class Server {
        private static readonly Encoding ENCODING = Encoding.UTF8;
        private static readonly List<Lobby> LOBBYS = new List<Lobby> { new Lobby() };

        /// <summary>
        /// 参考URL:https://qiita.com/Zumwalt/items/53797b0156ebbdcdbfb1
        /// </summary>
        /// <returns></returns>
        public static async Task Run() {
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

            //応答なしの累計時間
            double noResponseTime = 0;
            var deltaTimer = new DeltaTimer();
            //現在プレイヤーがいるルームのID
            int currentRoomIndex = -1;
            //受信中の応答無しの時間を測定する関係上、awaitは使わない
            var clientMessageTask = getReceiveMessageAsync(webSocket);
            while (true) {
                //毎フレーム必ず更新を行う
                //最後でなく最初に更新を行わないと、途中でcontinueを行った場合更新が行われないので注意
                deltaTimer.update();

                //あまり頻繁に送受信してもあまり意味がないので、意図的に1秒間隔で通信を行う
                await Task.Delay(1000);

                //メッセージの受信完了したら新たなメッセージの受信待ちを開始し、応答無しの累計時間をリセットする
                var clientMessageData = MessageData.getBlankData();
                if (clientMessageTask.IsCompletedSuccessfully) {
                    //ここで受信データを入れておかないと、この後では既に新しい待受けのタスクに変わってしまっているため、正常にメッセージを受信できないことに注意
                    clientMessageData = JsonSerializer.Deserialize<MessageData>(await clientMessageTask);
                    clientMessageTask = getReceiveMessageAsync(webSocket);
                    noResponseTime = 0;
                } else {
                    //完了していないなら応答なしの時間を加算し、タイムアウト次第切断する
                    //まだタイムアウトしないなら次のループへ
                    noResponseTime += deltaTimer.get();
                    Console.WriteLine($"No Responce Time: {noResponseTime}");
                    if (isTimeOut(noResponseTime)) {
                        Console.WriteLine("Time Out");
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Time Out", CancellationToken.None);
                        break;
                    } else continue;
                }

                //クライアントからのメッセージに応じた処理を行う
                switch (clientMessageData.type_) {
                    case MessageData.Type.Join:
                        Console.WriteLine(clientMessageData.ToString());
                        currentRoomIndex = getDefaultLobby().joinPlayer(clientMessageData.PLAYER_ID, clientMessageData.PLAYER_NICK_NAME, clientMessageData.MAX_PLAYER_COUNT);
                        break;

                    case MessageData.Type.Leave:
                        //ルームに入る→退室するという順番でないと、当然ながらエラーが出るので注意
                        getDefaultLobby().leavePlayer(clientMessageData.PLAYER_ID, currentRoomIndex);
                        break;

                    case MessageData.Type.PeriodicReport:
                        break;
                    case MessageData.Type.Disconnect:
                        //切断要請があり次第切断する
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Done", CancellationToken.None);
                        break;
                }
            }
        }

        /// <summary>
        /// 指定されたWebsocketで、メッセージ(文字列)を送信する
        /// </summary>
        /// <param name="webSocket"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        private static void sendMessage(WebSocket webSocket, string message) {
            //文字列をバイト列に変換して送る
            var buffer = ENCODING.GetBytes(message);
            var segment = new ArraySegment<byte>(buffer);
            webSocket.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        /// <summary>
        /// 送られてきたメッセージをstringとして非同期で取得する
        /// 参考URL:https://qiita.com/Zumwalt/items/53797b0156ebbdcdbfb1
        /// </summary>
        /// <param name="webSocket"></param>
        /// <returns></returns>
        private static async Task<string> getReceiveMessageAsync(WebSocket webSocket) {
            var buffer = new byte[1024];
            //所得情報確保用の配列を準備
            var segment = new ArraySegment<byte>(buffer);
            //サーバからのレスポンス情報を取得
            var result = await webSocket.ReceiveAsync(segment, CancellationToken.None);

            //エンドポイントCloseの場合、処理を中断
            if (result.MessageType == WebSocketMessageType.Close) {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Already Close",
                  CancellationToken.None);
                return null;
            }
            //バイナリの場合は扱えないため、処理を中断
            if (result.MessageType == WebSocketMessageType.Binary) {
                await webSocket.CloseAsync(WebSocketCloseStatus.InvalidMessageType,
                  "I don't do binary", CancellationToken.None);
                return null;
            }

            //メッセージの最後まで取得
            int count = result.Count;
            while (result.EndOfMessage == false) {
                //バッファの長さをメッセージの長さが超えるようなら中断
                if (count >= buffer.Length) {
                    await webSocket.CloseAsync(WebSocketCloseStatus.InvalidPayloadData,
                      "That's too long", CancellationToken.None);
                    return null;
                }

                segment = new ArraySegment<byte>(buffer, count, buffer.Length - count);
                result = await webSocket.ReceiveAsync(segment, CancellationToken.None);

                count += result.Count;
            }

            return ENCODING.GetString(buffer, 0, count);
        }

        /// <summary>
        /// クライアントからの応答がタイムアウトしたかどうか
        /// </summary>
        /// <param name="noResponceTime"></param>
        /// <returns></returns>
        private static bool isTimeOut(double noResponceTime) {
            return noResponceTime >= 10.0;
        }

        /// <summary>
        /// デフォルトのロビーを返す
        /// </summary>
        /// <returns></returns>
        public static Lobby getDefaultLobby() {
            return LOBBYS.First();
        }
    }
}
