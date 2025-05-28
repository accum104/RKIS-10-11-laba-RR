using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using StudentManagement.ViewModels;
using StudentManagement.Views;
using Avalonia.Platform.Storage;
using StudentManagement.Data;
using Microsoft.EntityFrameworkCore;

namespace StudentManagement;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Line below is needed to remove Avalonia data validation.
        // Without this line you will get duplicate validations from both Avalonia and CT
        DisableAvaloniaDataAnnotationValidation();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Создаем и инициализируем контекст базы данных
            var dbContext = new ApplicationDbContext();
            dbContext.Database.EnsureCreated();

            var mainWindow = new MainWindow();
            
            // Получаем IStorageProvider из окна
            var storageProvider = mainWindow.StorageProvider;

            // Создаем ViewModel и устанавливаем ее как DataContext
            mainWindow.DataContext = new MainWindowViewModel(dbContext, storageProvider);
            
            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}