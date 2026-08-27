using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using School_Management_System.Models;

namespace School_Management_System.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Ensure database is created
            await context.Database.EnsureCreatedAsync();

            // ============================================
            // 1. CREATE ROLES
            // ============================================
            string[] roleNames = { "Admin", "Teacher", "Student", "Parent" };

            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // ============================================
            // 2. CREATE ADMIN USER
            // ============================================
            var adminEmail = "admin@school.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                var user = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "System Administrator",
                    Role = "Admin",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    EmailConfirmed = true
                };

                var createResult = await userManager.CreateAsync(user, "Admin@123");

                if (createResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, "Admin");
                }
            }

            // ============================================
            // 3. CREATE TEACHERS (FIXED - Only properties that exist in Teacher model)
            // ============================================
            var teacherData = new List<(string Email, string FullName, string Department, string Qualification, string EmployeeId)>
            {
                ("john.smith@school.com", "John Smith", "Mathematics", "M.Sc.", "EMP-26-0005"),
                ("sarah.johnson@school.com", "Sarah Johnson", "English", "M.A.", "EMP-26-0006"),
                ("robert.wilson@school.com", "Robert Wilson", "Science", "Ph.D.", "EMP-26-0007"),
                ("emily.davis@school.com", "Emily Davis", "Social Studies", "M.A.", "EMP-26-0008"),
                ("michael.brown@school.com", "Michael Brown", "ICT / Computer Science", "M.Sc.", "EMP-26-0009"),
                ("lisa.anderson@school.com", "Lisa Anderson", "Arts", "B.A.", "EMP-26-0010"),
                ("david.miller@school.com", "David Miller", "Physical Education", "B.Sc.", "EMP-26-0011"),
                ("jennifer.taylor@school.com", "Jennifer Taylor", "Administration", "MBA", "EMP-26-0012")
            };

            foreach (var teacherDataItem in teacherData)
            {
                var existingUser = await userManager.FindByEmailAsync(teacherDataItem.Email);
                if (existingUser == null)
                {
                    var user = new ApplicationUser
                    {
                        UserName = teacherDataItem.Email,
                        Email = teacherDataItem.Email,
                        FullName = teacherDataItem.FullName,
                        Role = "Teacher",
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true,
                        EmailConfirmed = true
                    };

                    var createResult = await userManager.CreateAsync(user, "Teacher@123");
                    if (createResult.Succeeded)
                    {
                        await userManager.AddToRoleAsync(user, "Teacher");

                        var teacher = new Teacher
                        {
                            UserId = user.Id,
                            // FullName is in ApplicationUser - NOT in Teacher
                            // PhoneNumber is NOT in Teacher model
                            EmployeeId = teacherDataItem.EmployeeId,
                            Department = teacherDataItem.Department,
                            Qualification = teacherDataItem.Qualification,
                            HireDate = DateTime.UtcNow,
                            IsActive = true
                        };

                        await context.Teachers.AddAsync(teacher);
                    }
                }
            }
            await context.SaveChangesAsync();

            // ============================================
            // 4. CREATE STUDENTS
            // ============================================
            var studentData = new List<(string Email, string FullName, string AdmissionNumber, string Class, string Section, DateTime DoB)>
            {
                ("james.wilson@student.school.com", "James Wilson", "STU-26-0005", "10", "A", new DateTime(2008, 5, 15)),
                ("emma.martinez@student.school.com", "Emma Martinez", "STU-26-0006", "10", "A", new DateTime(2008, 8, 22)),
                ("liam.jones@student.school.com", "Liam Jones", "STU-26-0007", "10", "B", new DateTime(2008, 3, 10)),
                ("olivia.brown@student.school.com", "Olivia Brown", "STU-26-0008", "10", "B", new DateTime(2008, 11, 1)),
                ("noah.garcia@student.school.com", "Noah Garcia", "STU-26-0009", "11", "A", new DateTime(2007, 6, 18)),
                ("ava.rodriguez@student.school.com", "Ava Rodriguez", "STU-26-0010", "11", "A", new DateTime(2007, 9, 25)),
                ("ethan.miller@student.school.com", "Ethan Miller", "STU-26-0011", "11", "B", new DateTime(2007, 2, 14)),
                ("sophia.davis@student.school.com", "Sophia Davis", "STU-26-0012", "11", "B", new DateTime(2007, 12, 5)),
                ("mason.thomas@student.school.com", "Mason Thomas", "STU-26-0013", "12", "A", new DateTime(2006, 4, 20)),
                ("mia.jackson@student.school.com", "Mia Jackson", "STU-26-0014", "12", "A", new DateTime(2006, 7, 8)),
                ("logan.white@student.school.com", "Logan White", "STU-26-0015", "12", "B", new DateTime(2006, 10, 30)),
                ("charlotte.lee@student.school.com", "Charlotte Lee", "STU-26-0016", "12", "B", new DateTime(2006, 1, 12)),
                ("elijah.harris@student.school.com", "Elijah Harris", "STU-26-0017", "9", "A", new DateTime(2009, 3, 15)),
                ("amelia.clark@student.school.com", "Amelia Clark", "STU-26-0018", "9", "A", new DateTime(2009, 8, 20)),
                ("benjamin.robinson@student.school.com", "Benjamin Robinson", "STU-26-0019", "9", "B", new DateTime(2009, 5, 10))
            };

            foreach (var studentDataItem in studentData)
            {
                var existingUser = await userManager.FindByEmailAsync(studentDataItem.Email);
                if (existingUser == null)
                {
                    var user = new ApplicationUser
                    {
                        UserName = studentDataItem.Email,
                        Email = studentDataItem.Email,
                        FullName = studentDataItem.FullName,
                        Role = "Student",
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true,
                        EmailConfirmed = true
                    };

                    var createResult = await userManager.CreateAsync(user, "Student@123");
                    if (createResult.Succeeded)
                    {
                        await userManager.AddToRoleAsync(user, "Student");

                        var student = new Student
                        {
                            UserId = user.Id,
                            AdmissionNumber = studentDataItem.AdmissionNumber,
                            Class = studentDataItem.Class,
                            Section = studentDataItem.Section,
                            DateOfBirth = studentDataItem.DoB,
                            IsActive = true
                        };

                        await context.Students.AddAsync(student);
                    }
                }
            }
            await context.SaveChangesAsync();

            // ============================================
            // 5. CREATE PARENTS
            // ============================================
            var parentData = new List<(string Email, string FullName, string Phone, string Address, string Occupation)>
            {
                ("michael.wilson@family.com", "Michael Wilson", "+12345678911", "123 Main St, City", "Engineer"),
                ("laura.martinez@family.com", "Laura Martinez", "+12345678912", "456 Oak Ave, City", "Teacher"),
                ("robert.brown@family.com", "Robert Brown", "+12345678913", "789 Pine Rd, City", "Doctor"),
                ("susan.garcia@family.com", "Susan Garcia", "+12345678914", "321 Elm St, City", "Lawyer"),
                ("david.rodriguez@family.com", "David Rodriguez", "+12345678915", "654 Maple Dr, City", "Business Owner")
            };

            foreach (var parentDataItem in parentData)
            {
                var existingUser = await userManager.FindByEmailAsync(parentDataItem.Email);
                if (existingUser == null)
                {
                    var user = new ApplicationUser
                    {
                        UserName = parentDataItem.Email,
                        Email = parentDataItem.Email,
                        FullName = parentDataItem.FullName,
                        Role = "Parent",
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true,
                        EmailConfirmed = true
                    };

                    var createResult = await userManager.CreateAsync(user, "Parent@123");
                    if (createResult.Succeeded)
                    {
                        await userManager.AddToRoleAsync(user, "Parent");

                        var parent = new Parent
                        {
                            UserId = user.Id,
                            PhoneNumber = parentDataItem.Phone,
                            Address = parentDataItem.Address,
                            Occupation = parentDataItem.Occupation,
                            CreatedAt = DateTime.UtcNow,
                            IsActive = true
                        };

                        await context.Parents.AddAsync(parent);
                    }
                }
            }
            await context.SaveChangesAsync();

            // ============================================
            // 6. LINK PARENTS TO STUDENTS
            // ============================================
            var parentLinks = new List<(string ParentEmail, string StudentAdmissionNumber, string Relationship)>
            {
                ("michael.wilson@family.com", "STU-26-0005", "Father"),
                ("laura.martinez@family.com", "STU-26-0006", "Mother"),
                ("robert.brown@family.com", "STU-26-0007", "Father"),
                ("susan.garcia@family.com", "STU-26-0008", "Mother"),
                ("david.rodriguez@family.com", "STU-26-0009", "Father")
            };

            foreach (var link in parentLinks)
            {
                var parent = await context.Parents
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.User.Email == link.ParentEmail);
                var student = await context.Students
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.AdmissionNumber == link.StudentAdmissionNumber);

                if (parent != null && student != null)
                {
                    var existingLink = await context.StudentParents
                        .FirstOrDefaultAsync(sp => sp.StudentId == student.Id && sp.ParentId == parent.Id);

                    if (existingLink == null)
                    {
                        var studentParent = new StudentParent
                        {
                            StudentId = student.Id,
                            ParentId = parent.Id,
                            Relationship = link.Relationship,
                            IsPrimaryContact = true,
                            IsActive = true
                        };
                        await context.StudentParents.AddAsync(studentParent);
                    }
                }
            }
            await context.SaveChangesAsync();

            // ============================================
            // 7. UPDATE CLASSES WITH TEACHER IDs
            // ============================================
            var mathTeacher = await context.Teachers.FirstOrDefaultAsync(t => t.Department == "Mathematics");
            var englishTeacher = await context.Teachers.FirstOrDefaultAsync(t => t.Department == "English");
            var scienceTeacher = await context.Teachers.FirstOrDefaultAsync(t => t.Department == "Science");
            var historyTeacher = await context.Teachers.FirstOrDefaultAsync(t => t.Department == "Social Studies");
            var ictTeacher = await context.Teachers.FirstOrDefaultAsync(t => t.Department == "ICT / Computer Science");
            var artsTeacher = await context.Teachers.FirstOrDefaultAsync(t => t.Department == "Arts");
            var peTeacher = await context.Teachers.FirstOrDefaultAsync(t => t.Department == "Physical Education");
            var adminTeacher = await context.Teachers.FirstOrDefaultAsync(t => t.Department == "Administration");

            var classes = await context.Classes.ToListAsync();
            if (classes.Any())
            {
                foreach (var classEntity in classes)
                {
                    if (classEntity.ClassName.Contains("Mathematics") && mathTeacher != null)
                        classEntity.TeacherId = mathTeacher.Id;
                    else if (classEntity.ClassName.Contains("English") && englishTeacher != null)
                        classEntity.TeacherId = englishTeacher.Id;
                    else if ((classEntity.ClassName.Contains("Science") || classEntity.ClassName.Contains("Biology")) && scienceTeacher != null)
                        classEntity.TeacherId = scienceTeacher.Id;
                    else if (classEntity.ClassName.Contains("History") && historyTeacher != null)
                        classEntity.TeacherId = historyTeacher.Id;
                    else if (classEntity.ClassName.Contains("Computer Science") && ictTeacher != null)
                        classEntity.TeacherId = ictTeacher.Id;
                    else if (classEntity.ClassName.Contains("Art") && artsTeacher != null)
                        classEntity.TeacherId = artsTeacher.Id;
                    else if (classEntity.ClassName.Contains("Physical Education") && peTeacher != null)
                        classEntity.TeacherId = peTeacher.Id;
                    else if (classEntity.ClassName.Contains("Business") && adminTeacher != null)
                        classEntity.TeacherId = adminTeacher.Id;
                }
                await context.SaveChangesAsync();
            }

            // ============================================
            // 8. CREATE ENROLLMENTS
            // ============================================
            if (!await context.Enrollments.AnyAsync())
            {
                var students = await context.Students.ToListAsync();
                var classList = await context.Classes.ToListAsync();

                foreach (var student in students)
                {
                    var classIds = new List<int>();

                    if (student.Class == "9")
                    {
                        classIds.AddRange(new[] { 9, 11 });
                    }
                    else if (student.Class == "10")
                    {
                        classIds.AddRange(new[] { 1, 2, 5, 9, 11 });
                    }
                    else if (student.Class == "11")
                    {
                        classIds.AddRange(new[] { 3, 4, 6, 10, 12 });
                    }
                    else if (student.Class == "12")
                    {
                        classIds.AddRange(new[] { 7, 8, 13 });
                    }

                    foreach (var classId in classIds)
                    {
                        var classEntity = classList.FirstOrDefault(c => c.ClassId == classId);
                        if (classEntity != null)
                        {
                            var enrollment = new Enrollment
                            {
                                StudentId = student.Id,
                                ClassId = classEntity.ClassId,
                                EnrollmentDate = DateTime.UtcNow,
                                IsActive = true
                            };
                            await context.Enrollments.AddAsync(enrollment);
                        }
                    }
                }
                await context.SaveChangesAsync();
            }

            // ============================================
            // 9. CREATE ATTENDANCE RECORDS
            // ============================================
            if (!await context.Attendances.AnyAsync())
            {
                var enrollments = await context.Enrollments.ToListAsync();
                var random = new Random();
                var statuses = new[] {
                    Attendance.AttendanceStatus.Present,
                    Attendance.AttendanceStatus.Present,
                    Attendance.AttendanceStatus.Present,
                    Attendance.AttendanceStatus.Absent,
                    Attendance.AttendanceStatus.Late,
                    Attendance.AttendanceStatus.Excused
                };

                for (int day = 1; day <= 20; day++)
                {
                    var date = DateTime.UtcNow.AddDays(-day);
                    foreach (var enrollment in enrollments.Take(50))
                    {
                        var status = statuses[random.Next(statuses.Length)];
                        var attendance = new Attendance
                        {
                            EnrollmentId = enrollment.EnrollmentId,
                            AttendanceDate = date,
                            Status = status,
                            IsActive = true
                        };
                        await context.Attendances.AddAsync(attendance);
                    }
                }
                await context.SaveChangesAsync();
            }

            // ============================================
            // 10. CREATE GRADES
            // ============================================
            if (!await context.Grades.AnyAsync())
            {
                var enrollments = await context.Enrollments.ToListAsync();
                var random = new Random();
                var assessments = new[] { "Term Average", "Midterm", "Final Exam", "Project", "Quiz 1", "Quiz 2" };

                foreach (var enrollment in enrollments.Take(30))
                {
                    var numGrades = random.Next(2, 4);
                    for (int i = 0; i < numGrades; i++)
                    {
                        var marks = random.Next(40, 100);
                        var letterGrade = CalculateLetterGrade(marks);
                        var assessment = assessments[random.Next(assessments.Length)];

                        var grade = new Grade
                        {
                            EnrollmentId = enrollment.EnrollmentId,
                            AssessmentName = assessment,
                            Marks = marks,
                            LetterGrade = letterGrade,
                            DateRecorded = DateTime.UtcNow,
                            IsActive = true
                        };
                        await context.Grades.AddAsync(grade);
                    }
                }
                await context.SaveChangesAsync();
            }

            // ============================================
            // 11. CREATE AUDIT LOGS
            // ============================================
            if (!await context.AuditLogs.AnyAsync())
            {
                var adminUserObj = await userManager.FindByEmailAsync("admin@school.com");
                var logs = new List<AuditLog>
                {
                    new AuditLog {
                        Action = "Login",
                        UserId = adminUserObj?.Id,
                        FullName = "System Administrator",
                        Details = "User logged in",
                        ActionDate = DateTime.UtcNow.AddDays(-1)
                    },
                    new AuditLog {
                        Action = "Create Student",
                        UserId = adminUserObj?.Id,
                        FullName = "System Administrator",
                        Details = "Created new student: James Wilson",
                        ActionDate = DateTime.UtcNow.AddDays(-2)
                    },
                    new AuditLog {
                        Action = "Create Teacher",
                        UserId = adminUserObj?.Id,
                        FullName = "System Administrator",
                        Details = "Created new teacher: John Smith",
                        ActionDate = DateTime.UtcNow.AddDays(-3)
                    },
                    new AuditLog {
                        Action = "Enroll Student",
                        UserId = adminUserObj?.Id,
                        FullName = "System Administrator",
                        Details = "Enrolled James Wilson in Grade 10A - Mathematics",
                        ActionDate = DateTime.UtcNow.AddDays(-4)
                    },
                    new AuditLog {
                        Action = "Mark Attendance",
                        UserId = adminUserObj?.Id,
                        FullName = "System Administrator",
                        Details = "Marked attendance for Grade 10A - Mathematics",
                        ActionDate = DateTime.UtcNow.AddDays(-5)
                    },
                    new AuditLog {
                        Action = "Enter Grades",
                        UserId = adminUserObj?.Id,
                        FullName = "System Administrator",
                        Details = "Entered grades for Grade 10A - Mathematics",
                        ActionDate = DateTime.UtcNow.AddDays(-6)
                    }
                };

                await context.AuditLogs.AddRangeAsync(logs);
                await context.SaveChangesAsync();
            }
        }

        private static string CalculateLetterGrade(double marks)
        {
            if (marks >= 90) return "A";
            if (marks >= 80) return "B";
            if (marks >= 70) return "C";
            if (marks >= 60) return "D";
            return "F";
        }
    }
}