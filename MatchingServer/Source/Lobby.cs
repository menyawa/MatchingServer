using System;
using System.Collections.Generic;
using System.Linq;

namespace MatchingServer {
    /// <summary>
    /// 対戦ロビーのクラス
    /// </summary>
    sealed class Lobby : ElementsBase {
        private readonly List<Room> ROOMS = new List<Room>();

        public Lobby() { }

        /// <summary>
        /// プレイヤーをロビーのいずれかのルームに加入させ、入れたかどうかを返す
        /// </summary>
        /// <returns></returns>
        public void joinPlayer(string id, string nickName, int maxPlayerCount) {
            //希望した対戦人数で空いているルームを見つけ次第入れる
            foreach (var room in ROOMS) {
                if (room.canJoin() && room.MAX_PLAYER_COUNT == maxPlayerCount) {
                    room.join(id, nickName);
                    return;
                }
            }

            //どこも空いていなかったら新たにルームを作成して入り、待機する
            ROOMS.Add(new Room(new KeyValuePair<string, string>(id, nickName), maxPlayerCount));
        }
    }
}
