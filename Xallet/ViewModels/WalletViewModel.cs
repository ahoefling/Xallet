﻿using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Xallet.Data;
using Xallet.Extensions;
using Xallet.Models;
using Xallet.Services;
using Xallet.Views;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace Xallet.ViewModels
{
    public class WalletViewModel : BindableBase
    {
        protected WalletService WalletService { get; }
        protected FiatService FiatService { get; }
        public WalletViewModel()
        {
            WalletService = new WalletService();
            FiatService = new FiatService();

            MessagingCenter.Instance.Subscribe<AddOrUpdateWalletViewModel, WalletEntity>(this, "AddOrUpdateWallet", OnNewWallet);
            Add = new Command(OnAdd);
            ShowCode = new Command<Wallet>(OnShowCode);
            EditWallet = new Command<Wallet>(OnEditWallet);
            Refresh = new Command(OnRefresh);
            
            Initialize();
        }

        public ICommand Add { get; }
        public ICommand ShowCode { get; }
        public ICommand EditWallet { get; }
        public ICommand Refresh { get; }

        private Amount _totalAmount;
        public Amount TotalAmount
        {
            get => _totalAmount;
            set => SetProperty(ref _totalAmount, value);
        }

        private bool _isRefreshing;
        public bool IsRefreshing
        {
            get => _isRefreshing;
            set => SetProperty(ref _isRefreshing, value);
        }

        private ObservableCollection<Wallet> _wallets;
        public ObservableCollection<Wallet> Wallets
        {
            get => _wallets;
            set => SetProperty(ref _wallets, value);
        }

        private async void Initialize()
        {
            LoadLocalData();
            if (Connectivity.NetworkAccess == NetworkAccess.Internet)
            {
                await WalletService.SyncWithBlockchainAsync();
                LoadLocalData();
            }
        }

        private void LoadLocalData()
        {
            var wallets = WalletService
                .GetWallets()
                .OrderBy(x => x.FriendlyName)
                .Select(x =>
                {
                    var rate = FiatService.GetCurrentRate(x.CryptoCurrency);
                    return x.ToWallet(rate);
                });

            Wallets = new ObservableCollection<Wallet>(wallets);
            TotalAmount = new Amount
            {
                Value = Math.Round(Wallets == null || Wallets.Count() == 0 ? 0 : Wallets.Select(x => x.LocalCurrency.Value).Aggregate((x, y) => x + y), 2),
                Currency = "USD"
            };
        }

        private void OnAdd()
        {
            App.Current.MainPage.Navigation.PushAsync(new AddOrUpdateWalletPage());
        }

        private bool _isShowingCode = false;
        private void OnShowCode(Wallet item)
        {
            if (_isShowingCode)
                return;

            _isShowingCode = true;
            App.Current.MainPage.Navigation.PushAsync(new AddressCodePage(item));
            _isShowingCode = false;
        }

        private bool _isEditing = false;
        private void OnEditWallet(Wallet item)
        {
            if (_isEditing)
                return;

            _isEditing = true;
            App.Current.MainPage.Navigation.PushAsync(new AddOrUpdateWalletPage(item));
            _isEditing = false;
        }

        private async void OnRefresh()
        {
            if (IsRefreshing)
                return;

            IsRefreshing = true;

            if (Connectivity.NetworkAccess == NetworkAccess.Internet)
            {
                await WalletService.SyncWithBlockchainAsync();
                LoadLocalData();
            }

            IsRefreshing = false;
        }

        private void OnNewWallet(AddOrUpdateWalletViewModel sender, WalletEntity args)
        {
            if (args != null)
                Initialize();
        }
    }
}
