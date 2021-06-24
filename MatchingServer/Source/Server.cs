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
        private static readonly List<Lobby> LOBBYS;

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

            //現在プレイヤーがいるルームのID
            int currentRoomIndex = -1;
            while (true) {
                //意図的に0.5秒間隔で行う
                await Task.Delay(500);

                var messageData = JsonSerializer.Deserialize<MessageData>(await getReceiveMessageAsync(webSocket));
                switch (messageData.type_) {
                    case MessageData.Type.Join:
                        currentRoomIndex = getDefaultLobby().joinPlayer(messageData.PLAYER_ID, messageData.PLAYER_NICK_NAME, messageData.MAX_PLAYER_COUNT);
                        break;

                    case MessageData.Type.Leave:
                        //ルームに入る→退室するという順番でないと、当然ながらエラーが出るので注意
                        getDefaultLobby().leavePlayer(messageData.PLAYER_ID, currentRoomIndex);
                        break;

                    case MessageData.Type.Disconnect:
                        //切断要請があり次第切断する
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure,
              "Done", CancellationToken.None);
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
        /// デフォルトのロビーを返す
        /// </summary>
        /// <returns></returns>
        public static Lobby getDefaultLobby() {
            return LOBBYS.First();
        }
    }
}
