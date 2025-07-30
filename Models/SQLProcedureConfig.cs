using System.Collections.Generic;

namespace CMMT.Models
{
    public class SqlProcedureConfig
    {
        public string MasterProcedure { get; set; }
        public Dictionary<string, List<string>> Procedures { get; set; }
    }
}
