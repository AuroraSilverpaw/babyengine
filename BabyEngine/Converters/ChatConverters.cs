using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace BabyEngine.Converters
{
    public class BoolToAlignmentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isFromMommy)
            {
                return isFromMommy ? HorizontalAlignment.Left : HorizontalAlignment.Right;
            }
            
            return HorizontalAlignment.Left;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    
    public class BoolToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isFromMommy)
            {
                return isFromMommy ? new SolidColorBrush(Color.FromRgb(147, 112, 219)) : new SolidColorBrush(Color.FromRgb(230, 230, 250));
            }
            
            return new SolidColorBrush(Color.FromRgb(147, 112, 219));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 