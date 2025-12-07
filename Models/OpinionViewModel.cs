using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MonitoringConfigurator.Models
{
    public class OpinionViewModel
    {
        [Required, MaxLength(4000)]
        public string Message { get; set; } = string.Empty;

        public IEnumerable<Contact> ExistingOpinions { get; set; } = System.Linq.Enumerable.Empty<Contact>();
    }
}
