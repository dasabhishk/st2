using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMT.Helpers
{
    public static class AppConstants
    {
        // Mapping UI
        public const string NoMappingOptional = "-- No Mapping (Optional) --";

        // CSV Types
        public const string PatientStudy = "Patient Study";
        public const string SeriesInstance = "Series Instance";

        //Int Constants
        public const int MAX_SAMPLE_ROWS = 5;
        public const int MAX_PARALLEL = 2;
        public const int PreviewSampleCount = 3;

        //CSV Delimiter
        public const string CSV_DELIMITER = "|";
        // Procedure Names
        public const string StudyDataProcedure = "[dbo].[HIS_RegisterPACSStudyData]";
    }
}