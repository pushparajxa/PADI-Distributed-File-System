/* Code file for data types and enums  common to all services */
using System;
using System.Collections.Generic;

namespace Shared.Services {
    [Serializable]
    public class ServerDTO {
            public readonly string address;
            public readonly string localFileName;

            public ServerDTO(string address, string localFileName) {
                this.address = address;
                this.localFileName = localFileName;
            }
        }

    [Serializable]
    public class MetadataDTO {
        public readonly int nbDataServers;
        public readonly int readQuorum;
        public readonly int writeQuorum;
        public readonly ServerDTO[] dataServers;
        public readonly int ticket;

        public MetadataDTO(int nbDataServers, int readQuorum, int writeQuorum, Dictionary<string, string> dataServers, int ticket) {
            String[] servers = new string[dataServers.Count];
            this.nbDataServers = nbDataServers;
            this.readQuorum = readQuorum;
            this.writeQuorum = writeQuorum;
            this.dataServers = new ServerDTO[dataServers.Count];
            this.ticket = ticket;
            dataServers.Keys.CopyTo(servers, 0);
            for(int i = 0; i < dataServers.Count; i++) {
                this.dataServers[i] = new ServerDTO(servers[i], dataServers[servers[i]]);
            }
        }
    }

    [Serializable]
    public class DataDTO {
        public readonly int version;
        public readonly byte[] content;

        public DataDTO(int version, byte[] content) {
            this.version = version;
            this.content = content;
        }
    }

    [Serializable]
    public class DataServerInfo : IComparable<DataServerInfo>
    {
        public int numFiles;
        public DateTime lastHeartbeat;
        public string address;

        public DataServerInfo(string address)
        {
            this.address = address;
            this.numFiles = 0;
            this.lastHeartbeat = DateTime.Now;
        }

        public int CompareTo(DataServerInfo other)
        {
            return (other.address == this.address ? 0 : (other.numFiles == this.numFiles ? int.Parse(this.address) - int.Parse(other.address) : this.numFiles - other.numFiles));
        }
    }

    [Serializable]
    public class MetadataServerInfo
    {
        public int port;

        public MetadataServerInfo(string address)
        {
            this.port = int.Parse(address);
        }

        public int getID(int basePort)
        {
            return port - basePort;
        }
    }


    [Serializable]
    public class MetadataContent
    {
        public MetadataDTO metadataDTO;
        public int timestamp;
        public int access;
        public int realWriteQuorum;
        public int realReadQuorum;

        public MetadataContent() : this(null, 0, 0) { }

        public MetadataContent(MetadataDTO metadataDTO, int access, int timestamp)
        {
            this.metadataDTO = metadataDTO;
            this.access = access;
            this.timestamp = timestamp;
            this.realReadQuorum = metadataDTO.readQuorum;
            this.realWriteQuorum = metadataDTO.writeQuorum;
        }

        public void update(MetadataDTO metadataDTO, int access, int timestamp)
        {
            this.metadataDTO = metadataDTO;
            this.access = access;
            this.timestamp = timestamp;
        }

    }

    [Serializable]
    public class MetadataQuorumDto
    {
        private bool[] availableMetadataServers;
        private DataServerInfo[] dataservers;
        private int round;
        private int electedServer; //port number of elected server
        public int[] timestamps; //metadata servers timestamps

        public MetadataQuorumDto() {
            timestamps = new int[3];
            for(int i=0; i<3; i++) {
                timestamps[i] = 0;
            }
        }

        public bool[] AvailableMetadataServers {
            get { return availableMetadataServers; }
            set { availableMetadataServers = value; }
        }

        public DataServerInfo[] Dataservers {
            get { return dataservers; }
            set { dataservers = value; }
        }

        public int Round
        {
            get { return round; }
            set { round = value; }
        }

        public int ElectedMetadataServer
        {
            get { return electedServer; }
            set { electedServer = value; }
        }

        public bool runCreate(int metadataPort)
        {
            return metadataPort == electedServer;
        }
    }
}