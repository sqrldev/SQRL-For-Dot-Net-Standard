using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using SqrlForNet;

namespace WithDatabase.Database
{
    public class NutInfoData : NutInfo
    {

        [Key]
        public string Nut { get; set; }
        
        public bool Authorized { get; set; }

    }
}