using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace StudentManagement.Models;

public partial class Course : ObservableObject
{
    [ObservableProperty]
    private int _id;

    [ObservableProperty]
    private string _name = "";

    [ObservableProperty]
    private string _description = "";

    [ObservableProperty]
    private ObservableCollection<Student> _students = new();

    [ObservableProperty]
    [NotMapped]
    private bool _isSelected;
} 