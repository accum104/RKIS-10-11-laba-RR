using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StudentManagement.Models;
using StudentManagement.Data;
using Microsoft.EntityFrameworkCore;
using Avalonia.Platform.Storage;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System;

namespace StudentManagement.ViewModels;

public partial class StudentsViewModel : ViewModelBase
{
    private readonly ApplicationDbContext _context;
    private readonly IStorageProvider _storageProvider;
    
    public event EventHandler? StudentsChanged;
    
    [ObservableProperty]
    private ObservableCollection<Student> _students = new();
    
    [ObservableProperty]
    private string _newStudentName = "";
    
    [ObservableProperty]
    private ObservableCollection<Course> _availableCourses = new();

    [ObservableProperty]
    private ObservableCollection<Course> _selectedCoursesForNewStudent = new();

    [ObservableProperty]
    private Student? _selectedStudent;

    [ObservableProperty]
    private bool _isEditing;
    
    public StudentsViewModel(ApplicationDbContext context, IStorageProvider storageProvider)
    {
        _context = context;
        _storageProvider = storageProvider;
        LoadData();
    }

    public void LoadData()
    {
        Debug.WriteLine("Loading data...");
        
        // Загружаем студентов с их курсами
        var loadedStudents = _context.Students
            .Include(s => s.Courses)
            .AsNoTracking()
            .Select(s => new Student
            {
                Id = s.Id,
                Name = s.Name,
                PhotoPath = s.PhotoPath,
                Courses = new ObservableCollection<Course>(
                    s.Courses.Select(c => new Course
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Description = c.Description
                    })
                )
            })
            .ToList();

        Debug.WriteLine($"Loaded {loadedStudents.Count} students");
        
        Students.Clear();
        foreach (var student in loadedStudents)
        {
            Students.Add(student);
        }
        
