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

        //新しくルームに入りたいのか(対戦を行いたいのか)、今いるルームから退出したい(接続は切らない)のか、切断するのか
        public enum Type {
            Join, 
            Leave, 
            Disconnect
        }
        public Type type_;

        public MessageData(string id, string nickName, int maxPlayerCount, Type type) {
            PLAYER_ID = id;
            PLAYER_NICK_NAME = nickName;
            MAX_PLAYER_COUNT = maxPlayerCount;
            type_ = type;
        }
    }
}
