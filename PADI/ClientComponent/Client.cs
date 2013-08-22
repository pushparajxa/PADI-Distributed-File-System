using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Shared.Script;
using Shared.Services.Command;
using Shared.Services.Message;
using Shared.Services.Request;
using Shared.Services;
using Shared;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Collections;
using System.Threading;
namespace ClientComponent {
    class Client {
        private CommandService cs;
        private CommandParser parser;
        private int port;
        private int[] mds;
        MetadataRequestService[] mrs;
        Metadata[] fileRegisters = new Metadata[10];
        int currentFileRegister = 0;//Used in addfileregister
        Data[] dataRegisters = new Data[10];
        int currentDataRegister = 0;
        Hashtable dataServices = new Hashtable();
        Data dataRegisterForCopy;
        Hashtable fileDetails = new Hashtable();
        private Random MDSOpRandom = new Random();

        private class Server {
            public readonly string address;
            public readonly string localFileName;

            public Server(ServerDTO dto) {
                this.address = dto.address;
                this.localFileName = dto.localFileName;
            }
        }

        private class Metadata {
            public readonly int nbDataServers;
            public readonly int readQuorum;
            public readonly int writeQuorum;
            public readonly Server[] dataServers;
            public readonly int ticket;
            public readonly string fileName;

            public Metadata(MetadataDTO dto, string fileName) {
                this.nbDataServers = dto.nbDataServers;
                this.readQuorum = dto.readQuorum;
                this.writeQuorum = dto.writeQuorum;
                this.ticket = dto.ticket;
                this.dataServers = new Server[dto.dataServers.Length];
                this.fileName = fileName;
                for (int i = 0; i < dataServers.Length; i++) {
                    this.dataServers[i] = new Server(dto.dataServers[i]);
                }
            }
        }

        private class Data {
            public readonly int version;
            public readonly byte[] content;
            public Data(int version, byte[] content)
            {
                this.version = version;
                this.content = content;
            }
            public Data(DataDTO dto) {
                this.version = dto.version;
                this.content = dto.content;
            }
        }

        public Client(int clientPort, int MDSPort) {
            this.port = clientPort;
            cs = new CommandService();
            cs.CloseHandler += new CloseCommandHandler(this.Close);
            cs.OpenHandler += new OpenCommandHandler(this.Open);
            cs.ReadHandler += new ReadCommandHandler(this.Read);
            cs.WriteHandler += new WriteCommandHandler(this.Write);
            cs.WriteRegisterHandler += new WriteRegisterCommandHandler(this.WriteRegister);
            cs.CopyHandler += new CopyCommandHandler(this.Copy);
            cs.DumpHandler += new DumpCommandHandler(this.Dump);
            cs.DeleteHandler += new DeleteCommandHandler(this.Delete);
            cs.ScriptHandler += new ScriptCommandHandler(this.ExeScript);
            cs.CreateHandler += new CreateCommandHandler(this.Create);

            parser = new CommandParser();
            parser.DumpCommand += Dump;
            parser.CreateCommand += Create;
            parser.DeleteCommand += Delete;
            parser.OpenCommand += Open;
            parser.CloseCommand += Close;
            parser.ReadCommand += Read;
            parser.WriteRegisterCommand += WriteRegister;
            parser.WriteCommand += Write;
            parser.CopyCommand += Copy;

            mds = new int[3];
            mds[0] = MDSPort; mds[1] = MDSPort + 1; mds[2] = MDSPort + 2;
            mrs = new MetadataRequestService[3];
        }

        // static public Hashtable liveClient = new Hashtable();
        public void Begin() {
            TcpChannel channel = new TcpChannel(port);
            ChannelServices.RegisterChannel(channel, false);
            Console.WriteLine("Client Component Started at port " + port);
            RemotingServices.Marshal(cs, "CommandService", typeof(CommandService));
            initProxy(mrs);
            while (true) ;
        }


        static void Main(string[] args) {
            if (args.Length < 2) return;
            Client clnt = new Client(Convert.ToInt32(args[0]), Convert.ToInt32(args[1]));
            clnt.Begin();
        }
        public void print(String str) {
            //Handle Debug information. For now redirecting the input to the console.
            Console.WriteLine(str);
        }
        public void ExeScript(String file) {
            parser.LoadFile(file);
            parser.ExecuteAll();
        }