        // Обновляем список доступных курсов
        RefreshAvailableCourses();
    }

    private void RefreshAvailableCourses()
    {
        // Загружаем актуальный список курсов из базы данных
        var courses = _context.Courses
            .AsNoTracking()
            .Select(c => new Course
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                IsSelected = false
            })
            .ToList();

        AvailableCourses.Clear();
        foreach (var course in courses)
        {
            AvailableCourses.Add(course);
        }

        // Если есть выбранный студент, обновляем состояние выбора курсов
        if (SelectedStudent != null)
        {
            var studentCourses = _context.Students
                .Include(s => s.Courses)
                .Where(s => s.Id == SelectedStudent.Id)
                .SelectMany(s => s.Courses)
                .Select(c => c.Id)
                .ToList();

            foreach (var course in AvailableCourses)
            {
                course.IsSelected = studentCourses.Contains(course.Id);
            }
        }
    }

    [RelayCommand]
    public void AddStudent()
    {
        if (string.IsNullOrWhiteSpace(NewStudentName))
            return;

        using var transaction = _context.Database.BeginTransaction();
        try
        {
            var student = new Student { Name = NewStudentName };
            _context.Students.Add(student);
            _context.SaveChanges();

            // Добавляем выбранные курсы
            var selectedCourses = AvailableCourses.Where(c => c.IsSelected).ToList();
            foreach (var course in selectedCourses)
            {
                var dbCourse = _context.Courses.Find(course.Id);
                if (dbCourse != null)
                {
                    student.Courses.Add(dbCourse);
                }
            }
            
            _context.SaveChanges();
            transaction.Commit();

            // Создаем нового студента для отображения
            var newStudent = new Student
            {
                Id = student.Id,
                Name = student.Name
            };

            // Добавляем курсы
            foreach (var course in selectedCourses)
            {
                newStudent.Courses.Add(new Course
                {
                    Id = course.Id,
                    Name = course.Name,
                    Description = course.Description
                });
            }

            Students.Add(newStudent);
            
            // Очищаем форму
            NewStudentName = "";
            foreach (var course in AvailableCourses)
            {
                course.IsSelected = false;
            }
            SelectedCoursesForNewStudent.Clear();

            // Уведомляем об изменении студентов
            NotifyStudentsChanged();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    [RelayCommand]
    public void DeleteStudent(Student student)
    {
        if (student == null) return;

        try
        {
            Debug.WriteLine($"DeleteStudent: Starting deletion of student {student.Id}");

            using var context = new ApplicationDbContext();
            var dbStudent = context.Students
                .Include(s => s.Courses)
                .FirstOrDefault(s => s.Id == student.Id);

            if (dbStudent == null)
            {
                Debug.WriteLine("DeleteStudent: Student not found in database");
                Students.Remove(student);
                return;
            }

            // Удаляем фото, если оно существует
            if (!string.IsNullOrEmpty(dbStudent.PhotoPath))
            {
                var possiblePaths = new[]
                {
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Photos", dbStudent.PhotoPath),
                    Path.Combine(Directory.GetCurrentDirectory(), "Photos", dbStudent.PhotoPath),
                    Path.Combine(Directory.GetCurrentDirectory(), "StudentManagement", "Photos", dbStudent.PhotoPath)
                };

                foreach (var photoPath in possiblePaths)
                {
                    try
                    {
                        if (File.Exists(photoPath))
                        {
                            File.Delete(photoPath);
                            Debug.WriteLine($"DeleteStudent: Deleted photo at {photoPath}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"DeleteStudent: Error deleting photo at {photoPath}: {ex.Message}");
                        // Продолжаем удаление даже если не удалось удалить фото
                    }
                }
            }

            // Очищаем связи с курсами
            dbStudent.Courses.Clear();
            context.SaveChanges();

            // Удаляем студента
            context.Students.Remove(dbStudent);
            context.SaveChanges();

            // Если удаляемый студент был выбран для редактирования, отменяем режим редактирования
            if (SelectedStudent == student)
            {
                IsEditing = false;
                SelectedStudent = null;
            }

            // Удаляем из коллекции
            Students.Remove(student);

            // Уведомляем об изменении студентов
            NotifyStudentsChanged();
            Debug.WriteLine("DeleteStudent: Successfully deleted student");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"DeleteStudent: Error occurred - {ex.Message}");
            Debug.WriteLine(ex.StackTrace);
            throw;
        }
    }

    [RelayCommand]
    public void StartEditing(Student student)
    {
        try
        {
            // Проверяем, существует ли студент в базе
            var dbStudent = _context.Students
                .Include(s => s.Courses)
                .FirstOrDefault(s => s.Id == student.Id);

            if (dbStudent == null)
            {
                LoadData();
                return;
            }

            // Обновляем список доступных курсов
            RefreshAvailableCourses();

            // Обновляем состояние выбора курсов
            foreach (var course in AvailableCourses)
            {
                course.IsSelected = dbStudent.Courses.Any(c => c.Id == course.Id);
            }

            SelectedStudent = student;
            IsEditing = true;
        }
        catch
        {
            LoadData();
            throw;
        }
    }

    public class CourseRemovalParameters
    {
        public Course Course { get; set; }
        public Student Student { get; set; }
    }

    public event EventHandler<CourseRemovalParameters> CourseRemoved;

    [RelayCommand]
    private void RemoveCourse(Course courseToRemove)
    {
        if (courseToRemove == null)
        {
            Debug.WriteLine("RemoveCourse: Course is null");
            return;
        }

        Debug.WriteLine($"RemoveCourse: Starting removal of course {courseToRemove.Id} - {courseToRemove.Name}");

        try
        {
            // Определяем, откуда происходит удаление
            bool isRemovingFromStudent = Students.Any(s => s.Courses.Contains(courseToRemove));
            Debug.WriteLine($"RemoveCourse: Removing from student: {isRemovingFromStudent}");

            using (var context = new ApplicationDbContext())
            {
                if (isRemovingFromStudent)
                {
                    // Если удаляем у студента, то только разрываем связь
                    Debug.WriteLine("RemoveCourse: Removing course from student only");
                    
                    var studentWithCourse = Students.First(s => s.Courses.Contains(courseToRemove));
                    var dbStudent = context.Students
                        .Include(s => s.Courses)
                        .First(s => s.Id == studentWithCourse.Id);

                    var dbCourse = dbStudent.Courses.First(c => c.Id == courseToRemove.Id);
                    dbStudent.Courses.Remove(dbCourse);
                    context.SaveChanges();

                    // Обновляем UI
                    studentWithCourse.Courses.Remove(courseToRemove);
                    var studentIndex = Students.IndexOf(studentWithCourse);
                    if (studentIndex != -1)
                    {
                        Students[studentIndex] = studentWithCourse;
                    }
                }
                else
                {
                    // Если удаляем сам курс
                    Debug.WriteLine("RemoveCourse: Removing course completely");
                    
                    var dbCourse = context.Courses
                        .Include(c => c.Students)
                        .FirstOrDefault(c => c.Id == courseToRemove.Id);

                    if (dbCourse != null)
                    {
                        // Очищаем все связи со студентами
                        dbCourse.Students.Clear();
                        context.SaveChanges();

                        // Удаляем сам курс
                        context.Courses.Remove(dbCourse);
                        context.SaveChanges();
                    }
                }
            }

            // Обновляем UI
            Debug.WriteLine("RemoveCourse: Refreshing UI");
            RefreshAvailableCourses();
            NotifyStudentsChanged();
            Debug.WriteLine("RemoveCourse: Operation completed successfully");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"RemoveCourse: Critical error occurred - {ex.Message}");
            Debug.WriteLine(ex.StackTrace);
        }
    }

    [RelayCommand]
    public void SaveChanges()
    {
        if (SelectedStudent == null) return;

        using var transaction = _context.Database.BeginTransaction();
        try
        {
            var dbStudent = _context.Students
                .Include(s => s.Courses)
                .FirstOrDefault(s => s.Id == SelectedStudent.Id);

            if (dbStudent == null)
            {
                LoadData();
                return;
            }

            // Обновляем основные данные студента
            dbStudent.Name = SelectedStudent.Name;
            dbStudent.PhotoPath = SelectedStudent.PhotoPath;

            // Обновляем курсы
            dbStudent.Courses.Clear();
            var selectedCourses = AvailableCourses.Where(c => c.IsSelected).ToList();
            foreach (var course in selectedCourses)
            {
                var dbCourse = _context.Courses.Find(course.Id);
                if (dbCourse != null)
                {
                    dbStudent.Courses.Add(dbCourse);
                }
            }

            _context.SaveChanges();
            transaction.Commit();

            // Обновляем UI
            SelectedStudent.Courses.Clear();
            foreach (var course in selectedCourses)
            {
                SelectedStudent.Courses.Add(new Course
                {
                    Id = course.Id,
                    Name = course.Name,
                    Description = course.Description
                });
            }

            IsEditing = false;
            SelectedStudent = null;

            // Уведомляем об изменении студентов
            NotifyStudentsChanged();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    [RelayCommand]
    public void CancelEditing()
    {
        if (SelectedStudent == null) return;

        // Перезагружаем данные
        LoadData();
        
        IsEditing = false;
        SelectedStudent = null;
    }

    [RelayCommand]
    private async Task SelectPhoto(Student student)
    {
        if (_storageProvider == null || student == null)
        {
            Debug.WriteLine("SelectPhoto: StorageProvider is not available or student is null");
            return;
        }

        try
        {
            var result = await _storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Выберите фото студента",
                AllowMultiple = false,
                FileTypeFilter = new[] 
                { 
                    new FilePickerFileType("Image Files")
                    {
                        Patterns = new[] { "*.jpg", "*.jpeg", "*.png" },
                        MimeTypes = new[] { "image/jpeg", "image/png" }
                    }
                }
            });

            var file = result.FirstOrDefault();
            if (file == null) return;

            try
            {
                Debug.WriteLine($"SelectPhoto: Starting photo selection for student {student.Id}");
                
                // Получаем пути к директориям
                var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                var currentDirectory = Directory.GetCurrentDirectory();
                var projectDirectory = Path.Combine(currentDirectory, "StudentManagement");
                
                Debug.WriteLine($"SelectPhoto: Base directory: {baseDirectory}");
                Debug.WriteLine($"SelectPhoto: Current directory: {currentDirectory}");
                Debug.WriteLine($"SelectPhoto: Project directory: {projectDirectory}");

                // Создаем все необходимые директории
                var directories = new[]
                {
                    Path.Combine(baseDirectory, "Photos"),
                    Path.Combine(currentDirectory, "Photos"),
                    Path.Combine(projectDirectory, "Photos")
                };

                foreach (var dir in directories)
                {
                    Directory.CreateDirectory(dir);
                    Debug.WriteLine($"SelectPhoto: Created/Verified directory: {dir}");
                }

                // Удаляем старые фото
                if (!string.IsNullOrEmpty(student.PhotoPath))
                {
                    foreach (var dir in directories)
                    {
                        var oldPath = Path.Combine(dir, student.PhotoPath);
                        if (File.Exists(oldPath))
                        {
                            File.Delete(oldPath);
                            Debug.WriteLine($"SelectPhoto: Deleted old photo: {oldPath}");
                        }
                    }
                }

                // Формируем имя нового файла
                var extension = Path.GetExtension(file.Path.LocalPath);
                var fileName = $"student_{student.Id}_{DateTime.Now:yyyyMMddHHmmss}{extension}";
                Debug.WriteLine($"SelectPhoto: New file name: {fileName}");

                // Копируем файл во все директории
                foreach (var dir in directories)
                {
                    var destinationPath = Path.Combine(dir, fileName);
                    try
                    {
                        using (var sourceStream = await file.OpenReadAsync())
                        using (var destinationStream = File.Create(destinationPath))
                        {
                            await sourceStream.CopyToAsync(destinationStream);
                        }
                        Debug.WriteLine($"SelectPhoto: Copied photo to: {destinationPath}");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"SelectPhoto: Error copying to {destinationPath}: {ex.Message}");
                    }
                }

                // Обновляем путь в базе данных
                using (var context = new ApplicationDbContext())
                {
                    var dbStudent = context.Students.Find(student.Id);
                    if (dbStudent != null)
                    {
                        dbStudent.PhotoPath = fileName;
                        await context.SaveChangesAsync();
                        Debug.WriteLine($"SelectPhoto: Updated database with new photo path: {fileName}");
                    }
                }

                // Обновляем UI
                student.PhotoPath = fileName;
                var index = Students.IndexOf(student);
                if (index != -1)
                {
                    Students[index] = student;
                    Debug.WriteLine($"SelectPhoto: Updated UI for student at index {index}");
                }

                Debug.WriteLine("SelectPhoto: Photo update completed successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SelectPhoto: Critical error - {ex.Message}");
                Debug.WriteLine(ex.StackTrace);
                throw;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"SelectPhoto: Critical error - {ex.Message}");
            Debug.WriteLine(ex.StackTrace);
        }
    }

    private void NotifyStudentsChanged()
    {
        StudentsChanged?.Invoke(this, EventArgs.Empty);
    }
} 