using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using StoreDocApi.Interfaces;
using StoreDocApi.Models;

namespace StoreDocApi.Controllers
{
    [Produces("application/json")]
    [Consumes("application/json", "multipart/form-data")]
    [Route("api/[controller]")]
    public class FilesController : ControllerBase
    {
        private readonly IFileRepository _fileRepo;

        public FilesController(IFileRepository repo)
        {
            _fileRepo = repo;
        }

        [NoCache]
        [HttpGet]
        public Task<IEnumerable<FileBlock>> Get()
        {
            return GetFileInternal();
        }

        private async Task<IEnumerable<FileBlock>> GetFileInternal()
        {
            return await _fileRepo.GetAllFiles();
        }

        // GET api/Files/5
        [HttpGet("{id}")]
        public Task<FileBlock> Get(string id)
        {
            return GetFileByIdInternal(id);
        }

        private async Task<FileBlock> GetFileByIdInternal(string id)
        {
            return await _fileRepo.GetFile(id) ?? new FileBlock();
        }

        // POST api/Files
        [HttpPost]
        public void Post([FromBody]string value)
        {
           // _fileRepo.AddFile(new FileBlock() { Body = value, CreatedOn = DateTime.Now, UpdatedOn = DateTime.Now });
        }

        // PUT api/Files/5
        [HttpPut("{id}")]
        public void Put(string id, [FromBody]string value)
        {
            _fileRepo.UpdateFileDocument(id, value);
        }

        // DELETE api/Files/23243423
        [HttpDelete("{id}")]
        public void Delete(string id)
        {
            _fileRepo.RemoveFile(id);
        }

        // POST api/Files/uploadFile
        [HttpPost("uploadFile")]
        public async Task<ObjectId> UploadFile(List<IFormFile> files)
        {
            return await _fileRepo.UploadFile(files);
        }
        // GET api/Files/getFileInfo/d1we24ras41wr
        [HttpGet("getFileInfo/{id}")]
        public Task<String> GetFileInfo(string id)
        {
            return _fileRepo.GetFileInfo(id);
        }

        [HttpPost]
        [Route("upload")]
        public async Task PostOrUpdateAsync(string signatureId)
        {
            var files = HttpContext.Request.Form.Files;
            if (files.Count > 0)
            {
               await _fileRepo.UploadFile(files, signatureId);
            }
        }
    }
}
