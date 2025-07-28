using System.Collections.Generic;
using System;

namespace AspnetCoreMvcFull.Models
{
  public class GradeConfiguration
  {
    public int GradeConfigurationId { get; set; } // Primary Key

    // Foreign Key to JenisForm (also its primary key due to OneToOne relationship)
    public int FormTypeId { get; set; }
    public virtual JenisForm JenisForm { get; set; } // Navigation property back to JenisForm

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? LastModifiedAt { get; set; } // Nullable for initial creation

    // Collection of GradeRanges associated with this configuration
    public virtual ICollection<GradeRange> GradeRanges { get; set; } = new List<GradeRange>();
  }
}
