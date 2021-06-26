﻿using System.Collections.Generic;
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

            PLAYERS.Add(player);
            //満員になり次第ルームを閉じる
            if (PLAYERS.Count() == MAX_PLAYER_COUNT) isOpened_ = false;
            return player;
        }

        /// <summary>
        /// 指定したIDのプレイヤーを退室させ、そのプレイヤーのインスタンスを返す
        /// </summary>
        public Player leave(string id) {
            //無効なidなら何もせず返す
            if (isCorrect(id) == false) return null;

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
        /// ホストのプレイヤーIDを返す
        /// </summary>
        /// <returns></returns>
        public string getHostPlayerID() { return getHostPlayer().ID; }

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
    }
}
