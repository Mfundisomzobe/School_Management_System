namespace School_Management_System.ViewModels.Teacher
{
    
    
        public class GradeHistoryViewModel
        {
            public int ClassId { get; set; }
            public string ClassName { get; set; }
            public List<GradeRecord> GradeRecords { get; set; }
            public List<string> Assessments { get; set; }
            public Dictionary<string, double> AssessmentAverages { get; set; }
            public int TotalStudents { get; set; }
            public double OverallClassAverage { get; set; }
            public string GradeDistribution { get; set; }
        }

        public class GradeRecord
        {
            public string StudentName { get; set; }
            public string AdmissionNumber { get; set; }
            public string AssessmentName { get; set; }
            public double? Marks { get; set; }
            public string LetterGrade { get; set; }
            public DateTime DateRecorded { get; set; }
            public string GradeColor { get; set; }
        }
    
}
