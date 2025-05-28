using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace StudentManagement.Models;

public partial class Student : ObservableObject
{
    [ObservableProperty]
    private int _id;

    [ObservableProperty]
    private string _name = "";

    [ObservableProperty]
    private string? _photoPath;

    [ObservableProperty]
    private ObservableCollection<Course> _courses = new();
} 