using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using WebSocketSharp;

namespace FileSharingSystem
{
    public class ClientPeer2Peer
    {
        public IDictionary<string, WebSocket> wsDict = new Dictionary<string, WebSocket>();

        public object BinaryData { get; private set; }

        //=======================================================================================================
        //====================================  Merging Distributed Hash Tables   ===============================
        //=======================================================================================================
        private bool MigrateDHT(List<Node> MainDHT, List<Node> ClientDHT)
        {
            bool IsChanged = false;
            if (MainDHT == null || ClientDHT == null)
                return IsChanged;

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
        //================  connecting and sending data to other Peer and DHT by this function   ================
        //=======================================================================================================
        public void Connect(string url, string data, bool IsForFileRequest)
        {
            WebSocket ws = null;
            if (!wsDict.TryGetValue(url, out ws))
            {
                ws = new WebSocket(url);

                ws.OnMessage += Message;
                ws.Connect();
                wsDict.Add(url, ws);
            }
            if (IsForFileRequest)
                ws.Send(data); //this send() function will fire the OnMessage() function in server node
            else
                ws.Send(JsonConvert.SerializeObject(Program.DHT)); // Sending DHT to the other side Peer to be synced
        }

        //=======================================================================================================
        //=======================  the message event that will be fired when data is sent to this user   ========
        //=======================================================================================================
        public void Message(object sender, MessageEventArgs e)
        {
            if (e.IsText && e.Data.Length > 0)
            {
                bool IsFileResult = false;
                FileResult FileRes = null;
                //handling the error
                try
                {
                    FileRes = JsonConvert.DeserializeObject<FileResult>(e.Data);
                    if (FileRes.FileData == null && FileRes.PublicKey == 0 && FileRes.PrimeNumber == 0)
                        IsFileResult = false;
                    else
                        IsFileResult = true;
                }
                catch (Exception)
                {
                    IsFileResult = false;
                    FileRes = null;
                }
                //if the decrypted file is successfully received
                if (IsFileResult)
                {
                    Program.Log("------- Diffi-Hellman Parameters--------");
                    Program.Log("");
                    Program.Log($"Prime : {FileRes.PrimeNumber}");
                    Program.Log("");
                    Program.Log($"My PublicKey : {Program.PublicKey}");
                    Program.Log("");
                    Program.Log($"Other Side PublicKey : {FileRes.PublicKey}");
                    Program.Log("");
                    Program.Log("SHARED KEY for Decryption:");
                    string DecodedFileData = Crypto.Decrypt(FileRes.FileData, FileRes.PrimeNumber, FileRes.PublicKey, Program.PrivateKey);
                    Program.Log("");
                    Program.Log("--------------------------------------------");
                    Program.Log("Recieved File Content :");
                    Program.Log("");
                    Program.Log(DecodedFileData);
                }
                //If the received data is DHT
                else
                {
                    bool IsHashTable = false;
                    List<Node> NL = null;
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
                        if (Program.IsActive)
                        {
                            bool IsChanged = MigrateDHT(Program.DHT, NL);

                            //Program.Log("DHT Node Count = " + Program.DHT.Count.ToString() + Environment.NewLine);
                            //foreach (var nd in Program.DHT)
                            //{
                            //    Program.Log(nd.Name + "::::" + nd.Url + Environment.NewLine);
                            //}

                            if (IsChanged)
                                Program.Server.Brodcast(JsonConvert.SerializeObject(Program.DHT));
                            CheckServerActiveStatus();
                        }

                    }
                    // If the received data is a Error Messages
                    else
                    {
                        bool IsErrorMessage = false;
                        ErrorMessage EM = null;
                        try
                        {
                            EM = JsonConvert.DeserializeObject<ErrorMessage>(e.Data);
                            IsErrorMessage = true;
                        }
                        catch (Exception)
                        {
                            IsErrorMessage = false;
                            EM = null;
                        }

                        if (IsErrorMessage)
                        {
                            Program.Log("REQUEST FAILED : " + EM.Message);
                        }
                    }
                }
            }
        }
        //=======================================================================================================
        //=======================  Disconnects websockets connected to a Down node ===============================
        //=======================================================================================================
        public bool CheckServerActiveStatus()
        {
            List<string> PortList = new List<string>();
            bool Status = false;
            List<clsRemove> RemoveList = new List<clsRemove>();
            foreach (var item in Program.DHT)
            {
                if (!item.IsActive)
                {
                    foreach (var wsitem in wsDict)
                    {
                        if (wsitem.Value.Url.Port.ToString() == item.Url)
                        {
                            DisconnectedClient DC = new DisconnectedClient();
                            DC.MyUrl = Program.Port.ToString();
                            wsitem.Value.Send(JsonConvert.SerializeObject(DC));
                            wsitem.Value.Close();
                            Program.Log("You Cannot Connect");

                            Program.Log(wsitem.Key + " is  Disconnected");
                            PortList.Add(wsitem.Key);
                            Status = true;

                            var ND = Program.DHT.Where(i => i.Url == DC.MyUrl).DefaultIfEmpty(null).First();
                            if (ND != null)
                            {
                                clsRemove Rm = new clsRemove();
                                Rm.ND = ND;
                                Rm.url = "ws://127.0.0.1:" + item.Url.ToString() + ",";
                                RemoveList.Add(Rm);
                            }
                        }
                    }
                }

            }

            //foreach (var rm in RemoveList)
            //{
            //    if (rm.ND.ServerList == rm.url)
            //        Program.DHT.Remove(rm.ND);
            //    else
            //        rm.ND.ServerList.Replace(rm.url, "");
            //}

            foreach (var item in PortList)
            {
                wsDict.Remove(item);
            }
            return Status;
        }
        //=======================================================================================================
        //=====================================Sending data through websocket====================================
        //=======================================================================================================
        public void Send(string url, string data)
        {
            foreach (var item in wsDict)
            {
                if (item.Key == url)
                {
                    item.Value.Send(data);
                }
            }
        }

        //=======================================================================================================
        //==========================  broadcast data to all connected nodes =====================================
        //=======================================================================================================
        public void Broadcast(string data)
        {
            foreach (var item in wsDict)
            {
                item.Value.Send(data);
            }
        }

        //=======================================================================================================
        //=======================================================================================================
        //=======================================================================================================
        public IList<string> GetServers()
        {
            IList<string> servers = new List<string>();
            foreach (var item in wsDict)
            {
                servers.Add(item.Key);
            }
            return servers;
        }

        //=======================================================================================================
        //========================================= Close Websocket ===========================================
        //=======================================================================================================
        public void Close()
        {
            foreach (var item in wsDict)
            {
                item.Value.Close();
            }
        }

        //=======================================================================================================
        //======================================= Close Websocket ===========================================
        //=======================================================================================================

        public void Close(string url)
        {
            foreach (var item in wsDict)
            {
                if (item.Value.Url.AbsoluteUri == url)
                    item.Value.Close();
            }
        }

        //=======================================================================================================
        //=======================================================================================================
        //=======================================================================================================

    }
}
