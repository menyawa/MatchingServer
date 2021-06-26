using System;
using System.Net.WebSockets;

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
        public enum Type {
            Client, 
            CPU
        }
        //途中で切断されてCPUに切り替わる可能性があることに注意
        public Type type_;

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
        /// 結びついているクライアントに渡されたプレイヤーのデータを渡す
        /// </summary>
        /// <param name="player"></param>
        public void sendPlayerDataToClient(Player player, int maxPlayerCount, MessageData.Type type) {
            //CPUなら結びついているクライアントがいないので、送らない
            if (type_ == Type.CPU) return;

            Server.sendPlayerDataToClient(WEBSOCKET, player, maxPlayerCount, type);
        }
    }
}
