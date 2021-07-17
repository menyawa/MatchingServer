using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private static readonly HttpListener HTTP_LISTENER;
        private static readonly List<Lobby> LOBBYS = new List<Lobby> { new Lobby() };
        //メッセージの送信・復号に用いるエンコーディング
        private static readonly Encoding ENCODING = Encoding.UTF8;
        //無効な場合のIDの値(リセットに用いる)
        private const int INVAID_ID = -1;

        static Server() {
            //接続待ちは別で行う
            Debug.WriteLine("HttpListenerを初期化します");
            HTTP_LISTENER = new HttpListener();
            HTTP_LISTENER.Prefixes.Add("http://localhost:8000/ws/");
            HTTP_LISTENER.Start();
        }

        /// <summary>
        /// クライアントとの接続を行っているWebSocketを受け取り、各種作業を行う
        /// クライアントの接続ごとに1つずつこのメソッドが回る
        /// 参考URL:https://qiita.com/Zumwalt/items/53797b0156ebbdcdbfb1
        /// </summary>
        /// <returns></returns>
        public static async Task RunAsync(WebSocket webSocket) {
            Debug.WriteLine("メッセージの送受信開始");

            //応答なしの時間を測るため、ストップウォッチを用意して開始
            var noResponseTimeStopwatch = new Stopwatch();
            noResponseTimeStopwatch.Start();

            //このメソッドで担当するプレイヤーのIDと、現在プレイヤーがいるルームのindex
            string playerID = INVAID_ID.ToString();
            int currentRoomIndex = INVAID_ID;
            //受信中の応答無しの時間を測定する関係上、awaitは使わない
            var getClientMessageTask = Task.Run(() => getReceiveMessageAsync(webSocket));
            //今後同期を行うことも考え、Task.Delayでの通信遅延は行わず毎フレーム更新を行う
            while (webSocket.State != WebSocketState.Closed && webSocket.State != WebSocketState.CloseReceived) {
                var clientMessageData = MessageData.getBlankData();
                if (getClientMessageTask.IsCompletedSuccessfully) {
                    //ここで受信データをキャッシュしておかないと、この後では既に新しい待受けのタスクに変わってしまっているため、正常にメッセージを受信できないことに注意
                    var temp = getClientMessageTask.Result;
                    clientMessageData = JsonSerializer.Deserialize<MessageData>(temp);
                    //メッセージの受信が完了したら新たな受信待ちを開始しないと次のメッセージが受け取れないことに注意
                    getClientMessageTask = Task.Run(() => getReceiveMessageAsync(webSocket));
                    
                    //応答があった時点で応答なしの時間はリセットしておく
                    noResponseTimeStopwatch.Restart();
                } else {
                    if (isTimeOut(noResponseTimeStopwatch.Elapsed.TotalSeconds)) {
                        await closeAsync(webSocket, "タイムアウト", playerID);
                        return;
                    } else continue;
                }

                //メッセージを受信しているならそれに応じた入室、退室等の処理を行う
                //ここを非同期で実行してしまうと前回のメッセージの処理が終わらないうちに次のメッセージの処理が始まる危険性があるので注意
                currentRoomIndex = runByClientMessageProgress(webSocket, clientMessageData, currentRoomIndex);
            }
        }

        /// <summary>
        /// クライアントからのメッセージに応じた処理を行う
        /// TODO:もうちょっといい名前を考える
        /// </summary>
        /// <param name="webSocket"></param>
        /// <param name="messageData"></param>
        /// <param name="currentRoomIndex"></param>
        /// <returns></returns>
        private static int runByClientMessageProgress(WebSocket webSocket, MessageData messageData, int currentRoomIndex) {
            switch (messageData.type_) {
                case MessageData.Type.Join:
                    currentRoomIndex = getDefaultLobby().joinPlayer(messageData.PLAYER_ID, messageData.PLAYER_NICK_NAME, webSocket, messageData.MAX_PLAYER_COUNT);
                    break;

                case MessageData.Type.Leave:
                    //ルームに入る→退室するという順番でないと、当然ながらエラーが出るので注意
                    getDefaultLobby().leavePlayer(messageData.PLAYER_ID, currentRoomIndex);
                    currentRoomIndex = INVAID_ID;
                    break;

                case MessageData.Type.PeriodicReport:
                    break;
                case MessageData.Type.Disconnect:
                    //切断要請があり次第切断する
                    Task.Run(() => closeAsync(webSocket, "通常終了", messageData.PLAYER_ID));
                    currentRoomIndex = INVAID_ID;
                    break;
            }
            return currentRoomIndex;
        }

        /// <summary>
        /// クライアントのアクセスを受け入れ、WebSocketを返す
        /// </summary>
        public static async Task<WebSocket> acceptClientConnecting() {
            Debug.WriteLine("クライアントの接続承認の待受けに入りました");
            var httpListenerContext = await HTTP_LISTENER.GetContextAsync();

            //クライアントからのリクエストがWebSocketでないなら閉じてnullを返す
            if (httpListenerContext.Request.IsWebSocketRequest == false) {
                Debug.WriteLine("エラー：クライアントのリクエストがWebSocketではありません");
                httpListenerContext.Response.StatusCode = 400;
                httpListenerContext.Response.Close();
                return null;
            }

            //WebSocketでレスポンスを返却
            Debug.WriteLine("クライアントの接続を承認しました");
            var httpListenerWebSocketContext = await httpListenerContext.AcceptWebSocketAsync(null);
            return httpListenerWebSocketContext.WebSocket;
        }

        /// <summary>
        /// 指定されたWebsocketで、メッセージ(文字列)を送信する
        /// </summary>
        /// <param name="webSocket"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        private static async Task sendMessageAsync(WebSocket webSocket, string message) {
            Debug.WriteLine($"クライアントアプリにメッセージを送信しました： {message}");
            //文字列をバイト列に変換して送る
            var buffer = ENCODING.GetBytes(message);
            var segment = new ArraySegment<byte>(buffer);
            //awaitを付けておかないと正常に送信できないので注意
            await webSocket.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        /// <summary>
        /// 送られてきたメッセージをstringとして非同期で取得する
        /// 複数送られてきている場合、回ごとに分けて受信されるため、全てのメッセージを見るにはその回数分呼び出さないといけないことに注意
        /// 参考URL:https://qiita.com/Zumwalt/items/53797b0156ebbdcdbfb1
        /// </summary>
        /// <param name="webSocket"></param>
        /// <returns></returns>
        private static async Task<string> getReceiveMessageAsync(WebSocket webSocket) {
            Debug.WriteLine("クライアントからのメッセージ取得を開始します");

            var buffer = new byte[1024];
            //所得情報確保用の配列を準備
            var segment = new ArraySegment<byte>(buffer);
            //サーバからのレスポンス情報を取得
            var result = await webSocket.ReceiveAsync(segment, CancellationToken.None);

            if (result.MessageType == WebSocketMessageType.Close) {
                Debug.WriteLine("エラー：エンドポイントCloseのためメッセージを取得できません、nullを返します");
                return null;
            }
            if (result.MessageType == WebSocketMessageType.Binary) {
                Debug.WriteLine("エラー：送られてきたメッセージがバイナリのため取得できません、nullを返します");
                return null;
            }

            string messageStr = ENCODING.GetString(buffer, 0, result.Count);
            Debug.WriteLine($"メッセージを取得しました： {messageStr}");
            return messageStr;
        }

        /// <summary>
        /// 渡されたWebSocketのインスタンスを通して、クライアントにプレイヤーのデータを送信する
        /// </summary>
        /// <param name="webSocket"></param>
        /// <param name="player"></param>
        public static void sendPlayerDataToClient(WebSocket webSocket, Player player, int maxPlayerCount, MessageData.Type type) {
            Debug.WriteLine("プレイヤーデータを送ります");
            var messageData = new MessageData(player.ID, player.NICK_NAME, maxPlayerCount, type);
            Task.Run(() => sendMessageAsync(webSocket, JsonSerializer.Serialize(messageData)));
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
        /// 指定されたWebSocketでの通信を終了する
        /// 非同期での実行を行うと、回線が閉じきらないうちに次のメッセージの処理を行ってしまう危険があるので注意
        /// </summary>
        /// <param name="webSocket"></param>
        /// <param name="playerID"></param>
        /// <returns></returns>
        private static async Task closeAsync(WebSocket webSocket, string statusDescription, string playerID) {
            Debug.WriteLine($"クライアントとの接続を終了します 理由： {statusDescription}");
            Debug.WriteLine($"該当プレイヤーID: {playerID}");
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, statusDescription, CancellationToken.None);
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
