﻿using System;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Xallet.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class WalletPage : ContentPage
    {
        public WalletPage()
        {
            InitializeComponent();
        }

        private void AddItemClicked(object sender, EventArgs e)
        {
            Navigation.PushAsync(new NewWalletPage());
        }
    }
}
