using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MatchingServer {
    /// <summary>
    /// マッチングの際のルームのクラス
    /// </summary>
    class Room {
        //現在部屋にいるプレイヤー
        private readonly List<Player> PLAYERS = new List<Player>();
        //ルームに紐付く何らかのパラメータ
        //パラメータごとに値の型は変わる可能性があるため、DictionaryでなくHashtableで作成
        private readonly Hashtable CUSTOM_PROPERTIES = new Hashtable();

        //開いているかどうか
        public bool isOpened_ = true;
        //最大何人まで入室できるのか
        private readonly int MAX_PLAYER_COUNT;

        /// <summary>
        /// ルームのみを作成する場合のコンストラクタ
        /// </summary>
        public Room(int maxPlayerCount) : this(new string[]{}, maxPlayerCount) {}

        /// <summary>
        /// 作成と同時に1人入室する際のコンストラクタ
        /// </summary>
        /// <param name="id"></param>
        public Room(string id, int maxPlayerCount) : this(new string[]{ id }, maxPlayerCount) {}

        /// <summary>
        /// 作成と同時に何人か入室する際のコンストラクタ
        /// </summary>
        /// <param name="ids"></param>
        public Room(string[] ids, int maxPlayerCount) {
            foreach(var id in ids) join(id);
            MAX_PLAYER_COUNT = maxPlayerCount;
        }

        /// <summary>
        /// 指定したIDのプレイヤーを入室させ、そのプレイヤーのインスタンスを返す
        /// </summary>
        /// <param name="id"></param>
        public Player join(string id) {
            //無効なidなら何もせず返す
            if (Player.isCorrect(id) == false) return null;

            return join(new Player(id));
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

            PLAYERS.Add(player);
            return player;
        }

        /// <summary>
        /// 指定したIDのプレイヤーを退室させ、そのプレイヤーのインスタンスを返す
        /// </summary>
        public Player leave(string id) {
            //無効なidなら何もせず返す
            if (Player.isCorrect(id) == false) return null;

            return leave(PLAYERS.Find(value => value.ID == id));
        }

        /// <summary>
        /// 指定したプレイヤーを退室させ、そのプレイヤーのインスタンスを返す
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public Player leave(Player player) {
            //渡されたPlayerがnullなら何もせず返す
            if (player == null) return null;

            PLAYERS.Remove(player);
            return player;
        }

        /// <summary>
        /// 指定された値で開室状況を切り替え、開室しているかを返す
        /// </summary>
        /// <param name="changingValue"></param>
        /// <returns></returns>
        public bool changeOpen(bool changingValue) { return isOpened_ = changingValue; }

        public Player getHostPlayer() {
            //先頭のプレイヤー(最初に入室したプレイヤー)を常にホストとする
            return PLAYERS.First();
        }
    }
}
