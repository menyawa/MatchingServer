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
        public const int INVAID_ID = -1;
        //現在走っているクライアントとの接続のタスクのリスト
        private static readonly List<Task> CLIENT_CONNECTING_TASK_LIST = new List<Task>();

        static Server() {
            //接続待ちは別で行う
            Debug.WriteLine("HttpListenerを初期化します");
            HTTP_LISTENER = new HttpListener();
            HTTP_LISTENER.Prefixes.Add("http://localhost:8000/ws/");
            HTTP_LISTENER.Start();
            Debug.WriteLine("HttpListenerを初期化しました");
        }

        /// <summary>
        /// クライアントのアクセスを受け入れ、コネクションのWebSocketを返す
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

            var httpListenerWebSocketContext = await httpListenerContext.AcceptWebSocketAsync(null);
            Debug.WriteLine("クライアントの接続を承認しました\n");
            return httpListenerWebSocketContext.WebSocket;
        }

        /// <summary>
        /// クライアントとのコネクションのWebSocketを受け取り、メッセージの送受信、それに伴う入退室等の対応を行う
        /// クライアントの接続ごとに1つずつこのメソッドが回る
        /// 参考URL:https://qiita.com/Zumwalt/items/53797b0156ebbdcdbfb1
        /// </summary>
        /// <returns></returns>
        public static async Task supportMessageAsync(WebSocket webSocket) {
            //応答なしの時間を測るため、ストップウォッチを用意して開始
            var noResponseTimeStopwatch = new Stopwatch();
            noResponseTimeStopwatch.Start();
            //このメソッドで担当するプレイヤーのIDと、現在プレイヤーがいるルームのindex
            string playerID = INVAID_ID.ToString();
            int currentRoomIndex = INVAID_ID;
            //受信中の応答無しの時間を測定する関係上、awaitで待つことはしない
            //また別スレッドで実行しなくても良い(内部的にメッセージ取得を別メソッドで行うようにしているため)
            var getClientMessageTask = getReceiveMessageAsync(webSocket);

            //今後同期を行うことも考え、Task.Delayでの通信遅延は行わず毎フレーム更新を行う
            while (isConnected(webSocket.State)) {
                //受信完了していたらそのメッセージに応じた入室、退室等の処理を行う
                if (getClientMessageTask.IsCompletedSuccessfully) {
                    //応答があった時点で応答なしの時間はリセットしておく
                    noResponseTimeStopwatch.Restart();
                    //メッセージの受信が完了したら新たな受信待ちを開始しないと次のメッセージが受け取れないことに注意
                    //また受信データをキャッシュしておかないと、新しい受信待ちタスクに入れ替えた後では古いメッセージが受信できないので注意
                    var messageStr = getClientMessageTask.Result;

                    //エンドポイントCloseやバイナリは有効なメッセージとみなされない
                    if (messageStr == null) {
                        Debug.WriteLine("有効なメッセージが送られませんでした");
                        Debug.WriteLine("次のメッセージ取得に移行します");
                        try {
                            getClientMessageTask = getReceiveMessageAsync(webSocket);
                        } catch (ArgumentException) {
                            Debug.WriteLine("新規のメッセージ取得タスクを生成する際、引数関係でエラーが発生しました");
                            Debug.WriteLine("メッセージの応対を終了します");
                            break;
                        }

                        continue;
                    }

                    var clientMessageData = JsonSerializer.Deserialize<MessageData>(getClientMessageTask.Result);
                    playerID = clientMessageData.PLAYER_ID;
                    //ここで待たないと前回のメッセージの処理が終わらないうちに次のメッセージの処理が始まる危険性があるので注意
                    //また処理を行う→新しいメッセージ取得タスクを開始という順で行わないと、回線切断時にエラーが起きる危険があるので注意
                    currentRoomIndex = await runByClientMessageProgressAsync(webSocket, clientMessageData, currentRoomIndex);

                    //切断されているなら即抜けで問題ない
                    if (isConnected(webSocket.State) == false) break;
                    try {
                        getClientMessageTask = getReceiveMessageAsync(webSocket);
                    } catch (ArgumentException) {
                        Debug.WriteLine("新規のメッセージ取得タスクを生成する際、引数関係でエラーが発生しました");
                        Debug.WriteLine("メッセージの応対を終了します");
                        break;
                    }
                } else {
                    //していなかったらタイムアウトしているかどうか見て、していた場合コネクションを切断する
                    //受信していないかつタイムアウトもしていないなら何も行わない
                    //またタイムアウト時点で退室が行えていない場合があるので、ルームIDはリセットしないままメインループを抜ける
                    if (isTimeOut(noResponseTimeStopwatch.Elapsed.TotalSeconds)) {
                        Debug.WriteLine("タイムアウトしたため、接続を強制切断します");
                        //AbortすることでIO操作も打ち切られる
                        try {
                            webSocket.Abort();
                        } catch (WebSocketException) {
                            Debug.WriteLine("エラー：タイムアウトでの切断時にエラーが発生しました(恐らく、クライアントアプリ側でタイムアウトとして切断されました)");
                        }
                        break;
                    }
                }
            }

            //breakで抜けるなどして、メインループを抜けた場合でも接続されたままの場合を考慮する
            if (isConnected(webSocket.State)) {
                Debug.WriteLine("異常終了を検知しました");
                Debug.WriteLine("回線を切断します");
                //AbortすることでIO操作も打ち切られる
                webSocket.Abort();
            }

            //クライアントアプリの終了等による強制切断を考慮し、接続切断時点でルームIDが無効になっていないなら退室処理を行う
            if (currentRoomIndex != INVAID_ID) {
                Debug.WriteLine("強制切断を検知したため、プレイヤーの退室処理を行います");
                await getDefaultLobby().leavePlayerAsync(playerID, currentRoomIndex, MessageData.Type.Leave);
            }
            Debug.WriteLine($"プレイヤーID：{playerID}の接続を終了しました\n");
        }

        /// <summary>
        /// クライアントからのメッセージに応じた処理を非同期で行う
        /// TODO:もうちょっといい名前を考える
        /// </summary>
        /// <param name="webSocket"></param>
        /// <param name="messageData"></param>
        /// <param name="currentRoomIndex"></param>
        /// <returns></returns>
        private static async Task<int> runByClientMessageProgressAsync(WebSocket webSocket, MessageData messageData, int currentRoomIndex) {
            switch (messageData.type_) {
                case MessageData.Type.Join:
                    currentRoomIndex = await getDefaultLobby().joinPlayerAsync(messageData.PLAYER_ID, messageData.PLAYER_NICK_NAME, webSocket, messageData.MAX_PLAYER_COUNT);
                    break;

                case MessageData.Type.Leave:
                    //ルームに入る→退室するという順番でないと、当然ながらエラーが出るので注意
                    await getDefaultLobby().leavePlayerAsync(messageData.PLAYER_ID, currentRoomIndex, messageData.type_);
                    currentRoomIndex = INVAID_ID;
                    break;

                case MessageData.Type.PeriodicReport:
                    break;
                case MessageData.Type.Disconnect:
                    //切断要請があり次第こちらからも切断する
                    //サーバとクライアント両方でCloseAsyncは行わないと、WebSocketは閉じないので注意
                    await closeClientConnectingAsync(webSocket, "通常終了", messageData.PLAYER_ID);
                    //まだルームにいるなら退室処理を行う
                    if (currentRoomIndex != INVAID_ID)
                        await getDefaultLobby().leavePlayerAsync(messageData.PLAYER_ID, currentRoomIndex, messageData.type_);
                    currentRoomIndex = INVAID_ID;
                    break;
                case MessageData.Type.TimeOut:
                    //タイムアウトのメッセージが届く == まだサーバとは接続されており切断を望んでいるということなため、closeAsyncによる切断が必要となる
                    await closeClientConnectingAsync(webSocket, "ゲームアプリタイムアウト", messageData.PLAYER_ID);
                    if (currentRoomIndex != INVAID_ID)
                        await getDefaultLobby().leavePlayerAsync(messageData.PLAYER_ID, currentRoomIndex, messageData.type_);
                    currentRoomIndex = INVAID_ID;
                    break;
                default:
                    break;
            }
            return currentRoomIndex;
        }

        /// <summary>
        /// 指定されたWebsocketで、メッセージ(文字列)を送信し、送信成功したかどうかを返す
        /// 発生する可能性のある例外：ArgumentException
        /// </summary>
        /// <param name="webSocket"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        private static async Task<bool> sendMessageAsync(WebSocket webSocket, string message) {
            Debug.WriteLine("クライアントアプリにメッセージを送信します");
            if (isConnected(webSocket.State) == false) {
                Debug.WriteLine("エラー：WebSocketが接続されていないため、送信できません");
                throw new ArgumentException();
            }

            //文字列をバイト列に変換して送る
            var segment = new ArraySegment<byte>(ENCODING.GetBytes(message));
            try {
                await Task.Run(() => webSocket.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None));
            } catch (WebSocketException) {
                Debug.WriteLine("WebSocketExceptionを検知しました(恐らく、クライアントアプリが強制的に切断しました)");
                Debug.WriteLine("送信失敗しました\n");
                return false;
            }
            Debug.WriteLine($"クライアントアプリにメッセージを送信成功しました： {message}");
            return true;
        }

        /// <summary>
        /// 指定されたWebsocketで、渡されたMessageDataを文字列として送信し、送信成功したかどうかを返す
        /// </summary>
        /// <param name="webSocket"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static async Task<bool> sendMessageAsync(WebSocket webSocket, MessageData data) {
            return await sendMessageAsync(webSocket, data.ToString());
        }

        /// <summary>
        /// 送られてきたメッセージをstringとして非同期で取得する
        /// 複数送られてきている場合、回ごとに分けて受信されるため、全てのメッセージを見るにはその回数分呼び出さないといけないことに注意
        /// 発生する可能性のある例外：ArgumentException
        /// 参考URL:https://qiita.com/Zumwalt/items/53797b0156ebbdcdbfb1
        /// </summary>
        /// <param name="webSocket"></param>
        /// <returns></returns>
        private static async Task<string> getReceiveMessageAsync(WebSocket webSocket) {
            Debug.WriteLine("クライアントからのメッセージ取得を開始します");
            if (isConnected(webSocket.State) == false) {
                Debug.WriteLine("エラー：WebSocketが接続されていないため、取得できません");
                throw new ArgumentException();
            }

            var buffer = new byte[1024];
            //所得情報確保用の配列を準備
            var segment = new ArraySegment<byte>(buffer);
            try {
                //サーバからのレスポンス情報を取得
                var result = await Task.Run(() => webSocket.ReceiveAsync(segment, CancellationToken.None));

                //バイナリ、エンドポイントCloseのメッセージは無視する
                if (result.MessageType == WebSocketMessageType.Binary) {
                    Debug.WriteLine("エラー：送られてきたメッセージがバイナリのため取得できません、nullを返します");
                    return null;
                }
                if (result.MessageType == WebSocketMessageType.Close) {
                    Debug.WriteLine("エンドポイントがCloseのメッセージ(CloseAsyncの切断要求で自動で届くもの)なので無視してnullを返します");
                    return null;
                }
                string messageStr = ENCODING.GetString(buffer, 0, result.Count);
                Debug.WriteLine($"メッセージ取得に成功しました： {messageStr}");
                return messageStr;
            } catch (WebSocketException) {
                Debug.WriteLine("WebSocketExceptionを検知しました(恐らく、クライアントアプリが強制的に切断しました)");
                Debug.WriteLine("受信失敗しました(nullを返します)");
                return null;
            }
        }

        /// <summary>
        /// 渡されたWebsocketの状態を見て、接続されているかどうか返す
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        private static bool isConnected(WebSocketState state) {
            //ConnectingかOpenかどうかでは、クライアントから切断要求が届いた際にまだ切断されていないのに切断されていると判定されてしまうので注意
            return state != WebSocketState.Aborted && state != WebSocketState.None && state != WebSocketState.Closed;
        }

        /// <summary>
        /// クライアントからの応答がタイムアウトしたかどうか
        /// </summary>
        /// <param name="noResponceTime"></param>
        /// <returns></returns>
        private static bool isTimeOut(double noResponceTime) {
            return noResponceTime >= 20.0;
        }

        /// <summary>
        /// デフォルトのロビーを返す
        /// </summary>
        /// <returns></returns>
        private static Lobby getDefaultLobby() {
            return LOBBYS.FirstOrDefault();
        }

        /// <summary>
        /// クライアントとの接続のタスクをリストに加える
        /// </summary>
        /// <param name="task"></param>
        public static void addClientConnectingTask(Task task) {
            CLIENT_CONNECTING_TASK_LIST.Add(task);
        }

        /// <summary>
        /// 終了したクライアントとの接続のタスクを削除する
        /// </summary>
        public static void removeCompleteClientConnectingTask() {
            CLIENT_CONNECTING_TASK_LIST.RemoveAll(task => task.IsCompleted);
        }

        /// <summary>
        /// 現在何人のクライアントが接続しているかを取得する
        /// </summary>
        /// <returns></returns>
        public static int getConnectingClientCount() {
            return CLIENT_CONNECTING_TASK_LIST.Count;
        }

        /// <summary>
        /// 指定されたWebSocketでのクライアント接続を終了する
        /// 非同期での実行を行うと、回線が閉じきらないうちに次のメッセージの処理を行ってしまう危険があるので注意
        /// </summary>
        /// <param name="webSocket"></param>
        /// <param name="playerID"></param>
        /// <returns></returns>
        private static async Task closeClientConnectingAsync(WebSocket webSocket, string statusDescription, string playerID) {
            Debug.WriteLine($"クライアントとの接続を終了します 理由： {statusDescription}");
            Debug.WriteLine($"該当プレイヤーID: {playerID}");
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, statusDescription, CancellationToken.None);
            Debug.WriteLine("クライアントとの接続終了に成功しました");
        }
    }
}
