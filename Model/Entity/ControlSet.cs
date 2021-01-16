using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vulnerator.Model.Entity
{
    public class ControlSet : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public ControlSet()
        { NIST_Controls = new ObservableCollection<NIST_Control>(); }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long ControlSet_ID { get; set; }
        
        [Required]
        [StringLength(50)]
        public string ControlSetName { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<NIST_Control> NIST_Controls { get; set; }
    }
}
