using System;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace MatchingServer {
    /// <summary>
    /// オンラインプレイにおける、プレイヤー1人あたりのクラス
    /// </summary>
    sealed class Player : ElementsBase {
        //プレイヤーID
        public readonly string ID;
        //ニックネーム
        public readonly string NICK_NAME;

        //実際に操作されるクライアントか、CPUなのか
        private enum Type {
            Client, 
            CPU
        }
        //途中で切断されてCPUに切り替わる可能性があることに注意
        private Type type_;

        //クライアントと結びついているWEBSOCKET
        private readonly WebSocket WEBSOCKET;

        private Player(string id, string nickName, Type type, WebSocket webSocket) {
            ID = id;
            NICK_NAME = nickName;
            type_ = type;
            WEBSOCKET = webSocket;
        }

        /// <summary>
        /// 指定されたID、ニックネームのクライアントのプレイヤーを生成して返す
        /// </summary>
        /// <param name="id"></param>
        /// <param name="nickName"></param>
        /// <returns></returns>
        public static Player createClient(string id, string nickName, WebSocket webSocket) {
            return new Player(id, nickName, Type.Client, webSocket);
        }

        /// <summary>
        /// 指定された番号でCPUのプレイヤーのインスタンスを生成して返す
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public static Player createCPU(int number) {
            return new Player(DateTime.Now.ToString(), $"CPU{number}", Type.CPU, null);
        }

        /// <summary>
        /// 指定された他のプレイヤーに、自身のプレイヤーデータと、入室・退室等の情報を送信させる
        /// </summary>
        /// <param name="otherPlayers"></param>
        public async Task sendMyDataToOthersAsync(Player[] otherPlayers, int maxPlayerCount, MessageData.Type type) {
            foreach (var otherPlayer in otherPlayers) {
                await otherPlayer.sendPlayerDataToClientAsync(this, maxPlayerCount, type);
            }
            //見やすいよう最後に改行を入れる
            Debug.WriteLine("\n");
        }

        /// <summary>
        /// 結びついているクライアントに渡されたプレイヤーのデータを渡す
        /// </summary>
        /// <param name="otherPlayer"></param>
        private async Task sendPlayerDataToClientAsync(Player otherPlayer, int maxPlayerCount, MessageData.Type type) {
            //CPUなら結びついているクライアントがいないので、送らない
            if (type_ == Type.CPU) return;
            Debug.WriteLine($"プレイヤーID: {this.ID}にプレイヤーID: {otherPlayer.ID}が{MessageData.getMessageTypeDataTypeStr(type)}したというメッセージの送信を開始します");

            var messageData = new MessageData(otherPlayer.ID, otherPlayer.NICK_NAME, maxPlayerCount, type);
            await Server.sendMessageAsync(WEBSOCKET, messageData);
            Debug.WriteLine($"プレイヤーID: {this.ID}にプレイヤーID: {otherPlayer.ID}が{MessageData.getMessageTypeDataTypeStr(type)}したというメッセージの送信に成功しました");
        }

        public override string ToString() {
            string str = $"ID: {ID}\n";
            str += $"ニックネーム： {NICK_NAME}\n";
            str += type_ == Type.Client ? "参加プレイヤー\n" : "CPU\n";

            return str;
        }

        /// <summary>
        /// CPUかどうか
        /// </summary>
        /// <returns></returns>
        public bool isCPU() {
            return type_ == Type.CPU;
        }

        /// <summary>
        /// 切断時、プレイヤーをCPUに切り替える
        /// </summary>
        public void switchTypeByDisconnect() {
            type_ = Type.CPU;
        }
    }
}
