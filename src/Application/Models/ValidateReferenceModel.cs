using System;

namespace Application.Models
{
    public class ValidateReferenceModel
    {
        public string Type { get; set; }

        public DateTime CreatedAt { get; set; }

        public DataModel Data { get; set; }

        public class DataModel
        {
            public string Reference { get; set; }
            public string Type { get; set; }

        }
    }
}
