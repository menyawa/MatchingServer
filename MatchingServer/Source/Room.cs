using System.Collections.Generic;
using System.Linq;

namespace MatchingServer {
    /// <summary>
    /// マッチングの際のルームのクラス
    /// </summary>
    class Room {
        //現在部屋にいるプレイヤー
        private readonly List<Player> PLAYERS = new List<Player>();

        /// <summary>
        /// ルームのみを作成する場合のコンストラクタ
        /// </summary>
        public Room() {}

        /// <summary>
        /// 作成と同時に1人入室する際のコンストラクタ
        /// </summary>
        /// <param name="id"></param>
        public Room(string id) : this(new string[]{ id }) {}

        /// <summary>
        /// 作成と同時に何人か入室する際のコンストラクタ
        /// </summary>
        /// <param name="ids"></param>
        public Room(string[] ids) {
            foreach(var id in ids) join(id);
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
            //渡されたPlayerがnullなら何もせず返す
            if (player == null) return null;

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

        public Player getHostPlayer() {
            //先頭のプレイヤー(最初に入室したプレイヤー)を常にホストとする
            return PLAYERS.First();
        }
    }
}
