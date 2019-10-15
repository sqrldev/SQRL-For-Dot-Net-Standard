using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WithDatabase.Database
{
    public class User
    {

        public int Id { get; set; }

        public string Username { get; set; }

        public SqrlUser SqrlUser { get; set; }

        public int SqrlUserId { get; set; }

        public string Role { get; set; }

    }
}
