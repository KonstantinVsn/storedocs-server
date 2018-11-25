using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace StoreDocApi.Models
{
    public class FileBlock
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public string Hash { get; set; } = string.Empty;
        public string PrevBlockHash { get; set; } = string.Empty;
        public DateTime Date { get; set; } = DateTime.Now;
        public string UserId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FileId { get; set; }
        public string SignatureName { get; set; } = string.Empty;
        public string SignatureId { get; set; }

    }
}
