using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vulnerator.Model.Entity
{
    public class ExitCriteria : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public ExitCriteria()
        { SecurityAssessmentProcedures = new ObservableCollection<SecurityAssessmentProcedure>(); }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long ExitCriteria_ID { get; set; }

        [Column("ExitCriteria")]
        [Required]
        [StringLength(100)]
        public string Exit_Criteria { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<SecurityAssessmentProcedure> SecurityAssessmentProcedures { get; set; }
    }
}
