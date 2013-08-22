using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Net.Sockets;
using Shared;
using Shared.Services;
using Shared.Services.Command;
using Shared.Services.Request;
using Shared.Services.Message;

namespace DataComponent {
    public class DataServer {
        private CommandService commandService;
        private DataRequestService requestService;
        private DataMessageService messageService;
        private int port;

        private readonly int baseMetadataPort;

        private bool failing;
        private ManualResetEvent freezer;

        private long uniqueFileId;

        private class Data {
            public byte[] content;
            public int version;
            public int nextVersion;
            //private Dictionary<int, int> sessions;
            private int biggestTieBreaker;
            public Data() : this(null) { }

            public Data(byte[] content) : this(content, 0) { }

            public Data(byte[] content, int version) {
                //this.sessions = new Dictionary<int,int>();
                this.content = content;
                this.version = version;
                this.nextVersion = version;
                this.biggestTieBreaker = -1;
            }

            public void Update(byte[] content, int version, int tieBreaker) {
                if (version > this.version || (version == this.version && tieBreaker > biggestTieBreaker)) {
                    this.content = content;
                    this.version = version;
                    biggestTieBreaker = tieBreaker;
                }                
            }

            /*
            public int Session(int version, int tieBreaker) {
                if(version <= this.version) return -1;
                if (version == this.nextVersion) {
                    if (tieBreaker > biggestTieBreaker) {
                        bool searching = true;
                        int sessionid = 0;
                        while (searching) {
                            if (sessions.ContainsKey(sessionid)) sessionid++;
                            else searching = false;
                        }
                        sessions.Add(sessionid, tieBreaker);
                        biggestTieBreaker = tieBreaker;
                        return sessionid;
                    }
                    else return tieBreaker == biggestTieBreaker ? -2 : -1;
                }
                else if(version > this.nextVersion) {
                    bool searching = true;
                    int sessionid = 0;
                    while (searching) {
                        if (sessions.ContainsKey(sessionid)) sessionid++;
                        else searching = false;
                    }
                    sessions.Clear();
                    sessions.Add(sessionid, tieBreaker);
                    biggestTieBreaker = tieBreaker;
                    nextVersion = version;
                    return sessionid;
                }
                return -1;
            }

            public void Update(byte[] content, int version, int sessionid) {
                if (sessions[sessionid] == biggestTieBreaker && version == nextVersion) {
                    this.content = content;
                    this.version = version;
                }
                sessions.Clear();
            }

            public void Cancel(int sessionid) {
                if (sessions.ContainsKey(sessionid)) {
                    if (sessions[sessionid] == biggestTieBreaker) {
                        biggestTieBreaker = 0;
                        foreach (int breaker in sessions.Values) {
                            if (breaker > biggestTieBreaker) biggestTieBreaker = breaker;
                        }
                    }
                    sessions.Remove(sessionid);
                }
            }
             * */
        }

        private Dictionary<string, Data> files;

        public DataServer(int port, int baseMetadataServerPort) {
            failing = false;
            freezer = new ManualResetEvent(true);
            uniqueFileId = 0;
            this.port = port;
            this.baseMetadataPort = baseMetadataServerPort;
            this.files = new Dictionary<string,Data>();

            commandService = new CommandService();
            commandService.FailHandler += Fail;
            commandService.RecoverHandler += Recover;
            commandService.FreezeHandler += Freeze;
            commandService.UnfreezeHandler += Unfreeze;

            requestService = new DataRequestService();
            requestService.ReadHandler += Read;
            requestService.WriteHandler += Write;
            //requestService.WriteableHandler += IsWriteable;
            //requestService.ReadableHandler += IsReadable;
            //requestService.CancelHandler += Cancel;
            requestService.VersionHandler += GetVersion;

            messageService = new DataMessageService();
            messageService.CreateNewFileHandler += NewFile;
            messageService.DeleteFileHandler += DeleteFile;
            messageService.CopyFileHandler += CopyFile;
            messageService.PasteFileHandler += PasteFile;
        }


        public void Fail() {
            if (!failing) {
                RemotingServices.Disconnect(requestService);
                RemotingServices.Disconnect(messageService);
                failing = true;
            }
            Console.WriteLine("Failing...");
        }

        public void Recover() {
            if (failing) {
                RemotingServices.Marshal(requestService, "RequestService", typeof(DataRequestService));
                RemotingServices.Marshal(messageService, "MessageService", typeof(DataMessageService));
                failing = false;
            }
            Console.WriteLine("Recovered.");
        }

        public void Freeze() {
            freezer.Reset();
            Console.WriteLine("Freezing...");
        }

        public void Unfreeze() {
            freezer.Set();
            Console.WriteLine("Unfrozen.");
        }