        public void Delete(string process, String fileName) { Delete(fileName); }
        public void Delete(String fileName) {
            print("Delete Function Called with fileName=" + fileName);
            if (fileDetails.ContainsKey(fileName)) {
                Indices inde = (Indices)fileDetails[fileName];
                if (inde.getFileRegisterIndex() != -1) {
                    if (fileRegisters[inde.getFileRegisterIndex()].fileName.Equals(fileName)) {
                        fileRegisters[inde.getFileRegisterIndex()] = null;
                    }
                    else {
                        //Do not remove this fileRegister as it has been replaced by some other fileregister.
                    }
                }

                fileDetails.Remove(fileName);
            }
            //int i = MDSOpRandom.Next() % 3;
            //client responsible to delete metadata in all metadata server (most simple way for fail-stop inconsistency in active replication aproach)
            for (int cnt = 0; cnt < this.mrs.GetLength(0);  cnt++)
            {
                try
                {
                    mrs[cnt].Delete(fileName);
                }
                catch (Exception e)
                {
                    print("Exception in Contacting MDS[" + cnt + "]for Delete Command" + e.ToString());
                }
                //i = (i + 1) % 3;
            }
            print("Completed Delete Function for fileName= " + fileName);
        }

        public void Dump(string p) { print(Dump()); }
        public String Dump() {
            print("Processing Dump Command");
            String all_Contents = "Dumping contents at Client with Port=" + this.port;

            String byte_arrays = "Printing Byte-Array-Registers\n";

            int i = 0;
            for (i = 0; i < 10; i++) {
                if (dataRegisters[i] != null) {
                    byte_arrays = byte_arrays + "Register Number=" + i + ". Version=" + dataRegisters[i].version + ". Data=" + Encoding.ASCII.GetString(dataRegisters[i].content) + ".\n";

                }
                else {
                    byte_arrays = byte_arrays + "Register Number=" + i + ", does not have any contents.\n";
                }
            }

            String file_registers = "Printing File-Registers";
            for (i = 0; i < 10; i++) {
                if (fileRegisters[i] != null) {
                    file_registers = file_registers + "Register Number=" + i + ". FileName=" + fileRegisters[i].fileName + ". NumberOfDataServers=" + fileRegisters[i].nbDataServers + ". ReadQuorum=" + fileRegisters[i].readQuorum + ". WriteQuroum=" + fileRegisters[i].writeQuorum + ".\n";
                }
                else {
                    file_registers = file_registers + "Register Number=" + i + ", does not have any contents.\n";
                }
            }
           // print(all_Contents + byte_arrays + file_registers);
            print("Dump Command Completed");
            
            return all_Contents + byte_arrays + file_registers;
        }

        public void Copy(string process, int fileRegister1, Semantics semantics, int fileRegister2, byte[] salt) { Copy(fileRegister1, semantics, fileRegister2, salt); }
        public void Copy(int fileRegister1, Semantics semantics, int fileRegister2, byte[] salt) {
            print("Copy command execution started");
            /* Read the contents into the dataRegisterForCopy */
            Read(fileRegister1, semantics, -1);
            /*Add the salt */
            byte[] newContent = new byte[dataRegisterForCopy.content.Length + salt.Length];
            dataRegisterForCopy.content.CopyTo(newContent, 0);
            salt.CopyTo(newContent, dataRegisterForCopy.content.Length);
            /*Store the NewContent in  */
            //Write(fileRegister2, newContent);
            Console.WriteLine("Storing the new contenst in dataRegister=" + fileRegister2);
            Data d = new Data(1,newContent);

            addContentRegister(d,fileRegister2);
            print("Copy Command execution complete");

        }

