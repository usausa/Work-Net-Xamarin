namespace KeySample.FormsApp.Effects
{
    using System.Linq;

    using Xamarin.Forms;

    public sealed class FocusableEffect : RoutingEffect
    {
        public static readonly BindableProperty OnProperty = BindableProperty.CreateAttached(
            "On",
            typeof(bool),
            typeof(FocusableEffect),
            false,
            propertyChanged: OnOnChanged);

        public static bool GetOn(BindableObject view)
        {
            return (bool)view.GetValue(OnProperty);
        }

        public static void SetOn(BindableObject view, bool value)
        {
            view.SetValue(OnProperty, value);
        }

        private static void OnOnChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is not Element element)
            {
                return;
            }

            if ((bool)newValue)
            {
                element.Effects.Add(new FocusableEffect());
            }
            else
            {
                var effect = element.Effects.FirstOrDefault(x => x is FocusableEffect);
                if (effect != null)
                {
                    element.Effects.Remove(effect);
                }
            }
        }

        public FocusableEffect()
            : base("KeySample.FocusableEffect")
        {
        }
    }
}
