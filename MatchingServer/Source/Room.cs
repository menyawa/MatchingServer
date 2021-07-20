using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.WebSockets;
using System;

namespace MatchingServer {
    /// <summary>
    /// マッチングの際のルームのクラス
    /// </summary>
    sealed class Room : ElementsBase {
        //現在部屋にいるプレイヤー
        private readonly List<Player> PLAYERS = new List<Player>();
        //開いているかどうか
        private bool openingState_ = true;
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
        /// 指定されたプレイヤーを入室させ、渡された人数分CPUを入れる
        /// 新しいプレイヤーを受け入れる空いているルームが無かった場合に新しい部屋を作って入室するため複数人入室することはありえないことに注意
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
        /// 発生する可能性のある例外：ArgumentException、ArgumentNullException
        /// </summary>
        /// <param name="id"></param>
        /// <param name="nickName"></param>
        public Player join(string id, string nickName, WebSocket webSocket) {
            //無効なidなら何もせず返す
            if (isCorrect(id) == false) {
                Debug.WriteLine("エラー：無効なIDのため、入室処理が行えません\n");
                throw new ArgumentException();
            }

            return join(Player.createClient(id, nickName, webSocket));
        }

        /// <summary>
        /// 指定したプレイヤーを入室させ、そのプレイヤーのインスタンスを返す
        /// 発生する可能性のある例外：ArgumentNullException、InvalidOperationException
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public Player join(Player player) {
            //渡されたPlayerがnull、または既に満員なら何もせず返す
            if (player == null) {
                Debug.WriteLine("エラー：渡されたPlayerのインスタンスがnullのため、入室処理が行なえません");
                throw new ArgumentNullException();
            }
            if (PLAYERS.Count() == MAX_PLAYER_COUNT) {
                Debug.WriteLine("エラー：既にルームが満室のため、入室処理が行なえません");
                throw new InvalidOperationException();
            }

            if (player.isCPU() == false) Debug.WriteLine($"プレイヤーが入室しました ID: {player.ID}\n");

            PLAYERS.Add(player);
            //満員になり次第ルームを閉じる
            if (PLAYERS.Count() == MAX_PLAYER_COUNT) {
                Debug.WriteLine("満員になったため、ルームを閉じます\n");
                changeOpeningState(false);
            }
            return player;
        }

        /// <summary>
        /// 指定したIDのプレイヤーを退室させ、そのプレイヤーのインスタンスを返す
        /// 発生する可能性のある例外：ArgumentException
        /// </summary>
        public Player leave(string id) {
            //無効なidなら何もせず返す
            if (isCorrect(id) == false) {
                Debug.WriteLine("エラー：無効なIDのため、退室処理が行なえません\n");
                throw new ArgumentException();
            }

            return leave(getPlayer(id));
        }

        /// <summary>
        /// 指定したプレイヤーを退室させ、そのプレイヤーのインスタンスを返す
        /// 発生する可能性のある例外：ArgumentNullException、ArgumentException
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public Player leave(Player player) {
            //渡されたPlayerがnullなら何もせず返す
            if (player == null) {
                Debug.WriteLine("エラー：Playerのインスタンスがnullのため、退室処理が行なえません");
                throw new ArgumentNullException();
            } 
            bool result = PLAYERS.Remove(player);
            if(result == false) {
                Debug.WriteLine("エラー：PlayerのインスタンスのIDがルームにいるプレイヤーのIDと一致しないため、退室処理が行なえません");
                throw new ArgumentException();
            }

            if (player.isCPU() == false) Debug.WriteLine($"プレイヤーが退出しました ID: {player.ID}");

            return player;
        }

        /// <summary>
        /// 指定されたプレイヤー以外のプレイヤーを取得する
        /// 発生する可能性のある例外：ArgumentNullException
        /// </summary>
        /// <param name="myPlayer"></param>
        /// <returns></returns>
        public Player[] getOtherPlayers(Player myPlayer) {
            if(myPlayer == null) {
                Debug.WriteLine("エラー：渡されたPlayerがnullのため、他プレイヤーを取得できません");
                throw new ArgumentNullException();
            }

            return PLAYERS.Where(player => player != myPlayer).ToArray();
        }

        /// <summary>
        /// ホストのプレイヤーを返す
        /// </summary>
        /// <returns></returns>
        public Player getHostPlayer() {
            //先頭のプレイヤー(最初に入室したプレイヤー)を常にホストとする
            return PLAYERS.FirstOrDefault();
        }

        /// <summary>
        /// 指定されたIDのプレイヤーを返す
        /// 発生する可能性のある例外：ArgumentExcepition
        /// </summary>
        /// <param name="targetID"></param>
        /// <returns></returns>
        public Player getPlayer(string targetID) {
            //有効なIDでないならnullを返す
            if (isCorrect(targetID) == false) {
                Debug.WriteLine("エラー：渡されたIDが無効なIDのため、プレイヤーを取得できません");
                throw new ArgumentException();
            }
            var target = PLAYERS.Find(player => player.ID == targetID);
            if(target == null) {
                Debug.WriteLine("エラー：渡されたIDに一致するプレイヤーが見つかりませんでした");
                throw new ArgumentException();
            }

            return target;
        }

        /// <summary>
        /// 指定された値で開室状況を切り替え、開室しているかを返す
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public bool changeOpeningState(bool state) { return openingState_ = state; }

        /// <summary>
        /// まだこの部屋に入れるかどうか
        /// </summary>
        /// <returns></returns>
        public bool canJoin() { return openingState_ && PLAYERS.Count() < MAX_PLAYER_COUNT; }

        /// <summary>
        /// ルームのプレイヤーが全てCPUかどうか
        /// </summary>
        /// <returns></returns>
        public bool allPlayerIsCPU() { 
            return PLAYERS.Exists(player => player.isCPU() == false) == false; 
        }

        public override string ToString() {
            string str = "開室・閉室： " + (openingState_ ? "開室" : "閉室") + "\n";
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