        public string Dump() {
            string dump = "DataServer at port " + port + " is "  + (failing ? "failing" : "functional") + " and " + (freezer.WaitOne(0) ? "unfrozen" : "frozen") + "." + Console.Out.NewLine;
            foreach (string filename in files.Keys) {
                dump += "Local file: " + filename + "; Version: " + files[filename].version + "; Content: [" + files[filename].content + "];" + Console.Out.NewLine;
            }
            dump += "END OF REPORT";
            return dump;
        }


        
        public void Write(string localFileName, byte[] content, int version, int tieBreaker) {
            freezer.WaitOne();
            //this is still not good because threads will need to be synchronized here
            if (files.ContainsKey(localFileName)) {
                files[localFileName].Update(content, version, tieBreaker);
                Console.WriteLine("Data written to local \"" + localFileName + "\", version " + version + ".");
            }
            else {
                //files.Add(localFileName, new Data(content));
                Console.WriteLine("File not found: \"" + localFileName + "\"");
            }
        }

        public DataDTO Read(string localFileName, Semantics semantics) {
            freezer.WaitOne();
            Data data = files[localFileName];
            Console.WriteLine("\"" + localFileName + "\" read by a client.");
            return new DataDTO(data.version, data.content);
        }

        /*
        public int IsWriteable(string localFileName, int version, int tieBreaker) {
            freezer.WaitOne();
            return files[localFileName].Session(version, tieBreaker);
        }

        public int IsReadable(string localFileName, int version) {
            freezer.WaitOne();
            return files.ContainsKey(localFileName) ? 0 : -1;
        }

        public void Cancel(string localFileName, int sessionid) {
            freezer.WaitOne();
            if (files.ContainsKey(localFileName)) files[localFileName].Cancel(sessionid);
        }
        */
        public int GetVersion(string localFileName) {
            freezer.WaitOne();
            if (files.ContainsKey(localFileName)) return files[localFileName].version;
            else return -1;
        }




        public string NewFile() {
            freezer.WaitOne();
            string fileName = "file" + uniqueFileId++; //should improve by reusing deleted Ids
            files.Add(fileName, new Data());
            Console.WriteLine("File \"" + fileName + "\" was created.");
            return fileName;
        }

        public void DeleteFile(string localFileName) {
            freezer.WaitOne();
            files.Remove(localFileName);
            Console.WriteLine("File \"" + localFileName + "\" was deleted.");
        }

        public DataDTO CopyFile(string localFileName) {
            freezer.WaitOne();
            Data data = files[localFileName];
            Console.WriteLine("File \"" + localFileName + "\" was read by a Metadata server for replication.");
            return new DataDTO(data.version, data.content);
        }

        public string PasteFile(DataDTO file) {
            freezer.WaitOne();
            string fileName = "file" + uniqueFileId++; //should improve by reusing deleted Ids
            files.Add(fileName, new Data(file.content, file.version));
            Console.WriteLine("File \"" + fileName + "\" was replicated.");
            return fileName;
        }



        private int ConnectMetadataServer() {
            int metaport = baseMetadataPort + new Random(System.DateTime.Now.Millisecond).Next() % 3;
            while (true) {
                try {
                    ConnectMetadataServer(metaport);
                    return metaport;
                }
                catch (RemotingException e) {
                    Console.WriteLine(e.Message + " Retrying...");
                    if (metaport < baseMetadataPort + 2) metaport++;
                    else metaport = baseMetadataPort;
                }
                catch (SocketException e) {
                    Console.WriteLine(e.Message + " Retrying...");
                    if (metaport < baseMetadataPort + 2) metaport++;
                    else metaport = baseMetadataPort;
                }
            }
        }
        private int ConnectMetadataServer(int port) {
            Console.WriteLine("Connecting to MDS at " + port);
            MetadataMessageService metadataServer = (MetadataMessageService)Activator.GetObject(typeof(MetadataMessageService), "tcp://localhost:" + port + "/MessageService");
            metadataServer.Register(this.port.ToString());
            return port;
        }


        public void Begin() {
            TcpChannel channel = new TcpChannel(port);
            ChannelServices.RegisterChannel(channel, false);
            Console.WriteLine("Running at port " + port);
            RemotingServices.Marshal(commandService, "CommandService", typeof(CommandService));
            RemotingServices.Marshal(requestService, "RequestService", typeof(DataRequestService));
            RemotingServices.Marshal(messageService, "MessageService", typeof(DataMessageService));
            Console.WriteLine("Ready.");

            int metaport = ConnectMetadataServer();
            while (true) {
                freezer.WaitOne();
                try {
                    if (!failing) ConnectMetadataServer(metaport); //heartbeat
                }
                catch (Exception e) {
                    Console.WriteLine("Connection to MDS at " + metaport + " lost.");
                    metaport = ConnectMetadataServer();
                }
                Thread.Sleep(10000);
            }
        }


        static void Main(string[] args) {
            if(args.Length < 2) return;
            DataServer server = new DataServer(int.Parse(args[0]), int.Parse(args[1]));
            server.Begin();
        }
    }
}
