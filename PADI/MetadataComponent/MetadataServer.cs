using System;
using System.Collections;
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

namespace MetadataComponent
{
    public class MetadataServer
    {
        private CommandService commandService;
        private MetadataRequestService requestService;
        private MetadataMessageService messageService;

        private readonly int __OPEN__ = 1;
        private readonly int __DELETE__ = -1;

        private bool failing;
        private ManualResetEvent freezer;
        private int timestamp, ticket;
        private int port;
        private int base_port;
        private int metadataPort;

        private bool[] onlineMS = { false, false, false }; //state control of metadata servers
        private MetadataServerInfo[] msi; //registered metadata servers to local metadata server

        private Dictionary<string, MetadataContent> metadataDB;
        private SortedSet<DataServerInfo> onlineDataServers; //registered data servers to local metadata server

        private int getTicket()
        {
            return ++(this.ticket);
        }

        private int getTimestamp()
        {
            return ++(this.timestamp);
        }


        public MetadataServer(int port, int baseMetadataServerPort, int metadataPort)
        {
            failing = false;
            freezer = new ManualResetEvent(true);
            timestamp = 0;
            ticket = 0;
            this.port = port;
            this.base_port = baseMetadataServerPort;
            this.metadataPort = metadataPort;

            metadataDB = new Dictionary<string, MetadataContent>();
            onlineDataServers = new SortedSet<DataServerInfo>();
            msi = new MetadataServerInfo[3];

            commandService = new CommandService();
            commandService.FailHandler += Fail;
            commandService.RecoverHandler += Recover;
            commandService.FreezeHandler += Freeze;
            commandService.UnfreezeHandler += Unfreeze;
            commandService.DumpHandler += Dump;

            requestService = new MetadataRequestService();
            requestService.CreateHandler += Create;
            requestService.DeleteHandler += Delete;
            requestService.CloseHandler += Close;
            requestService.OpenHandler += Open;
            requestService.SendCreatePromiseHandler += sendCreatePromise;
            requestService.SendCreateAcceptedHandler += sendCreateAccepted;
            requestService.CreateCommitHandler += createCommit;

            messageService = new MetadataMessageService();
            messageService.RegisterHandler += Register;
            messageService.RegisterDataReplicaHandler += RegisterDataReplica;
            messageService.RegisterMetadataHandler += RegisterMetadata;
            messageService.GetMetadataDtoHandler += GetMetadataDto;
            messageService.SendMetadataDtoHandler += SendMetadataDto;
            messageService.MetadataTransferHandler += MetadataTransfer;

        }

        private void ConnectMetadataServer(int port, int i)
        {
            Console.WriteLine("Connecting to MDS at " + port);
            MetadataMessageService metadataServer = (MetadataMessageService)Activator.GetObject(typeof(MetadataMessageService), "tcp://localhost:" + port + "/MessageService");
            metadataServer.RegisterMetadata(this.metadataPort.ToString());
            onlineMS[i] = true;
        }

        public void metadataseverConnect()
        {
            int metaport;
            for (int i = 0; !failing && i < 3; i++)
            {
                metaport = base_port + i;
                
                try
                {
                    if (!onlineMS[i] && metaport == this.metadataPort) onlineMS[i] = true; //this metadata server so it's always on
                    else if (!onlineMS[i]) ConnectMetadataServer(metaport, i); //connect to other metadata servers
                }
                catch (RemotingException e)
                {
                    Console.WriteLine(e.Message + " -> metadata server " + i + " is offline...");
                    onlineMS[i] = false;
                }
                catch (SocketException e)
                {
                    Console.WriteLine(e.Message + " -> metadata server " + i + " is offline...");
                    onlineMS[i] = false;
                }
            }
        }

        public void Begin()
        {
            Hashtable RemoteChannelProperties = new Hashtable();
            RemoteChannelProperties["port"] = port.ToString();
            RemoteChannelProperties["name"] = "allnet";
            TcpChannel channel = new TcpChannel(RemoteChannelProperties, null, null);
            ChannelServices.RegisterChannel(channel, false);
            Console.WriteLine("Running metadata service at port " + port + " and metadata sync layer at port " + metadataPort);
            RemotingServices.Marshal(commandService, "CommandService", typeof(CommandService));
            RemotingServices.Marshal(requestService, "RequestService", typeof(MetadataRequestService));
            RemotingServices.Marshal(messageService, "MessageService", typeof(MetadataMessageService));
            Console.WriteLine("Ready...");
            SpinWait spinner = new SpinWait();
            while (true)
            {
                dataServerCheck();
                metadataseverConnect();
                spinner.SpinOnce(); //a better way to control spin lock within while
            }
        }