        public void Write(string process, int fileRegister, byte[] content) { Write(fileRegister, content); }
        public void Write(int fileRegister, byte[] content) {
            print("Write Command for fileRegister " + fileRegister + " started executing.");
            Metadata md = fileRegisters[fileRegister];
            Server[] sDTO = md.dataServers;

            if (sDTO.Length < md.writeQuorum)
            {
                Console.WriteLine("To send the write request I have DatServers less than the writeQuorum. So I will try to open the file again.");
                this.Open(md.fileName);
                md = fileRegisters[fileRegister];
                sDTO = md.dataServers;
                Console.WriteLine("After Open Request the number of dataServers are " + sDTO.Length);
            }


            DataRequestService[] drs = new DataRequestService[md.nbDataServers];
            int maxVersionAtDS = getMaxVersionForWrite(fileRegister, md, sDTO, drs);
            Random rand = new Random(md.ticket);
            int randValue = rand.Next();
            ArrayList failedServers = new ArrayList();
            ArrayList possibleServers = new ArrayList();


            int i = 0; int cnt = 0;
            int requiredQuorum = sDTO.Length < md.writeQuorum ? sDTO.Length : md.writeQuorum;
            for (i = 0; i < sDTO.GetLength(0); i++)
            {
                try
                {
                    drs[i].Write(sDTO[i].localFileName, content, maxVersionAtDS+1, md.ticket);
                    cnt++;
                    if (cnt >= requiredQuorum)
                        break;
                }
                catch (Exception e)
                {
                    failedServers.Add(i);
                }
            }

            if (cnt < requiredQuorum)
            {
                i = 0; int index;
                while (cnt < requiredQuorum)
                {
                    index = (int)failedServers[i];
                    try
                    {
                        drs[index].Write(sDTO[index].localFileName, content, maxVersionAtDS+1, md.ticket);
                        failedServers.RemoveAt(i);
                        cnt++;
                    }
                    catch (Exception e)
                    {
                        //Data Server failed again.
                    }
                    i = (i + 1) % failedServers.Count;
                }
            }
            
            print("Write Command Complete for  " + fileRegister + " completed executing.");
        }

        public void WriteRegister(string process, int fileRegister, int ContentRegister) { WriteRegister(fileRegister, ContentRegister); }
        public void WriteRegister(int fileRegister, int contentRegister) {
            Write(fileRegister, dataRegisters[contentRegister].content);
        }

        private int getMaxVersionForWrite(int fileRegister, Metadata md1, Server[] sDTO1, DataRequestService[] drs1) {
            Metadata md = md1;
            Server[] sDTO = sDTO1;
            DataRequestService[] drs = drs1;
            int i = 0;
            int rajMax=0 ;
            if (sDTO.GetLength(0) <= md.nbDataServers)
            {
                rajMax = sDTO.GetLength(0);
            }
            else
            {
                //do nothing. This case never exits.
            }
            for (i = 0; i < rajMax; i++)
            {
                if (dataServices.ContainsKey(sDTO[i].address)) {
                    //do nothing;
                }
                else { //Get the proxy and store it in the Hashtable for future.
                    dataServices.Add(sDTO[i].address, (DataRequestService)Activator.GetObject(
                    typeof(DataRequestService),
                   "tcp://localhost:" + sDTO[i].address + "/RequestService"));

                }
                //populate the Array.
                drs[i] = (DataRequestService)dataServices[sDTO[i].address];

            }

            int[] versions = new int[md.nbDataServers];
            int k = 0; int cnt = 0; int maxVersion; int maxId = 0;
            ArrayList failedDataServers = new ArrayList();
            //Initialize versions
            for (k = 0; k < md.nbDataServers; k++) {
                versions[k] = -3;
            }

            for (k = 0; k < md.nbDataServers; k++) {

                try {
                    versions[k] = (drs[k].GetVersion(sDTO[k].localFileName));
                    cnt++;
                    if (cnt >= md.writeQuorum)
                        break;
                }
                catch (Exception e) {
                    failedDataServers.Add(k);
                    //DataServer is Failed.
                }
            }
            if (cnt < rajMax)
            {
                k = 0; int index;
                while (cnt < rajMax)
                {
                    if (k >= failedDataServers.Count || k<0)
                        break;
                    index = (int)failedDataServers[k];
                    try {
                        versions[index] = (drs[index].GetVersion(sDTO[index].localFileName));
                        cnt++;
                        failedDataServers.RemoveAt(k);
                    }
                    catch (Exception e) {
                        //DataServer is still failed.
                    }
                    if (failedDataServers.Count != 0)
                        k = (k + 1) % failedDataServers.Count;
                    else
                        break;
                }
            }

            //Get the maximum version 
            maxVersion = 0;
            for (k = 0; k < md.nbDataServers; k++) {
                if (versions[k] >= maxVersion) {
                    maxVersion = versions[k];
                    maxId = k;
                }

            }
            print("The maximum Version is " + maxVersion);
            return maxVersion;

        }


