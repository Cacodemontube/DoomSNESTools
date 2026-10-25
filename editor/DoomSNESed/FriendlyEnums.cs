using System;
using System.ComponentModel;
using System.Globalization;
using System.Text.RegularExpressions;
namespace DoomSNESed {
    public sealed class FriendlyEnumConverter:EnumConverter {
        public FriendlyEnumConverter(Type type):base(type){}
        static string Label(string name){return Regex.Replace(Regex.Replace(name,"([a-z])([A-Z0-9])","$1 $2"),"([0-9])([A-Z])","$1 $2");}
        public override object ConvertTo(ITypeDescriptorContext context,CultureInfo culture,object value,Type destinationType){if(destinationType==typeof(string)&&value!=null)return Label(value.ToString());return base.ConvertTo(context,culture,value,destinationType);}
        public override object ConvertFrom(ITypeDescriptorContext context,CultureInfo culture,object value){string text=value as string;if(text!=null)foreach(string name in Enum.GetNames(EnumType))if(Label(name)==text)return Enum.Parse(EnumType,name);return base.ConvertFrom(context,culture,value);}
    }
}
