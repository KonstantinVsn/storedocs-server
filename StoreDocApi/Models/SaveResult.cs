using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StoreDocApi.Models
{
    public class SaveResult
    {
        public string FileId { get; set; }
        public string UserId { get; set; }
        public string Message { get; set; }
    }
}
