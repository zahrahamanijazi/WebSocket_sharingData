using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace FileSharingSystem
{
    public class ServerConnection : WebSocketBehavior
    {
        public WebSocketServer wss = null;
        string MyPort;
        string MyName;
        public static DataPackage _dataPackage = new DataPackage();

        //------------------------------------------------------------------------------------------------------------------------
        //------------------------------------------------------------------------------------------------------------------------
        //------------------------------------------------------------------------------------------------------------------------
        public ServerConnection()
        {
            Program.Server = this;
        }

        //------------------------------------------------------------------------------------------------------------------------
        //------------------------------------------------------------------------------------------------------------------------
        //------------------------------------------------------------------------------------------------------------------------
        public ServerConnection(string port, string name)
        {
            MyPort = port;
            MyName = name;
        }

        //------------------------------------------------------------------------------------------------------------------------
        //------------------------------------------------------------------------------------------------------------------------
        //------------------------------------------------------------------------------------------------------------------------
        // creating a websocket for this node
        public void Start()
        {
            wss = new WebSocketServer($"ws://127.0.0.1:{MyPort}");
            wss.AddWebSocketService<ServerConnection>("/FileSharingSystem");
            wss.Start();

            Program.Log($"Started server at ws://127.0.0.1:{MyPort}");
        }

        //------------------------------------------------------------------------------------------------------------------------
        //------------------------------------------------------------------------------------------------------------------------
        //------------------------------------------------------------------------------------------------------------------------
        public void Brodcast(string data)
        {
            if (this.Sessions != null)
                this.Sessions.Broadcast(data);
            //this.Sessions.Broadcast(data);
        }
        //=======================================================================================================
        //====================================  Merging Distributed Hash Tables   ===============================
        //=======================================================================================================
        private bool MigrateDHT(List<Node> MainDHT, List<Node> ClientDHT)
        {
            bool IsChanged = false;
            if (MainDHT == null || ClientDHT == null)
                return IsChanged;
            //Merging Distributed Hash Tables
            foreach (var node in ClientDHT)
            {
                var FoundNode = MainDHT.Where(i => i.Url == node.Url).DefaultIfEmpty(null).First();
                if (FoundNode == null)
                {
                    MainDHT.Add(node);
                    IsChanged = true;
                }
                else
                {
                    if (node.LastUpdateTime > FoundNode.LastUpdateTime)
                    {
                        MainDHT.Remove(FoundNode);
                        MainDHT.Add(node);
                        IsChanged = true;
                    }
                }
            }

            return IsChanged;
        }

        //=======================================================================================================
        //============================== THis is for closing connection  =========================
        //=======================================================================================================
        protected override void OnClose(CloseEventArgs e)
        {
            base.OnClose(e);
            Program.Log("IMPOSSIBLE TO CONNECT!!!!!");
        }

        //=======================================================================================================
        //============================== THis is for getting data from the client node  =========================
        //=======================================================================================================
        protected override void OnMessage(MessageEventArgs e)
        {
            if (e.IsText && e.Data.Length > 0)
            {
                //Check if data is for disconnecting some nodes
                bool IsDisconnectRequest = false;
                DisconnectedClient DC = null;
                try
                {
                    DC = JsonConvert.DeserializeObject<DisconnectedClient>(e.Data);
                    if (!string.IsNullOrEmpty(DC.MyUrl))
                        IsDisconnectRequest = true;
                }
                catch (Exception)
                {
                    IsDisconnectRequest = false;
                    DC = null;
                }

                if (IsDisconnectRequest)
                {
                    Program.Log("CANNOT CONNECT-This Client TRIED TO CONNECT: " + DC.MyUrl);
                    //var ND = Program.DHT.Where(i => i.Url == DC.MyUrl).DefaultIfEmpty(null).First();
                    //if (ND != null)
                    //{
                    //    if (ND.ServerList == "ws://127.0.0.1:" + Program.Port.ToString() + ",")
                    //        Program.DHT.Remove(ND);
                    //    else
                    //    {
                    //        ND.ServerList.Replace("ws://127.0.0.1:" + Program.Port.ToString() + ",", "");
                    //    }
                    //}
                }
                //Check if server node has a file request
                else
                { 
                    bool IsFileRequest = false;
                    RequestFile FileReq = null;
                    try
                    {
                        FileReq = JsonConvert.DeserializeObject<RequestFile>(e.Data);
                        IsFileRequest = true;
                    }
                    catch (Exception)
                    {
                        IsFileRequest = false;
                        FileReq = null;
                    }
                    // if the received data is Prime number and PublicKey
                    if (IsFileRequest)
                    {
                        if (!Program.IsActive)
                        {
                            ErrorMessage EM = new ErrorMessage();
                            EM.Code = "001";
                            EM.Message = "Sorry!!!, the requested file is on Node ws://127.0.0.1:" + Program.Port.ToString() + " which is DOWN";
                            Send(JsonConvert.SerializeObject(EM));
                            return;
                        }

                        // Generating Prime number, Public key, Private key and ...
                        CyclicGroup.g = CyclicGroup.findPrimitiveRoot(FileReq.PrimeNumber);
                        Crypto.CyclicGroupNumbers = CyclicGroup.FormCyclicGroup(FileReq.PrimeNumber);

                        // public and private key
                        Random R = new Random();
                        int random = R.Next(3, FileReq.PrimeNumber - 1);
                        Program.PrivateKey = Crypto.CyclicGroupNumbers[random];
                        var myPublicKey = CyclicGroup.power(CyclicGroup.g, Program.PrivateKey, FileReq.PrimeNumber);

                        if (myPublicKey < 0)
                            myPublicKey = myPublicKey + FileReq.PrimeNumber;
                        Program.Log("----------Diffi-Hellman Parameters--------");
                        Program.Log($"Prime Number is: {FileReq.PrimeNumber}");
                        Program.Log("");
                        Program.Log($"My PublicKey is: {myPublicKey}");
                        Program.Log("");
                        Program.Log($"Other side Peer PublicKey is: {FileReq.PublicKey}");

                        Program.Log("");
                        Program.Log("SHARED KEY for Encryption:");
                        string EncryptedFileData = Crypto.Encrypt(Program.FileContent, FileReq.PrimeNumber, FileReq.PublicKey, Program.PrivateKey);

                        FileResult FileRes = new FileResult();
                        FileRes.PrimeNumber = FileReq.PrimeNumber;
                        FileRes.PublicKey = myPublicKey;
                        Program.Log("This is the Encrypted File Data ready to send:");
                        Program.Log("");
                        Program.Log("");
                        Program.Log(EncryptedFileData);
                        FileRes.FileData = EncryptedFileData;
                        Send(JsonConvert.SerializeObject(FileRes));
                        Program.Log("--------------------------------------------");
                        Program.Log($"File Sent To " + FileReq.RequestUrl);
                        Program.Log("--------------------------------------------");
                    }
                    //Check if the recevied data is DHT
                    else
                    {
                        bool IsHashTable = false;
                        List<Node> NL = null;
                        //check if the received data is a list of nodes/DHT
                        try
                        {
                            NL = JsonConvert.DeserializeObject<List<Node>>(e.Data);
                            IsHashTable = true;
                        }
                        catch (Exception)
                        {
                            IsHashTable = false;
                            NL = null;
                        }
                        if (IsHashTable)
                        {
                            bool IsChanged = MigrateDHT(Program.DHT, NL);
                            if (IsChanged)
                            {
                                if (Program.IsActive)
                                {
                                    Brodcast(JsonConvert.SerializeObject(Program.DHT));
                                    Program.Client.Broadcast(JsonConvert.SerializeObject(Program.DHT));
                                }
                                else
                                {
                                    Send(JsonConvert.SerializeObject(Program.DHT));
                                }
                            }
                        }
                    }
                }
            }
        }
        //------------------------------------------------------------------------------------------------------------------------
        //------------------------------------------------------------------------------------------------------------------------
        //------------------------------------------------------------------------------------------------------------------------

    }
}
