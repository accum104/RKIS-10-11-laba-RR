using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StudentManagement.Models;
using StudentManagement.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Diagnostics;

namespace StudentManagement.ViewModels;

public partial class CoursesViewModel : ViewModelBase
{
    private readonly ApplicationDbContext _context;
    
    public event EventHandler? CoursesChanged;

    [ObservableProperty]
    private ObservableCollection<Course> _courses = new();
    
    [ObservableProperty]
    private string _newCourseName = "";
    
    [ObservableProperty]
    private string _newCourseDescription = "";

    [ObservableProperty]
    private Course? _selectedCourse;

    [ObservableProperty]
    private bool _isEditing;
    
    public CoursesViewModel(ApplicationDbContext context)
    {
        _context = context;
        LoadData();
    }

    public void LoadData()
    {
        Debug.WriteLine("CoursesViewModel: Loading data...");
        
        // Загружаем курсы вместе со студентами
        var loadedCourses = _context.Courses
            .Include(c => c.Students)
            .AsNoTracking()
            .Select(c => new Course
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                Students = new ObservableCollection<Student>(
                    c.Students.Select(s => new Student
                    {
                        Id = s.Id,
                        Name = s.Name,
                        PhotoPath = s.PhotoPath
                    })
                )
            })
            .ToList();

        Debug.WriteLine($"CoursesViewModel: Loaded {loadedCourses.Count} courses");
        
        Courses.Clear();
        foreach (var course in loadedCourses)
        {
            Courses.Add(course);
            Debug.WriteLine($"CoursesViewModel: Course {course.Name} has {course.Students.Count} students");
        }
    }

    private void NotifyCoursesChanged()
    {
        CoursesChanged?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    public void AddCourse()
    {
        if (string.IsNullOrWhiteSpace(NewCourseName))
            return;

        using var transaction = _context.Database.BeginTransaction();
        try
        {
            var course = new Course 
            { 
                Name = NewCourseName,
                Description = NewCourseDescription
            };
            
            _context.Courses.Add(course);
            _context.SaveChanges();

            var newCourse = new Course
            {
                Id = course.Id,
                Name = course.Name,
                Description = course.Description,
                Students = new ObservableCollection<Student>()
            };
            
            Courses.Add(newCourse);
            
            NewCourseName = "";
            NewCourseDescription = "";

            transaction.Commit();

            // Уведомляем об изменении курсов
            NotifyCoursesChanged();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error adding course: {ex.Message}");
            transaction.Rollback();
            throw;
        }
    }

    [RelayCommand]
    public void DeleteCourse(Course course)
    {
        if (course == null) return;

        using var transaction = _context.Database.BeginTransaction();
        try
        {
            var dbCourse = _context.Courses
                .Include(c => c.Students)
                .FirstOrDefault(c => c.Id == course.Id);

            if (dbCourse == null)
            {
                Courses.Remove(course);
                return;
            }

            // Очищаем связи со студентами
            dbCourse.Students.Clear();
            _context.SaveChanges();

            _context.Courses.Remove(dbCourse);
            _context.SaveChanges();

            transaction.Commit();
            
            // Если удаляемый курс был выбран для редактирования, отменяем режим редактирования
            if (SelectedCourse == course)
            {
                IsEditing = false;
                SelectedCourse = null;
            }

            Courses.Remove(course);
            
            // Уведомляем об изменении курсов
            NotifyCoursesChanged();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error deleting course: {ex.Message}");
            transaction.Rollback();
            throw;
        }
    }

    [RelayCommand]
    public void StartEditing(Course course)
    {
        if (course == null) return;
        SelectedCourse = course;
        IsEditing = true;
    }

    [RelayCommand]
    public void SaveChanges()
    {
        if (SelectedCourse == null) return;

        using var transaction = _context.Database.BeginTransaction();
        try
        {
            var dbCourse = _context.Courses.Find(SelectedCourse.Id);
            if (dbCourse != null)
            {
                dbCourse.Name = SelectedCourse.Name;
                dbCourse.Description = SelectedCourse.Description;
                _context.SaveChanges();
            }

            transaction.Commit();
            IsEditing = false;
            SelectedCourse = null;

            // Обновляем данные
            LoadData();

            // Уведомляем об изменении курсов
            NotifyCoursesChanged();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error saving course: {ex.Message}");
            transaction.Rollback();
            throw;
        }
    }

    [RelayCommand]
    public void CancelEditing()
    {
        if (SelectedCourse != null)
        {
            var dbCourse = _context.Courses.Find(SelectedCourse.Id);
            if (dbCourse != null)
            {
                SelectedCourse.Name = dbCourse.Name;
                SelectedCourse.Description = dbCourse.Description;
            }
        }
        IsEditing = false;
        SelectedCourse = null;
    }
} 