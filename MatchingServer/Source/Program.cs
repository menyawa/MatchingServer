﻿using System;
using System.Threading.Tasks;

namespace MatchingServer {
    class Program {
        private static async Task Main(string[] args) {
            while (true) {
                //クライアントの接続を受け入れ次第次の受け入れを開始する
                //awaitで止まるため、接続が行われないまま次の受け入れが始まる心配はない
                //受け入れがエラーとなって終わった場合nullが返されるので、その場合次に行く
                var webSocket = await Server.acceptClientConnecting();
                if(webSocket != null) Server.RunAsync(webSocket);
                Console.WriteLine("Next Standby");


                //ゲームアプリ→クライアントアプリ→サーバへのメッセージ伝送と、その逆を検証する
            }
        }
    }
}
