using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using Microsoft.AspNetCore.Http;
using MongoDB.Bson;
using MongoDB.Driver.GridFS;
using StoreDocApi.Models;

namespace StoreDocApi.Interfaces
{
    public interface IFileRepository
    {
        Task<IEnumerable<FileBlock>> GetAllFiles();
        Task<FileBlock> GetFile(string id);
        Task AddFile(FileBlock item);
        Task<DeleteResult> RemoveFile(string id);

        Task<UpdateResult> UpdateFile(string id, string body);

        // demo interface - full document update
        Task<ReplaceOneResult> UpdateFileDocument(string id, string body);

        // should be used with high cautious, only in relation with demo setup
        Task<DeleteResult> RemoveAllFiles();

        Task<ObjectId> UploadFile(List<IFormFile> files);
        Task<SaveResult> UploadFile(IFormFileCollection files, string signatureId);
        Task<String> GetFileInfo(string id);
    }
}