        private void dataServerCheck()
        {
            List<DataServerInfo> offline = new List<DataServerInfo>();
            Monitor.Enter(onlineDataServers);
            foreach (DataServerInfo ds in onlineDataServers)
            {
                if ((ds.lastHeartbeat - DateTime.Now).TotalSeconds > 20) {
                    Console.WriteLine("DataServer timeout: " + ds.address);
                    offline.Add(ds);
                }
            }
            Monitor.Exit(onlineDataServers);
            foreach (DataServerInfo ds in offline) onlineDataServers.Remove(ds);
        }


        // Data server register from data server itself
        public void Register(string address)
        {
            DataServerInfo dsi = new DataServerInfo(address);
            Monitor.Enter(onlineDataServers);
            onlineDataServers.Add(dsi);
            Monitor.Exit(onlineDataServers);
            for (int i = 0; i < msi.GetLength(0); i++)
            {
                MetadataServerInfo mi = msi[i];
                if (mi != null && mi.port != this.metadataPort)
                {
                    try
                    {
                        MetadataMessageService metadataServer = (MetadataMessageService)Activator.GetObject(typeof(MetadataMessageService), "tcp://localhost:" + mi.port + "/MessageService");
                        metadataServer.RegisterDataReplica(dsi.address);
                    }
                    catch (RemotingException e)
                    {
                        onlineMS[mi.getID(base_port)] = false;
                        msi[i] = null;
                    }
                    catch (SocketException e)
                    {
                        onlineMS[mi.getID(base_port)] = false;
                        msi[i] = null;
                    }
                }
            }
          //  Console.WriteLine("DataServer at " + address + " has registered.");
        }

        // Data server register replication
        public void RegisterDataReplica(string address) {
            DataServerInfo dsi = new DataServerInfo(address);
            Monitor.Enter(onlineDataServers);
            onlineDataServers.Add(dsi);
            Monitor.Exit(onlineDataServers);
           // Console.WriteLine("DataServer at " + address + " has registered (replica received from principal metadata server).");
        }

        public void RegisterMetadata(string address) {
            MetadataServerInfo mi = new MetadataServerInfo(address);
            msi[mi.getID(base_port)] = mi;
        //    Console.WriteLine("MetadataServer at " + address + " has registered.");
        }


        public void Fail()
        {
            if (!failing)
            {
                RemotingServices.Disconnect(requestService);
                RemotingServices.Disconnect(messageService);
                failing = true;
            }
            Console.WriteLine("Failing...");
        }

        public void Recover()
        {
            if (failing)
            {
                RemotingServices.Marshal(messageService, "MessageService", typeof(DataMessageService));
                getMetadataDatabase();
                RemotingServices.Marshal(requestService, "RequestService", typeof(DataRequestService));
                failing = false;
            }
            Console.WriteLine("Recovered.");
        }

        private void getMetadataDatabase()
        {
            if (msi == null) throw new System.ApplicationException("MetadataServer.getMetadataDatabase: there are no metadata servers registered in local server... Recover will not be possible!");
            Console.WriteLine("Recovering metadata server number " + (this.metadataPort - this.base_port) + "...");
            for(int i=0; i < msi.GetLength(0); i++)
            {
                MetadataServerInfo mi = msi[i];
                if (mi != null)
                {
                    try
                    {
                        metadataDB = ((MetadataMessageService)Activator.GetObject(typeof(MetadataMessageService), "tcp://localhost:" + mi.port + "/DataMessageService")).MetadataTransfer();
                        int msID = mi.getID(base_port);
                        Console.WriteLine("Metadata successfully tranfered from metadata server number " + msID + ".");
                        return;
                    }
                    catch (Exception e)
                    {
                        onlineMS[mi.getID(base_port)] = false;
                        msi[i] = null;
                    }
                }
            }
        }

        public Dictionary<string, MetadataContent> MetadataTransfer()
        {
            return metadataDB;
        }

        public void Freeze()
        {
            freezer.Reset();
            Console.WriteLine("Freezing...");
        }

        public void Unfreeze()
        {
            freezer.Set();
            Console.WriteLine("Unfrozen.");
        }

