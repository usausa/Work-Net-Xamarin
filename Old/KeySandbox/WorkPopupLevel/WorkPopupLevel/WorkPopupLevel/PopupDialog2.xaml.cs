﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.CommunityToolkit.UI.Views;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace WorkPopupLevel
{
    public partial class PopupDialog2
    {
        public PopupDialog2()
        {
            InitializeComponent();
        }

        private void CountButton_OnClicked(object sender, EventArgs e)
        {
            Debug.WriteLine("**Navigation : " + Application.Current.MainPage.Navigation.NavigationStack.Count);
            Debug.WriteLine("**Modal : " + Application.Current.MainPage.Navigation.ModalStack.Count);
        }

        private void CloseButton_OnClicked(object sender, EventArgs e)
        {
            Dismiss("ok");
        }

        public static IEnumerable<Element> FindChildren(Element parent, int level)
        {
            var indent = new string(' ', level * 2);

            foreach (var child in parent.LogicalChildren)
            {
                Debug.WriteLine($"**{indent} {child.GetType()} {child.GetHashCode()}");
                yield return child;

                foreach (var child2 in FindChildren(child, level + 1))
                {
                    yield return child2;
                }
            }
        }

        private void PopupDialog2_OnDismissed(object sender, PopupDismissedEventArgs e)
        {
            Debug.WriteLine("*******************PopupDialog2_OnDismissed");
        }
    }
}