        public void initProxy(MetadataRequestService[] mrs) {
            print("Initiating Connection to Meta Data Servers");
            int i = 0;
            for (i = 0; i < mrs.GetLength(0); i++) {
                try {
                    mrs[i] = (MetadataRequestService)Activator.GetObject(
                    typeof(MetadataRequestService),
                     "tcp://localhost:" + this.mds[i] + "/RequestService");
                    Console.WriteLine("Connection Success to the MetaData Server at port="+this.mds[i]);
                }
                catch (Exception e) {

                    Console.WriteLine("Exception in Client while getting MetaDataService Objects. For i=" + i + "and connection port=" + mds[i]);
                    Console.WriteLine(e.StackTrace);
                }
            }
        }

        public void Read(string process, int fileRegister, Semantics semantics, int contentRegister) { Read(fileRegister, semantics, contentRegister); }
        public void Read(int fileRegister, Semantics semantics, int contentRegister) {
            print("Read Command Started for fileRegister=" + fileRegister + ",Semantics=" + semantics + ",ContentRegister= " + contentRegister);
            Metadata md = fileRegisters[fileRegister];
            Server[] sDTO = md.dataServers;
            DataRequestService[] drs = new DataRequestService[md.nbDataServers];


            while (sDTO.Length < md.readQuorum)
            {
                try
                {
                    Console.WriteLine("To send the Read request I have DatServers="+sDTO.Length+" less than the ReadQuorum "+ md.readQuorum+". So I will try to open the file again.");
                    this.Open(md.fileName);
                    md = fileRegisters[fileRegister];
                    sDTO = md.dataServers;
                    Console.WriteLine("After Open Request the number of dataServers are " + sDTO.Length);
                }
                catch (Exception e)
                {

                }
                
            }
            
            
            
            int i = 0;
            for (i = 0; i < sDTO.GetLength(0); i++)
            {
                try
                {
                    if (dataServices.ContainsKey(sDTO[i].address))
                    {
                        //do nothing;
                    }
                    else
                    { //Get the proxy and store it in the Hashtable for future.
                        dataServices.Add(sDTO[i].address, (DataRequestService)Activator.GetObject(
                        typeof(DataRequestService),
                        sDTO[i].address));

                    }
                    //populate the Array.
                    drs[i] = (DataRequestService)dataServices[sDTO[i].address];
                }
                catch (Exception e)
                {

                }
                

            }


            if (semantics.Equals(Semantics.DEFAULT)) {
                int[] versions = new int[md.nbDataServers];
                int k = 0; int cnt = 0; int maxVersion; int maxId = 0;
                ArrayList failedDataServers = new ArrayList();
                //Initialize versions
                for (k = 0; k < md.nbDataServers; k++) {
                    versions[k] = -3;
                }
                //Try for readquorum number of DataServers.
                for (k = 0; k < md.readQuorum; k++) {

                    try {
                        versions[k] = (drs[i].GetVersion(sDTO[k].localFileName));
                        cnt++;
                    }
                    catch (Exception e) {
                        failedDataServers.Add(k);
                        //DataServer is Failed.
                    }
                }
                if (cnt < md.readQuorum) {
                    for (k = md.readQuorum; k < md.nbDataServers; k++) {
                        try {
                            versions[k] = (drs[i].GetVersion(sDTO[k].localFileName));
                            cnt++;
                            if (cnt >= md.readQuorum)
                                break;
                        }
                        catch (Exception e) {
                            failedDataServers.Add(k);
                            //DataServer is Failed.
                        }
                    }
                    k = 0; int index;
                    while (cnt < md.readQuorum) {
                        index = (int)failedDataServers[k];
                        try {
                            versions[index] = (drs[index].GetVersion(sDTO[index].localFileName));
                            cnt++;
                            failedDataServers.RemoveAt(k);
                        }
                        catch (Exception e) {
                            //DataServer is still failed.
                        }
                        if(failedDataServers.Count!=0)
                        k = (k + 1) % failedDataServers.Count;
                    }
                }

                //Get the maximum version 
                maxVersion = 0;
                for (k = 0; k < md.nbDataServers; k++) {
                    if (versions[k] >= maxVersion) {
                        maxVersion = versions[k];
                        maxId = k;
                    }

                }

                //Read and store the latest Version
                String fileName = fileRegisters[fileRegister].fileName;

                if (fileDetails.ContainsKey(fileName)) {
                    if (((Indices)fileDetails[fileName]).getLatestVersionSeen() < maxVersion) {
                        ((Indices)fileDetails[fileName]).setLatestVersionSeen(maxVersion);
                    }

                }

                Data contents = new Data(drs[maxId].Read(sDTO[maxId].localFileName, Semantics.DEFAULT));
                if (contentRegister == -1) {
                    dataRegisterForCopy = contents;
                }
                else {
                    addContentRegister(contents, contentRegister);
                }



            }

            else//semantics is Monotonic.
            {
                String fileName = fileRegisters[fileRegister].fileName;
                int currentVersion = -1;
                if (fileDetails.ContainsKey(fileName)) {
                    currentVersion = ((Indices)fileDetails[fileName]).getLatestVersionSeen();
                }
                else {
                    Console.WriteLine("Version last seen,mcorresponding to fileRegister " + fileRegister + " Was not found in fileDetails HashTable. So returning witout Reading");
                    return;
                }
                int maxVersion = currentVersion; int maxId = 0; int temp;
                int k = 0; int cnt = 0; //int responseCount = 0;
                ArrayList retryServers = new ArrayList();
                for (k = 0; k < md.nbDataServers; k++) {
                    try {
                        temp = drs[k].GetVersion(sDTO[k].localFileName);
                        if (currentVersion < temp) {
                            if (maxVersion < temp) {
                                maxVersion = temp;
                                maxId = k;
                                cnt++;
                            }
                            else {
                                cnt++;
                            }


                        }
                        else if (currentVersion == temp && maxVersion == currentVersion) {
                            maxId = k;
                            cnt++;

                        }
                        else if (currentVersion == temp && currentVersion < maxVersion) {
                            cnt++;

                        }

                        else {

                            retryServers.Add(k);

                        }

                    }
                    catch (Exception e) {
                        retryServers.Add(k);
                    }
                }

                if (cnt < md.readQuorum) {
                    int index = 0;
                    while (cnt < md.readQuorum) {
                        index = (int)retryServers[k];
                        try {
                            temp = drs[index].GetVersion(sDTO[index].localFileName);
                            if (currentVersion < temp) {
                                if (maxVersion < temp) {
                                    maxVersion = temp;
                                    maxId = index;
                                    cnt++;
                                }
                                else {
                                    cnt++;
                                }

                                retryServers.RemoveAt(k);
                            }
                            else if (currentVersion == temp && maxVersion == currentVersion) {
                                maxId = index;
                                cnt++;
                                retryServers.RemoveAt(k);
                            }
                            else if (currentVersion == temp && currentVersion < maxVersion) {
                                cnt++;
                                retryServers.RemoveAt(k);
                            }


                        }
                        catch (Exception e) {
                            //DataServer is still failed.
                        }
                        k = (k + 1) % retryServers.Count;
                    }

                }

                //Read and Store the max version
                if (fileDetails.ContainsKey(fileName)) {
                    ((Indices)fileDetails[fileName]).setLatestVersionSeen(maxVersion);
                }
                Data contents = new Data(drs[maxId].Read(sDTO[maxId].localFileName, Semantics.MONOTONIC));
                if (contentRegister == -1) {
                    dataRegisterForCopy = contents;
                }
                else {
                    addContentRegister(contents, contentRegister);
                }
            }
            print("Read Command execution complete for fileRegister=" + fileRegister + ",Semantics=" + semantics + ",ContentRegister= " + contentRegister);
        }


