using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.WebSockets;

namespace MatchingServer {
    /// <summary>
    /// マッチングの際のルームのクラス
    /// </summary>
    sealed class Room : ElementsBase {
        //現在部屋にいるプレイヤー
        private readonly List<Player> PLAYERS = new List<Player>();

        //開いているかどうか
        private bool isOpened_ = true;
        //最大何人まで入室できるのか
        public readonly int MAX_PLAYER_COUNT;

        /// <summary>
        /// ルームのみを作成する場合のコンストラクタ
        /// </summary>
        public Room(int maxPlayerCount) {
            MAX_PLAYER_COUNT = maxPlayerCount;
        }

        /// <summary>
        /// 作成と同時に1人入室する際のコンストラクタ
        /// </summary>
        /// <param name="id"></param>
        /// <param name="nickName"></param>
        /// <param name="maxPlayerCount"></param>
        /// <param name="cpuCount"></param>
        public Room(string id, string nickName, WebSocket webSocket, int maxPlayerCount, int cpuCount = 0) {
            MAX_PLAYER_COUNT = maxPlayerCount;
            //作成と同時に1人が入るので、ルームの他のプレイヤーに入ったことを告知する必要はない(そもそもいない)ことに注意
            join(id, nickName, webSocket);
            //CPUのカウントは1から始まることに注意
            for (int number = 1; number <= cpuCount; number++) join(Player.createCPU(number));
        }

        /// <summary>
        /// 指定したIDのプレイヤーを入室させ、そのプレイヤーのインスタンスを返す
        /// </summary>
        /// <param name="id"></param>
        /// <param name="nickName"></param>
        public Player join(string id, string nickName, WebSocket webSocket) {
            //無効なidなら何もせず返す
            if (isCorrect(id) == false) return null;

            return join(Player.createClient(id, nickName, webSocket));
        }

        /// <summary>
        /// 指定したプレイヤーを入室させ、そのプレイヤーのインスタンスを返す
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public Player join(Player player) {
            //渡されたPlayerがnull、または既に満員なら何もせず返す
            if (player == null) return null;
            if (PLAYERS.Count() == MAX_PLAYER_COUNT) return null;

            Debug.WriteLine($"プレイヤーが入室しました ID: {player.ID}");

            PLAYERS.Add(player);
            //満員になり次第ルームを閉じる
            if (PLAYERS.Count() == MAX_PLAYER_COUNT) isOpened_ = false;
            return player;
        }

        /// <summary>
        /// 指定したIDのプレイヤーを退室させ、そのプレイヤーのインスタンスを返す
        /// </summary>
        public Player leave(string id) {
            return leave(getPlayer(id));
        }

        /// <summary>
        /// 指定したプレイヤーを退室させ、そのプレイヤーのインスタンスを返す
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public Player leave(Player player) {
            //渡されたPlayerがnullなら何もせず返す
            if (player == null) return null;

            Debug.WriteLine($"プレイヤーが退出しました ID: {player.ID}");

            PLAYERS.Remove(player);
            return player;
        }

        /// <summary>
        /// 指定されたプレイヤー以外のプレイヤーを取得する
        /// </summary>
        /// <param name="myPlayer"></param>
        /// <returns></returns>
        public Player[] getOtherPlayers(Player myPlayer) {
            return PLAYERS.Where(player => player != myPlayer).ToArray();
        }

        /// <summary>
        /// ホストのプレイヤーを返す
        /// </summary>
        /// <returns></returns>
        public Player getHostPlayer() {
            //先頭のプレイヤー(最初に入室したプレイヤー)を常にホストとする
            return PLAYERS.First();
        }

        /// <summary>
        /// 指定されたIDのプレイヤーを返す
        /// </summary>
        /// <param name="targetID"></param>
        /// <returns></returns>
        public Player getPlayer(string targetID) {
            //有効なIDでないならnullを返す
            if (isCorrect(targetID) == false) return null;

            return PLAYERS.Find(player => player.ID == targetID);
        }

        /// <summary>
        /// 指定された値で開室状況を切り替え、開室しているかを返す
        /// </summary>
        /// <param name="changingValue"></param>
        /// <returns></returns>
        public bool changeOpen(bool changingValue) { return isOpened_ = changingValue; }

        /// <summary>
        /// まだこの部屋に入れるかどうか
        /// </summary>
        /// <returns></returns>
        public bool canJoin() { return isOpened_ && PLAYERS.Count() < MAX_PLAYER_COUNT; }

        public override string ToString() {
            string str = "開室・閉室： " + (isOpened_ ? "開室" : "閉室") + "\n";
            str += $"最大収容人数： {MAX_PLAYER_COUNT}\n";
            str += $"現在のプレイヤー人数(CPUも含む)： {PLAYERS.Count()}\n";
            for (int index = 0; index < PLAYERS.Count(); index++) {
                //プレイヤー番号は1から始まることに注意
                str += $"プレイヤー{index + 1}\n";
                //プレイヤーの情報間には間を開けたほうが分かりやすい
                str += PLAYERS[index].ToString() + "\n";
            }

            return str;
        }
    }
}
