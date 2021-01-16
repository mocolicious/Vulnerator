using System.Collections.ObjectModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vulnerator.Model.Entity
{
    public class Location : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage",
            "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Location()
        {
            Hardwares = new ObservableCollection<Hardware>();
            StepOneQuestionnaires = new ObservableCollection<StepOneQuestionnaire>();
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long Location_ID { get; set; }

        [Required]
        [StringLength(200)]
        public string LocationName { get; set; }

        [Required]
        [StringLength(200)]
        public string StreetAddressOne { get; set; }

        [Required]
        [StringLength(200)]
        public string StreeAddressTwo { get; set; }

        [StringLength(25)]
        public string BuildingNumber { get; set; }

        public long? FloorNumber { get; set; }

        public long? RoomNumber { get; set; }

        [StringLength(50)]
        public string City { get; set; }

        [StringLength(25)]
        public string State { get; set; }

        [Required]
        [StringLength(100)]
        public string Country { get; set; }

        public long? ZipCode { get; set; }

        [StringLength(200)]
        public string APO_FPO { get; set; }

        public DateTime? OSS_AccreditationDate { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Hardware> Hardwares { get; set; }
        
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<StepOneQuestionnaire> StepOneQuestionnaires { get; set; }
    }
}