        public void Create(string process, string fileName, int nbDataServers, int readQuorum, int writeQuorum) { Create(fileName, nbDataServers, readQuorum, writeQuorum); }
        public void Create(string fileName, int nbDataServers, int readQuorum, int writeQuorum) {
            print("Create Command for fileName= " + fileName);
            int i = MDSOpRandom.Next() % 3; int cnt = 0;
            int samround = 1;

            while (cnt < this.mrs.GetLength(0))
            {
                try
                {
                    // ask for a quorum to a random metadata server
                    MetadataQuorumDto serversQuorum = mrs[i].SendCreatePromise(nbDataServers, samround);

                    // synchronize state in all metadata (active replication intermediate state)
                    bool sam_state = true;
                    for (int sami = 0; sami < this.mrs.GetLength(0); sami++)
                    {
                        try
                        {
                            if (serversQuorum.AvailableMetadataServers[sami] && !mrs[sami].SendCreateAccepted(serversQuorum, nbDataServers))
                            {
                                print("Quorum wasn't accepted in round " + samround + " for fileName=" + fileName);
                                sam_state = false;
                                samround++;
                                break;
                            }
                        }
                        catch (Exception e)
                        {
                            print("Metadata server not available with number " + i + " when trying to register fileName=" + fileName+"eception is "+e);
                           
                        }
                    }

                    // do commit only to one random metadata server
                    if (sam_state)
                    {
                        Metadata m = new Metadata(mrs[i].CreateCommit(serversQuorum, fileName, nbDataServers, readQuorum, writeQuorum), fileName);
                        addFileRegister(m);
                        print("Created file with name=" + fileName+".and "+m.nbDataServers);
                        break;
                    }
                }
                catch (Exception e)
                {
                    print("Metadata server not available with number " + i + " when trying to register fileName=" + fileName);
                    cnt++;
                    i = (i + 1) % 3;
                    continue;
                }
                cnt++;
                i = (i + 1) % 3;
            }

            print("Print Command Execution Complete for fileName=" + fileName);
        }

