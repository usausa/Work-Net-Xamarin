﻿namespace BindingTest
{
    using Xamarin.Forms;

    public class ViewProperty
    {
        public static readonly BindableProperty TitleProperty = BindableProperty.CreateAttached(
            "Title",
            typeof(string),
            typeof(ViewProperty),
            null,
            propertyChanged: PropertyChanged);

        public static string GetTitle(BindableObject view)
        {
            System.Diagnostics.Debug.WriteLine("GetTitle " + view.GetHashCode());

            return (string)view.GetValue(TitleProperty);
        }

        public static void SetTitle(BindableObject view, string value)
        {
            System.Diagnostics.Debug.WriteLine("SetTitle " + view.GetHashCode());

            view.SetValue(TitleProperty, value);
        }

        private static void PropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            System.Diagnostics.Debug.WriteLine($"PropertyChanged {bindable.GetHashCode()} {oldValue} {newValue}");
            var element = ((ContentView)bindable).Parent;
            while (element != null)
            {
                if (element.BindingContext is ViewPropertyModel vm)
                {
                    vm.Title = GetTitle(bindable);
                    break;
                }

                element = element.Parent;
            }
        }
    }
}
