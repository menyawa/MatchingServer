using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MatchingServer {
    class Program {
        private static async Task Main(string[] args) {
            //デバッグ出力が標準出力に(コンソールに)出力されるようにする
            Trace.Listeners.Add(new ConsoleTraceListener());
            Debug.WriteLine("Debug Mode");

            while (true) {
                //クライアントの接続を受け入れ次第別スレッドでその接続でのメッセージ送受信のタスクを開始し、リストに放り込む
                //その後、完了済の同リストのタスクを削除し、次の受け入れを開始する
                //awaitで止まるため、接続が行われないまま次の受け入れが始まる心配はない
                //受け入れがエラーとなって終わった場合nullが返されるので、その場合次に行く
                var webSocket = await Server.acceptClientConnecting();
                if (webSocket != null) Server.addClientConnectingTask(Task.Run(() => Server.supportMessageAsync(webSocket)));
                Server.removeCompleteClientConnectingTask();
                Debug.WriteLine("次のクライアントの接続承認の待受けに入ります");
            }
        }
    }
}
