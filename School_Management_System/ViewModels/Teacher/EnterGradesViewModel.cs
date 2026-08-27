using System.ComponentModel.DataAnnotations;

namespace School_Management_System.ViewModels.Teacher
{
    public class EnterGradesViewModel
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; }
        public List<StudentGradeViewModel> Students { get; set; }
    }

    public class StudentGradeViewModel
    {
        public int EnrollmentId { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; }
        public string AdmissionNumber { get; set; }

        [Range(0, 100, ErrorMessage = "Marks must be between 0 and 100.")]
        public double? Marks { get; set; }

        public string LetterGrade { get; set; }
        public int? GradeId { get; set; }
        public string AssessmentName { get; set; } = "Term Average";
    }
}