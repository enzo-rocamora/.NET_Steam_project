using System.Globalization;

namespace Gauniv.Client.Converters
{
    public class UserStatusColorConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length == 0 || values[0] == null)
                return Colors.Gray;

            var status = (UserStatus)values[0];
            return status switch
            {
                UserStatus.Online => Colors.Green,
                UserStatus.InGame => Colors.Blue,
                UserStatus.Offline => Colors.Gray,
                _ => Colors.Gray
            };
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}