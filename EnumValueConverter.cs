using System;
using System.ComponentModel;
using System.Reflection;
using System.Windows.Data;

namespace NintendoMetadata
{
    public class EnumValueConverter : IValueConverter
    {
        private string GetEnumDescription(Enum enumObj)
        {
            FieldInfo fieldInfo = enumObj.GetType().GetField(enumObj.ToString());
            object[] attribArray = fieldInfo.GetCustomAttributes(false);

            if (attribArray.Length == 0)
            {
                return enumObj.ToString();
            }
            else
            {
                DescriptionAttribute attrib = null;

                foreach (var att in attribArray)
                {
                    if (att is DescriptionAttribute)
                        attrib = att as DescriptionAttribute;
                }

                if (attrib != null)
                    return attrib.Description;

                return enumObj.ToString();
            }
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }
            string description = GetEnumDescription((Enum)value);
            return description;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return string.Empty;
        }

    }
}
