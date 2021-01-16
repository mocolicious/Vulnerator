using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vulnerator.Model.Entity
{
    public class IATA_Standard : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public IATA_Standard()
        {
            Groups = new ObservableCollection<Group>();
            NIST_Controls = new ObservableCollection<NIST_Control>();
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long IATA_Standard_ID { get; set; }

        [Required]
        [StringLength(50)]
        public string IATA_StandardTitle { get; set; }

        [Required]
        [StringLength(1000)]
        public string IATA_StandardDescription { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Group> Groups { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<NIST_Control> NIST_Controls { get; set; }
    }
}
