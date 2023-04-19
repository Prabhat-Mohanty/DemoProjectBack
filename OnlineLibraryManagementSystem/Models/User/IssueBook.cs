using Org.BouncyCastle.Asn1.Crmf;
using System.ComponentModel.DataAnnotations;

namespace OnlineLibraryManagementSystem.Models.User
{
    public class IssueBook
    {
        public int Id { get; set; }
        
        public int BookId { get; set; }
        
        public string userEmail { get; set; } = string.Empty;
        
        public int days { get; set; }
        
        [DataType(DataType.Date)]
        public DateTime issued_Date { get; set; } = DateTime.Now;

        [DataType(DataType.Date)]
        public DateTime due_Date { get; set; } = DateTime.Now;
        
        public string status { get; set; } = "Pending";
    }
}