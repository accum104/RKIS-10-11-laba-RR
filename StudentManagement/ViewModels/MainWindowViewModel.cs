using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
using StudentManagement.Data;
using System;

namespace StudentManagement.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private ViewModelBase? _currentViewModel;
    private readonly ApplicationDbContext _context;
    private readonly IStorageProvider _storageProvider;

    public ViewModelBase? CurrentViewModel
    {
        get => _currentViewModel;
        set => SetProperty(ref _currentViewModel, value);
    }

    public MainWindowViewModel(ApplicationDbContext context, IStorageProvider storageProvider)
    {
        _context = context;
        _storageProvider = storageProvider;

        // Создаем экземпляры ViewModel
        var studentsViewModel = new StudentsViewModel(_context, _storageProvider);
        var coursesViewModel = new CoursesViewModel(_context);

        // Подписываемся на события изменения данных
        studentsViewModel.StudentsChanged += (s, e) => coursesViewModel.LoadData();
        coursesViewModel.CoursesChanged += (s, e) => studentsViewModel.LoadData();

        // Устанавливаем начальное представление
        CurrentViewModel = studentsViewModel;
    }

    [RelayCommand]
    public void NavigateToStudents()
    {
        try
        {
            if (CurrentViewModel is StudentsViewModel studentsViewModel)
            {
                studentsViewModel.LoadData();
            }
            else
            {
                var newStudentsViewModel = new StudentsViewModel(_context, _storageProvider);
                if (CurrentViewModel is CoursesViewModel coursesViewModel)
                {
                    newStudentsViewModel.StudentsChanged += (s, e) => coursesViewModel.LoadData();
                }
                CurrentViewModel = newStudentsViewModel;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error navigating to students: {ex.Message}");
            throw;
        }
    }

    [RelayCommand]
    public void NavigateToCourses()
    {
        try
        {
            if (CurrentViewModel is CoursesViewModel coursesViewModel)
            {
                coursesViewModel.LoadData();
            }
            else
            {
                var newCoursesViewModel = new CoursesViewModel(_context);
                if (CurrentViewModel is StudentsViewModel studentsViewModel)
                {
                    newCoursesViewModel.CoursesChanged += (s, e) => studentsViewModel.LoadData();
                }
                CurrentViewModel = newCoursesViewModel;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error navigating to courses: {ex.Message}");
            throw;
        }
    }
}
