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
        /// プレイヤーをロビーのいずれかのルームに加入させ、入ったルームのインデックスを返す
        /// </summary>
        /// <returns></returns>
        public int joinPlayer(string id, string nickName, int maxPlayerCount) {
            //希望した対戦人数で空いているルームを見つけ次第入れる
            for (int index = 0; index < ROOMS.Count(); index++) {
                var room = ROOMS[index];
                if(room.canJoin() && room.MAX_PLAYER_COUNT == maxPlayerCount) {
                    room.join(id, nickName);
                    return index;
                }
            }

            //どこも空いていなかったら新たにルームを作成して入り、待機する
            ROOMS.Add(new Room(new KeyValuePair<string, string>(id, nickName), maxPlayerCount));
            //返すのはインデックスなので-1することに注意
            return ROOMS.Count() - 1;
        }
    }
}
