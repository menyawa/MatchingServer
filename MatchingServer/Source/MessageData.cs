using System.Text;
using System.Text.Json.Serialization;

namespace MatchingServer {
    /// <summary>
    /// サーバ・クライアント間で送受信するメッセージのデータ
    /// 
    /// </summary>
    struct MessageData {
        //プレイヤーのIDとニックネーム
        //readonlyを付けるとコピーコンストラクタの関係か、デシリアライズができないので注意
        [JsonInclude]
        public string PLAYER_ID;
        [JsonInclude]
        public string PLAYER_NICK_NAME;
        //希望するプレイ人数
        [JsonInclude]
        public int MAX_PLAYER_COUNT;

        //新しくルームに入りたいのか(対戦を行いたいのか)、今いるルームから退出したい(接続は切らない)のか、定時連絡なのか、切断するのか
        public enum Type {
            Join,
            Leave,
            PeriodicReport,
            Disconnect
        }
        [JsonInclude]
        public Type type_;

        public MessageData(string id, string nickName, int maxPlayerCount, Type type) {
            PLAYER_ID = id;
            PLAYER_NICK_NAME = nickName;
            MAX_PLAYER_COUNT = maxPlayerCount;
            type_ = type;
        }

        /// <summary>
        /// 仮置の空データかどうか
        /// </summary>
        /// <returns></returns>
        public bool isBlank() {
            return this == getBlankData();
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

        public override bool Equals(object obj) {
            //引数のインスタンスの型がMessageDataかつ、全てのフィールドが同一なら等しいとみなす
            if (obj is MessageData otherMessageData) {
                return this.PLAYER_ID == otherMessageData.PLAYER_ID && this.PLAYER_NICK_NAME == otherMessageData.PLAYER_NICK_NAME && this.MAX_PLAYER_COUNT == otherMessageData.MAX_PLAYER_COUNT && this.type_ == otherMessageData.type_;
            } else return false;
        }

        public static bool operator ==(MessageData myMessageData, MessageData otherMessageData) {
            return myMessageData.Equals(otherMessageData);
        }

        public static bool operator !=(MessageData myMessageData, MessageData otherMessageData) {
            return myMessageData.Equals(otherMessageData) == false;
        }

        /// <summary>
        /// 仮のメッセージのデータを取得する
        /// 値型なので仮置としてデータが必要
        /// </summary>
        public static MessageData getBlankData() {
            return new MessageData("Blank", "Blank", -1, Type.PeriodicReport);
        }
    }
}
