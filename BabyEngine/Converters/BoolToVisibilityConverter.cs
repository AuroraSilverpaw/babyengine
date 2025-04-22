using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace BabyEngine.Converters
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool boolValue = value is bool val && val;
            bool inverse = parameter != null && parameter.ToString()?.ToLower() == "inverse";
            
            if (inverse)
            {
                return boolValue ? Visibility.Collapsed : Visibility.Visible;
            }
            else
            {
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                bool inverse = parameter != null && parameter.ToString()?.ToLower() == "inverse";
                
                if (inverse)
                {
                    return visibility != Visibility.Visible;
                }
                else
                {
                    return visibility == Visibility.Visible;
                }
            }
            
            return false;
        }
    }
} 