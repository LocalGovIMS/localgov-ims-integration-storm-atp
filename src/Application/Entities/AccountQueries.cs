using System;
using System.ComponentModel.DataAnnotations;


namespace Application.Entities
{
    public class AccountQueries : BaseEntity
    {
        [StringLength(36)]
        public string Reference { get; set; }

        [StringLength(100)]
        public string Status { get; set; }

        public DateTime? CapturedDate { get; set; }

    }
}
