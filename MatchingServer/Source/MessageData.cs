using System.Text;

namespace MatchingServer {
    /// <summary>
    /// サーバ・クライアント間で送受信するメッセージのデータ
    /// </summary>
    struct MessageData {
        //プレイヤーのIDとニックネーム
        public readonly string PLAYER_ID;
        public readonly string PLAYER_NICK_NAME;
        //希望するプレイ人数
        public readonly int MAX_PLAYER_COUNT;

        //新しくルームに入りたいのか(対戦を行いたいのか)、今いるルームから退出したい(接続は切らない)のか、定時連絡なのか、切断するのか
        public enum Type {
            Join, 
            Leave,
            PeriodicReport, 
            Disconnect
        }
        public Type type_;

        public MessageData(string id, string nickName, int maxPlayerCount, Type type) {
            PLAYER_ID = id;
            PLAYER_NICK_NAME = nickName;
            MAX_PLAYER_COUNT = maxPlayerCount;
            type_ = type;
        }

        /// <summary>
        /// メッセージ内容を表示
        /// デバッグ用
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            //大丈夫だとは思うが、一応パフォーマンスを考えてStringBuilderを使用
            var stringBuilder = new StringBuilder($"プレイヤーID: {PLAYER_ID}\n");
            stringBuilder.Append($"ニックネーム： {PLAYER_NICK_NAME}\n");
            stringBuilder.Append($"希望プレイ人数： {MAX_PLAYER_COUNT}\n");
            stringBuilder.Append($"指示タイプ： {type_}\n");

            return stringBuilder.ToString();
        }
    }
}