        public string Dump()
        {
            //string dump = "MetadataServer at port " + port + " is " + (failing ? "failing" : "functional") + " and " + (freezer.WaitOne(0) ? "unfrozen" : "frozen") + "." + Console.Out.NewLine;
            string dump = "Dumping Metadatas";
            foreach (string file in metadataDB.Keys) dump += "I have file with Name=" + file+"\n";
            dump += "END OF REPORT";
            Console.WriteLine(dump);
            return dump;
        }


        public MetadataDTO Open(string fileName)
        {
            freezer.WaitOne();
            MetadataContent metadata = metadataDB[fileName];
            //metadata.metadataDTO.ticket++;
            metadata.access += __OPEN__;
            Console.WriteLine("File \"" + fileName + "\" opened by client.");
            return metadata.metadataDTO;
        }

        public void Close(string fileName)
        {
            freezer.WaitOne();
            MetadataContent metadata = metadataDB[fileName];
            Console.WriteLine("File \"" + fileName + "\" closed by client."); //move this line to end of operation when this code gets fixed
            if (metadata.access > 0) metadata.access -= __OPEN__; //sam please remove this, the server shouldnt crash just because the client does something stupid
            else throw new System.InvalidOperationException("MetadataServer Close: file not open (access=" + metadata.access + ")");
        }

        /**
         * Consensus Client-Metadata Servers for active replication: send promise
         **/
        public MetadataQuorumDto sendCreatePromise(int nbDataServers, int round)
        {
            MetadataQuorumDto quorumDto = new MetadataQuorumDto();
            quorumDto.Dataservers = onlineDataServers.Take<DataServerInfo>(nbDataServers).ToArray<DataServerInfo>();
            quorumDto.AvailableMetadataServers = onlineMS;
            quorumDto.Round = round;
            quorumDto.ElectedMetadataServer = this.metadataPort;

            return quorumDto;
        }

        /**
         * Consensus Client-Metadata Servers for active replication: send accepted or accepted nack for chosen servers quorum
         **/
        public bool sendCreateAccepted(MetadataQuorumDto dataservers, int nbDataServers)
        {
            dataservers.timestamps[metadataPort - base_port] = getTimestamp();
            foreach (DataServerInfo ds in dataservers.Dataservers)
            {
                if (!this.onlineDataServers.Contains(ds)) return false;
            }

            return true;
        }

        /**
         * Consensus Client-Metadata Servers for active replication: send response to client and save metadata of created file
         **/
        public MetadataDTO createCommit(MetadataQuorumDto dataservers, string fileName, int nbDataServers, int readQuorum, int writeQuorum)
        {
            MetadataDTO metadataDTO;
            freezer.WaitOne();
            //next if-else is for load balancing the file creation task for selected metadata server from client
            if (dataservers.runCreate(metadataPort))
            {
                int ticket = getTicket();
                int timestamp = dataservers.timestamps[metadataPort - base_port];
                Dictionary<string, string> dataServers = CreateFileOnServers(dataservers.Dataservers, fileName, nbDataServers);
                metadataDTO = new MetadataDTO(nbDataServers, readQuorum, writeQuorum, dataServers, ticket);
                MetadataContent metadata = new MetadataContent(metadataDTO, __OPEN__, timestamp);
                metadataDB.Add(fileName, metadata);
                for(int i=0; i < msi.GetLength(0); i++)
                {
                    MetadataServerInfo mi = msi[i];
                    if (mi != null)
                    {
                        //indirect hearbeat for metadata being up or down
                        try
                        {
                            ((MetadataMessageService)Activator.GetObject(typeof(MetadataMessageService), "tcp://localhost:" + mi.port + "/DataMessageService")).SendMetadataDto(fileName, metadataDTO, dataservers);
                            int msID = mi.getID(base_port);
                            Console.WriteLine("Metadata successfully tranfered from metadata server number " + msID + ".");
                        }
                        catch (Exception e)
                        {
                            onlineMS[mi.getID(base_port)] = false;
                            msi[i] = null;
                        }
                    }
                }
                Console.WriteLine("File \"" + fileName + "\" created by client in elected metadata server.");

                return metadataDTO;
            }
            else { throw new System.ApplicationException("createCommit: unexpected run in non elected metadata server"); }

        }

        MetadataDTO GetMetadataDto(string fileName, int electedTimestamp) {
            // TODO: not necessary with active replication in accept (only for optimize the transport of that DTO)
            return metadataDB[fileName].metadataDTO;
        }

        void SendMetadataDto(string fileName, MetadataDTO metadataDTO, MetadataQuorumDto dataservers)
        {
            int timestamp = dataservers.timestamps[metadataPort - base_port];
            MetadataContent metadata = new MetadataContent(metadataDTO, __OPEN__, timestamp);
            metadataDB.Add(fileName, metadata);
            Console.WriteLine("File \"" + fileName + "\" created by client and replicated to this metadata server.");
        }

