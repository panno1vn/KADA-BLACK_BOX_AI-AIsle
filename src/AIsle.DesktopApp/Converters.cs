using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace AIsle.DesktopApp.Converters
{
    public class BooleanToBrushConverter : IValueConverter
    {
        public Brush TrueBrush { get; set; } = Brushes.Transparent;
        public Brush FalseBrush { get; set; } = Brushes.Transparent;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b && b) return TrueBrush;
            return FalseBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class InverseNullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class ShelfToImageConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string cat = "";
            string label = "";
            string id = "";
            if (value is AIsle.DesktopApp.Services.Shelf shelf)
            {
                cat = shelf.Category;
                label = shelf.Label;
                id = shelf.Id;
            }
            else if (value is string str)
            {
                cat = str;
            }

            var text = $"{cat} {label} {id}".ToLowerInvariant();
            if (text.Contains("uong") || text.Contains("beverage") || text.Contains("s1"))
                return "pack://application:,,,/UI/assets/store/shelves/do_uong.jpg";
            if (text.Contains("tuoi") || text.Contains("song") || text.Contains("nhanh") || text.Contains("instant-food") || text.Contains("fresh") || text.Contains("s2"))
                return "pack://application:,,,/UI/assets/store/shelves/hang_tuoi_song.png";
            if (text.Contains("snack") || text.Contains("candy") || text.Contains("keo") || text.Contains("s3") || text.Contains("s6"))
                return "pack://application:,,,/UI/assets/store/shelves/snack.png";
            if (text.Contains("kho") || text.Contains("nhan") || text.Contains("personal-care") || text.Contains("s4"))
                return "pack://application:,,,/UI/assets/store/shelves/hang_kho_cham_soc_ca_nhan.png";
            if (text.Contains("hoa") || text.Contains("dung") || text.Contains("household") || text.Contains("s5"))
                return "pack://application:,,,/UI/assets/store/shelves/hoa_pham.png";

            return "pack://application:,,,/UI/assets/store/shelves/hang_kho_cham_soc_ca_nhan.png";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class BoolToScaleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b && b) return -1.0;
            return 1.0;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}


