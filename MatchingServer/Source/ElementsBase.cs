using System.Collections;

namespace MatchingServer {
    /// <summary>
    /// Player、Room、Lobbyの親のabstractクラス
    /// TODO:もう少し良い名前を考える
    /// </summary>
    abstract class ElementsBase {
        //紐付いている何らかのパラメータ
        //パラメータごとに値の型は変わる可能性があるため、DictionaryでなくHashtableで作成
        private readonly Hashtable CUSTOM_PROPERTIES = new Hashtable();

        /// <summary>
        /// 指定したキー、値のカスタムプロパティをセットする
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void setCustomProperties(string key, object value) {
            //キーや値がnull等で無効なら何もせず返す
            if (isCorrect(key) == false) return;
            if (value == null) return;

            CUSTOM_PROPERTIES.Add(key, value);
        }

        /// <summary>
        /// 指定したキーのカスタムプロパティの値を取得する
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public object getCustomProperties(string key) {
            //そのキーが無効、あるいは値が存在しないなら何もせず返す
            if (isCorrect(key) == false) return null;
            if (CUSTOM_PROPERTIES.Contains(key) == false) return null;

            return CUSTOM_PROPERTIES[key];
        }

        /// <summary>
        /// 有効な(真正な)文字列の値かどうか
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        protected static bool isCorrect(string str) {
            //null、空文字、スペースのみでなければ有効とみなす
            return string.IsNullOrEmpty(str) == false && string.IsNullOrWhiteSpace(str) == false;
        }
    }
}