        // old version not used now
        protected MetadataDTO Create(string fileName, int nbDataServers, int readQuorum, int writeQuorum)
        {
            freezer.WaitOne();
            int ticket = getTicket();
            int timestamp = getTimestamp();
            Dictionary<string, string> dataServers = CreateFileOnServers(fileName, nbDataServers);
            MetadataDTO metadataDTO = new MetadataDTO(nbDataServers, readQuorum, writeQuorum, dataServers, ticket);
            MetadataContent metadata = new MetadataContent(metadataDTO, __OPEN__, timestamp);
            metadataDB.Add(fileName, metadata);
            Console.WriteLine("File \"" + fileName + "\" created by client.");
            return metadataDTO;
        }

        /** Delete metadata
         * more simple than create because it didn't do the creation of the quorum
         **/
        public void Delete(string fileName)
        {
            freezer.WaitOne();
            MetadataContent metadata = metadataDB[fileName];
            deleteFromALLdataServers(metadata.metadataDTO);
            metadata.access -= __DELETE__; //What you're doing here is exactly the same you're doing in open... Doesn't make much sense...
            /* DELETE equals -1, so doing access -= DELETE is the same as += OPEN
             * if this is for checking how many clients have the file open, just erase this access thing... 
             * using an integer to check how many clients have the file open is not going to work, no matter what you do */
            Console.WriteLine("File \"" + fileName + "\" deleted by client.");
        }

        


        private Dictionary<string, string> CreateFileOnServers(string fileName, int nbDataServers)
        {
            string localFileName;
            Dictionary<string, string> servers = new Dictionary<string, string>();
            //iterates over the nbDataServers with less file load
            foreach (DataServerInfo ds in this.onlineDataServers.Take<DataServerInfo>(nbDataServers))
            {
                localFileName = ((DataMessageService)Activator.GetObject(typeof(DataMessageService), "tcp://localhost:" + ds.address + "/DataMessageService")).CreateNewFile();
                ds.numFiles++;
                servers.Add(ds.address, localFileName);
                Console.WriteLine("File \"" + fileName + "\" created on DataServer at " + ds.address + " with name \"" + localFileName + "\".");
                Console.WriteLine("This DataServer now has " + ds.numFiles + " files.");
            }
            return servers;
        }

        private Dictionary<string, string> CreateFileOnServers(DataServerInfo[] dataservers, string fileName, int nbDataServers)
        {
            string localFileName;
            Dictionary<string, string> servers = new Dictionary<string, string>();
            //iterates over the nbDataServers with less file load
            foreach (DataServerInfo ds in dataservers)
            {
               
                try
                {
                    Console.WriteLine("File \"" + fileName + "\" will be created.");
                    DataMessageService dms = (DataMessageService)Activator.GetObject(typeof(DataMessageService), "tcp://localhost:" + ds.address + "/MessageService");
                    localFileName = dms.CreateNewFile();
                    ds.numFiles++;
                    servers.Add(ds.address, localFileName);
                    Console.WriteLine("File \"" + fileName + "\" created on DataServer at " + ds.address + " with name \"" + localFileName + "\".");
                    Console.WriteLine("This DataServer now has " + ds.numFiles + " files.");
                }
                catch (Exception e)
                {
                    Console.Write(e.Message);
                }
                 
            }
            return servers;
        }

        private void deleteFromALLdataServers(MetadataDTO metadataDTO)
        {
            int n;
            foreach (ServerDTO ds in metadataDTO.dataServers)
            {
                ((DataMessageService)Activator.GetObject(typeof(DataMessageService), "tcp://localhost:" + ds.address + "/DataMessageService")).DeleteFile(ds.localFileName);
                n = (onlineDataServers.First(info => info.address == ds.address).numFiles--);
                Console.WriteLine("Local file \"" + ds.localFileName + "\" deleted on DataServer at " + ds.address + ".");
                Console.WriteLine("This DataServer now has " + n + " files.");
            }
        }


        static void Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine("Metadata server needs 3 parameters for starting: int port, int baseMetadataServerPort and int metadataPort");
                return;
            }
            // parameters: int port, int baseMetadataServerPort, int metadataPort
            MetadataServer server = new MetadataServer(int.Parse(args[0]), int.Parse(args[1]), int.Parse(args[2]));
            server.Begin();
        }
    }
}


