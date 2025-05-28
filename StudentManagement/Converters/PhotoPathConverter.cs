using Avalonia.Data.Converters;
using System;
using System.Globalization;
using System.IO;
using System.Diagnostics;
using Avalonia.Media.Imaging;

namespace StudentManagement.Converters
{
    public class PhotoPathConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            try
            {
                Debug.WriteLine($"PhotoPathConverter: Converting value: {value}");
                
                if (value is string photoPath && !string.IsNullOrEmpty(photoPath))
                {
                    var possiblePaths = new[]
                    {
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Photos", photoPath),
                        Path.Combine(Directory.GetCurrentDirectory(), "Photos", photoPath),
                        Path.Combine(Directory.GetCurrentDirectory(), "StudentManagement", "Photos", photoPath)
                    };

                    foreach (var path in possiblePaths)
                    {
                        Debug.WriteLine($"PhotoPathConverter: Checking path: {path}");
                        
                        if (File.Exists(path))
                        {
                            Debug.WriteLine($"PhotoPathConverter: Found file at: {path}");
                            try
                            {
                                using var stream = File.OpenRead(path);
                                var bitmap = new Bitmap(stream);
                                Debug.WriteLine($"PhotoPathConverter: Successfully created Bitmap from {path}");
                                return bitmap;
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"PhotoPathConverter: Error creating Bitmap from {path}: {ex.Message}");
                            }
                        }
                    }

                    Debug.WriteLine("PhotoPathConverter: File not found in any location");
                    
                    // Выводим содержимое папок для отладки
                    foreach (var basePath in new[] { 
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Photos"),
                        Path.Combine(Directory.GetCurrentDirectory(), "Photos"),
                        Path.Combine(Directory.GetCurrentDirectory(), "StudentManagement", "Photos")
                    })
                    {
                        if (Directory.Exists(basePath))
                        {
                            Debug.WriteLine($"PhotoPathConverter: Contents of {basePath}:");
                            foreach (var file in Directory.GetFiles(basePath))
                            {
                                Debug.WriteLine($"  - {Path.GetFileName(file)}");
                            }
                        }
                        else
                        {
                            Debug.WriteLine($"PhotoPathConverter: Directory does not exist: {basePath}");
                        }
                    }
                }
                else
                {
                    Debug.WriteLine("PhotoPathConverter: Input value is null or empty");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PhotoPathConverter: Error - {ex.Message}");
                Debug.WriteLine(ex.StackTrace);
            }
            
            return null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 