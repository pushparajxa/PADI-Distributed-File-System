using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shared.Services.Message
{
    public class MetadataMessageService : MarshalByRefObject
    {
        public event RegisterMessageHandler RegisterHandler;
        public event RegisterDataReplicaMessageHandler RegisterDataReplicaHandler;
        public event RegisterMetadataMessageHandler RegisterMetadataHandler;
        public event CreateNewFileMdsMessageHandler CreateNewFileHandler;
        public event DeleteFileMdsMessageHandler DeleteFileHandler;
        public event ReplicateCreateMdsMessageHandler ReplicateCreateHandler;
        public event ReplicateDeleteMdsMessageHandler ReplicateDeleteHandler;
        public event GetMetadataDtoMessageHandler GetMetadataDtoHandler;
        public event SendMetadataDtoMessageHandler SendMetadataDtoHandler;
        public event MetadataTransferMessageHandler MetadataTransferHandler;

        public void Register(string address)
        {
            if (RegisterHandler != null) RegisterHandler(address);
        }

        public void RegisterDataReplica(string address)
        {
            if (RegisterDataReplicaHandler != null) RegisterDataReplicaHandler(address);
        }


        public void RegisterMetadata(string address)
        {
            if (RegisterMetadataHandler != null) RegisterMetadataHandler(address);
        }

        /* Requests the MetadataServer to create a new file. MDS replies with metadata for created file.
         * Metadata: filename (string), nbdata servers (int), read quorum (int), write quorum (int), dataservers (list<dictionary<server,filename>>)
         */
        public MetadataDTO createNewFile()
        {
            if (CreateNewFileHandler != null)
            {
                return CreateNewFileHandler();
            }
            else throw new System.InvalidOperationException("CreateNewFileHandler instance in MetadataMessageService doesn't exists!");
        }

        /* GetMetadataDto: get metadata from criated file from elected metadata server 
         */
        public MetadataDTO GetMetadataDto(string fileName, int electedTimestamp)
        {
            if (GetMetadataDtoHandler != null)
            {
                return GetMetadataDtoHandler(fileName, electedTimestamp);
            }
            else throw new System.InvalidOperationException("GetMetadataDtoHandler instance in MetadataMessageService doesn't exists!");
        }

        /* SendMetadataDto: send metadata from criated file from elected metadata server 
         */
        public void SendMetadataDto(string fileName, MetadataDTO metadataDTO, MetadataQuorumDto dataservers)
        {
            if (SendMetadataDtoHandler != null)
            {
                SendMetadataDtoHandler(fileName, metadataDTO, dataservers);
            }
            else throw new System.InvalidOperationException("SendMetadataDtoHandler instance in MetadataMessageService doesn't exists!");
        }

        //Deletes file
        public void deleteFile(string fileName)
        {
            if (DeleteFileHandler != null) DeleteFileHandler(fileName);
            else throw new System.InvalidOperationException("DeleteFileHandler instance in MetadataMessageService doesn't exists!");
        }

        //Active replication: add metadata entrance
        public void replicateCreate(MetadataDTO metadata)
        {
            if (ReplicateCreateHandler != null)
            {
                ReplicateCreateHandler(metadata);
            }
            else throw new System.InvalidOperationException("ReplicateCreateHandler instance in MetadataMessageService doesn't exists!");
        }

        //Active replication: remove metadata entrance
        public void replicateDelete(MetadataDTO metadata)
        {
            if (ReplicateDeleteHandler != null)
            {
                ReplicateDeleteHandler(metadata);
            }
            else throw new System.InvalidOperationException("ReplicateDeleteHandler instance in MetadataMessageService doesn't exists!");
        }

        public Dictionary<string, MetadataContent> MetadataTransfer()
        {
            if (MetadataTransferHandler != null)
            {
                return MetadataTransferHandler();
            }
            else throw new System.InvalidOperationException("MetadataTransferHandler instance in MetadataMessageService doesn't exists!");
        }
    }

    public delegate void RegisterMessageHandler(string address);
    public delegate void RegisterDataReplicaMessageHandler(string address);
    public delegate void RegisterMetadataMessageHandler(string address);
    public delegate MetadataDTO CreateNewFileMdsMessageHandler();
    public delegate void DeleteFileMdsMessageHandler(string localFileName);
    public delegate void ReplicateCreateMdsMessageHandler(MetadataDTO metadata);
    public delegate void ReplicateDeleteMdsMessageHandler(MetadataDTO metadata);
    public delegate MetadataDTO GetMetadataDtoMessageHandler(string fileName, int electedTimestamp);
    public delegate void SendMetadataDtoMessageHandler(string fileName, MetadataDTO metadataDTO, MetadataQuorumDto dataservers);
    public delegate Dictionary<string, MetadataContent> MetadataTransferMessageHandler();
}
