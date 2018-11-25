using StoreDocApi.DbContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

using MongoDB.Bson;
using Microsoft.AspNetCore.Http;
using System.IO;
using Microsoft.Net.Http.Headers;
using MongoDB.Driver.GridFS;
using StoreDocApi.Models;
using StoreDocApi.Interfaces;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace StoreDocApi.Repository
{
    public class FileRepository : IFileRepository
    {
        private readonly FileContext _context = null;

        public FileRepository(IOptions<Settings> settings)
        {
            _context = new FileContext(settings);
        }

        public async Task<IEnumerable<FileBlock>> GetAllFiles()
        {
            try
            {
                return await _context.FileBlocks.Find(_ => true).ToListAsync();
            }
            catch (Exception ex)
            {
                // log or manage the exception
                throw ex;
            }
        }

        public async Task<FileBlock> GetFile(string id)
        {
            var filter = Builders<FileBlock>.Filter.Eq("Id", id);

            try
            {
                return await _context.FileBlocks
                                .Find(filter)
                                .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                // log or manage the exception
                throw ex;
            }
        }

        public async Task AddFile(FileBlock item)
        {
            try
            {
                await _context.FileBlocks.InsertOneAsync(item);

            }
            catch (Exception ex)
            {
                // log or manage the exception
                throw ex;
            }
        }

        public async Task<DeleteResult> RemoveFile(string id)
        {
            try
            {
                return await _context.FileBlocks.DeleteOneAsync(
                     Builders<FileBlock>.Filter.Eq("Id", id));
            }
            catch (Exception ex)
            {
                // log or manage the exception
                throw ex;
            }
        }

        public async Task<UpdateResult> UpdateFile(string id, string body)
        {
            var filter = Builders<FileBlock>.Filter.Eq(s => s.Id, id);
            var update = Builders<FileBlock>.Update
                            .Set(s => s.FileName, body)
                            .CurrentDate(s => s.Date);

            try
            {
                return await _context.FileBlocks.UpdateOneAsync(filter, update);
            }
            catch (Exception ex)
            {
                // log or manage the exception
                throw ex;
            }
        }

        public async Task<ReplaceOneResult> UpdateFile(string id, FileBlock item)
        {
            try
            {
                return await _context.FileBlocks
                            .ReplaceOneAsync(n => n.Id.Equals(id)
                                            , item
                                            , new UpdateOptions { IsUpsert = true });
            }
            catch (Exception ex)
            {
                // log or manage the exception
                throw ex;
            }
        }

        // Demo function - full document update
        public async Task<ReplaceOneResult> UpdateFileDocument(string id, string body)
        {
            var item = await GetFile(id) ?? new FileBlock();
            //item.Body = body;
            //item.UpdatedOn = DateTime.Now;

            return await UpdateFile(id, item);
        }

        public async Task<DeleteResult> RemoveAllFiles()
        {
            try
            {
                return await _context.FileBlocks.DeleteManyAsync(new BsonDocument());
            }
            catch (Exception ex)
            {
                // log or manage the exception
                throw ex;
            }
        }

        public async Task<ObjectId> UploadFile(List<IFormFile> files)
        {
            try
            {
                var file = files.FirstOrDefault();
                var stream = file.OpenReadStream();
                var filename = file.FileName;
                int signatureId = 10;

                var url = $"https://test.ukey.net.ua:3020/api/v1/signatures/file/{signatureId}?mode=full";
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", "w5n77v3GRLGq6RJMybpVeaPv6V7sogG3");
                    using (HttpResponseMessage res = await client.GetAsync(url))
                    using (HttpContent content = res.Content)
                    {
                        string data = await content.ReadAsStringAsync();
                        if (data != null)
                        {
                            Console.WriteLine(data);
                        }
                    }
                }

                return await _context.Bucket.UploadFromStreamAsync(filename, stream);
            }
            catch (Exception ex)
            {
                // log or manage the exception
                return new ObjectId(ex.ToString());
            }
        }

        public async Task<SaveResult> UploadFile(IFormFileCollection files, string signatureId)
        {
            var result = new SaveResult()
            {
                UserId = "",
                FileId = "",
                Message = "Ошибка!"
            };

            var file = files.FirstOrDefault();
            var fileStream = file.OpenReadStream();
            var fileName = file.FileName;
            var baseUrl = $"https://test.ukey.net.ua:3020";
            var url = $"{baseUrl}/api/v1/signatures/file/{signatureId}";
            var signatureRequest = "";
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "w5n77v3GRLGq6RJMybpVeaPv6V7sogG3");
                    using (HttpResponseMessage res = await client.GetAsync(url))
                    {
                        using (HttpContent content = res.Content)
                        {
                            signatureRequest = await content.ReadAsStringAsync();
                        }
                        if (signatureRequest != null)
                        {
                            (string userId, string signUrl) = GetSignData(JsonConvert.DeserializeObject<Sign>(signatureRequest));

                            using (HttpResponseMessage httpResult = await client.GetAsync(baseUrl + signUrl))
                            {
                                using (HttpContent p7sFile = httpResult.Content)
                                {
                                    var signatureStream = await p7sFile.ReadAsStreamAsync();
                                    var actionResult =  await StoreFiles(userId, fileStream, fileName, signatureStream, $"{fileName}.ps7");
                                    return actionResult;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
                return result;
            }

            return result;
        }

        private async Task<SaveResult> StoreFiles(string userId, Stream docStream, string docName, Stream signatureStream, string signatureName)
        {
            ObjectId fileId = await _context.Bucket.UploadFromStreamAsync(docName, docStream);
            ObjectId signatureId = await _context.Bucket.UploadFromStreamAsync(signatureName, signatureStream);

            var block = new FileBlock()
            {
                FileName = docName,
                SignatureName = signatureName,
                Date = DateTime.Now,
                UserId = userId,
                FileId = fileId.ToString(),
                SignatureId = signatureId.ToString(),
                Id = fileId.ToString(),
                Hash = docName + userId
            };
            //block.Hash = CreateHash(docName + userId);
            var lastOneBlock = GetLastOneBlock();
            if (lastOneBlock != null)
            {
                block.PrevBlockHash = lastOneBlock.Hash;
            }

            await _context.FileBlocks.InsertOneAsync(block);
            var result = new SaveResult()
            {
                UserId = userId,
                FileId = fileId.ToString(),
                Message = "Файл успішно збережений!"
            };
            return result;
        }

        string CreateHash(string str)
        {
            byte[] salt = new byte[128 / 8];
            string hash = Convert.ToBase64String(KeyDerivation.Pbkdf2(
               password: str,
               salt: salt,
               prf: KeyDerivationPrf.HMACSHA1,
               iterationCount: 10000,
               numBytesRequested: 256 / 8));

            return hash;
        }

        private (string userId, string url) GetSignData(Sign sign)
        {
            string signUrl = sign.fileSignatures.signatures[0].url;
            string userId = sign.userId;
            return (userId, signUrl);
        }

        FileBlock GetLastOneBlock()
        {
            return _context.FileBlocks.AsQueryable().Where(l => l.Hash.Length > 0).AsQueryable().OrderByDescending(l => l.Date).FirstOrDefault();
        }

        public async Task<String> GetFileInfo(string id)
        {
            GridFSFileInfo info = null;
            var objectId = new ObjectId(id);
            try
            {
                using (var stream = await _context.Bucket.OpenDownloadStreamAsync(objectId))
                {
                    info = stream.FileInfo;
                }
                return info.Filename;
            }
            catch (Exception)
            {
                return "Not Found";
            }
        }
    }

    public class Sign
    {
        public Signature fileSignatures { get; set; }
        public string userId { get; set; }
    }

    public class Signature
    {
        public SignDetails[] signatures { get; set; }
    }

    public class SignDetails
    {
        public string url { get; set; }
    }
}