        public void Open(string p, string fileName) { Open(fileName); }
        public void Open(string fileName) {
            print("Open Command for fileName= " + fileName);
            int i = MDSOpRandom.Next() % 3; int cnt = 0;
            while (cnt < this.mrs.GetLength(0)) {
                try {

                    Metadata m = new Metadata(mrs[i].Open(fileName), fileName);
                    addFileRegister(m); 
                    print("Open Command Success "+m.nbDataServers);
                    return;
                }
                catch (Exception e) {
                    //Receives exception when the MetaData Server is Offline.
                }
                i = (i + 1) % 3;
                cnt++;
            }
            print("Open Command Execution Complete for fileName=" + fileName);
        }

        public void Close(string process, string fileName) { Close(fileName); }
        public void Close(string fileName) {
            print("Close Command for fileName= " + fileName);
            if (fileDetails.ContainsKey(fileName)) {
                Indices inde = (Indices)fileDetails[fileName];
                if (inde.getFileRegisterIndex() != -1) {
                    if (fileRegisters[inde.getFileRegisterIndex()].fileName.Equals(fileName)) {
                        fileRegisters[inde.getFileRegisterIndex()] = null;
                    }
                    else {
                        //Do not remove this fileRegister as it has been replaced by som other fileregister.
                    }
                }

                fileDetails.Remove(fileName);
            }


            int i = MDSOpRandom.Next() % 3; int cnt = 0;
            while (cnt < this.mrs.GetLength(0)) {
                try {
                    mrs[i].Close(fileName);
                    return;
                }
                catch (Exception e) {

                }
                cnt++;
                i = (i + 1) % 3;
            }
            print("Close Command Execution Complete for fileName=" + fileName);
        }

        void addFileRegister(Metadata reg) {
            if (!fileDetails.ContainsKey(reg.fileName))
            {
                fileDetails.Add(reg.fileName, new Indices(currentFileRegister, -1));
                Console.WriteLine("Adding fileREgitsrer in " + currentFileRegister + " and the contents are " + reg.fileName);
                fileRegisters[currentFileRegister] = reg;
                currentFileRegister = (currentFileRegister + 1) % 10;
            }
            else
            {
                //we already have this fileRegister. Lets update it with this new Register.
                Indices ind = (Indices)fileDetails[reg.fileName];
                Console.WriteLine("Updating the  fileRegister at index=" + ind.getFileRegisterIndex());
                fileRegisters[ind.getFileRegisterIndex()] = reg;
            }
           
        }


        void addContentRegister(Data reg, int regNum) {
            dataRegisters[regNum] = reg;

        }
    }

    class Indices {
        private int fileRegisterIndex;
        // private int dataRegisterIndex;
        private int latestVersionSeen;
        public Indices(int fileRegisterIndex, int latestVersionSeen) {
            this.fileRegisterIndex = fileRegisterIndex;
            //this.dataRegisterIndex = dataRegisterIndex;
            this.latestVersionSeen = latestVersionSeen;
        }

        public int getFileRegisterIndex() {
            return fileRegisterIndex;
        }
        /* public int getDataRegisterIndex()
         {
             return dataRegisterIndex;
         }*/

        public void setFileRegisterIndex(int index) {
            this.fileRegisterIndex = index;
        }
        /*  public void setDataRegisterIndex(int index)
          {
              this.dataRegisterIndex=index;
          }*/
        public int getLatestVersionSeen() {
            return latestVersionSeen;
        }
        public void setLatestVersionSeen(int version) {
            this.latestVersionSeen = version;
        }

    }
}


