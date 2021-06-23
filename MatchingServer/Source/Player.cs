using System;

namespace MatchingServer {
    /// <summary>
    /// オンラインプレイにおける、プレイヤー1人あたりのクラス
    /// </summary>
    class Player {
        //プレイヤーID
        public readonly string ID;

        public Player(string id) {
            ID = id;
        }

        /// <summary>
        /// 有効な(真正な)idかどうか
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static bool isCorrect(string id) {
            //null、空文字、スペースのみでなければ有効とみなす
            return string.IsNullOrEmpty(id) == false && string.IsNullOrWhiteSpace(id) == false;
        }
    }
}
