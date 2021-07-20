using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;

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
        public async Task<int> joinPlayerAsync(string id, string nickName, WebSocket webSocket, int maxPlayerCount) {
            //希望した対戦人数で空いているルームを見つけ次第入れる
            for (int index = 0; index < ROOMS.Count(); index++) {
                var room = ROOMS[index];
                if (room.canJoin() && room.MAX_PLAYER_COUNT == maxPlayerCount) {
                    var player = room.join(id, nickName, webSocket);
                    //ルームに自身が入ったことを他プレイヤーに通知する
                    //こうすることでルームに自身が入った場合・他プレイヤーがルームに入った場合共に対応可能
                    await player.sendMyDataToOthersAsync(room.getOtherPlayers(player), maxPlayerCount, MessageData.Type.Join);
                    return index;
                }
            }

            //どこも空いていなかったら新たにルームを作成して入り、待機する
            ROOMS.Add(new Room(id, nickName, webSocket, maxPlayerCount));
            //返すのはインデックスなので-1することに注意
            return ROOMS.Count() - 1;
        }

        /// <summary>
        /// 指定したルームの指定プレイヤーを退出させ、プレイヤーのインスタンスを返す
        /// </summary>
        /// <param name="id"></param>
        /// <param name="roomIndex"></param>
        public async Task<Player> leavePlayerAsync(string id, int roomIndex) {
            var room = ROOMS[roomIndex];
            //入室の際と同様に、退出の際にも他プレイヤーに通知する
            var player = room.leave(id);
            await player.sendMyDataToOthersAsync(room.getOtherPlayers(player), room.MAX_PLAYER_COUNT, MessageData.Type.Leave);
            //CPUしかいなくなり次第ルームを消去する
            if (room.allPlayerIsCPU()) ROOMS.RemoveAt(roomIndex);
            return player;
        }

        public override string ToString() {
            string str = $"現在のルーム数： {ROOMS.Count()}\n";
            for (int index = 0; index < ROOMS.Count(); index++) {
                //ルーム番号は1から始まることに注意
                //またルーム間は2行開ける(プレイヤーの情報間の空行はは1行なので、それとの区別の意も込めて)
                str += $"ルーム{index + 1}\n";
                str += ROOMS[index].ToString() + "\n\n";
            }
            return str;
        }
    }
}
