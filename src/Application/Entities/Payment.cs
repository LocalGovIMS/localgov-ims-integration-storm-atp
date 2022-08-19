﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Application.Entities
{
    public class Payment : BaseEntity
    {
        public DateTime CreatedDate { get; set; }

        [StringLength(36)]
        public string Reference { get; set; }

        public decimal Amount { get; set; }

        [StringLength(255)]
        public string PaymentId { get; set; }

        [StringLength(100)]
        public string Status { get; set; }

        public bool Finished { get; set; }

        public DateTime? CapturedDate { get; set; }

        [StringLength(255)]
        public string RefundReference { get; set; }

        //
        // Summary:
        //     Gets or Sets CardPrefix
        public string CardPrefix { get; set; }
        //
        // Summary:
        //     Gets or Sets CardSuffix
        public string CardSuffix { get; set; }
    }
}
