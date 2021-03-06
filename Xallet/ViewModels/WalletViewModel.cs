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
            OpenWallet = new Command<Wallet>(OnOpenWallet);
            Refresh = new Command(OnRefresh);
            Remove = new Command<Wallet>(OnRemove);
            
            Initialize();
        }

        public ICommand Add { get; }
        public ICommand ShowCode { get; }
        public ICommand OpenWallet { get; }
        public ICommand Refresh { get; }
        public ICommand Remove { get; }

        private Amount _totalAmount;
        public Amount TotalAmount
        {
            get => _totalAmount;
            set => SetProperty(ref _totalAmount, value);
        }

        private Amount _totalEther;
        public Amount TotalEther
        {
            get => _totalEther;
            set => SetProperty(ref _totalEther, value);
        }

        private Amount _totalBitcoin;
        public Amount TotalBitcoin
        {
            get => _totalBitcoin;
            set => SetProperty(ref _totalBitcoin, value);
        }

        private bool _isRefreshing;
        public bool IsRefreshing
        {
            get => _isRefreshing;
            set => SetProperty(ref _isRefreshing, value);
        }

        private double _etherFiatValue;
        public double EtherFiatValue
        {
            get => _etherFiatValue;
            set => SetProperty(ref _etherFiatValue, value);
        }

        private double _bitcoinFiatValue;
        public double BitcoinFiatValue
        {
            get => _bitcoinFiatValue;
            set => SetProperty(ref _bitcoinFiatValue, value);
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
            var etherFiat = FiatService.GetCurrentRate(CryptoCurrency.Ethereum);
            var bitcoinFiat = FiatService.GetCurrentRate(CryptoCurrency.Bitcoin);

            if (etherFiat != null)
                EtherFiatValue = etherFiat.Rate;

            if (bitcoinFiat != null)
                BitcoinFiatValue = bitcoinFiat.Rate;

            var wallets = WalletService
                .GetWallets()
                .OrderBy(x => x.FriendlyName)
                .Select(x =>
                {
                    var rate = x.CryptoCurrency == CryptoCurrency.Bitcoin ? bitcoinFiat : etherFiat;
                    return x.ToWallet(rate);
                });

            Wallets = new ObservableCollection<Wallet>(wallets);

            TotalAmount = new Amount
            {
                Value = Math.Round(Wallets == null || Wallets.Count() == 0 ? 0 : Wallets.Select(x => x.LocalCurrency.Value).Aggregate((x, y) => x + y), 2),
                Currency = "USD"
            };

            var etherWallets = Wallets?.Where(x => x.TokenType == CryptoCurrency.Ethereum);
            TotalEther = new Amount
            {
                Value = etherWallets == null || etherWallets.Count() == 0 ? 0 : etherWallets.Select(x => x.Token.Value).Aggregate((x, y) => x + y),
                Currency = "ETH"
            };

            var bitcoinWallets = Wallets?.Where(x => x.TokenType == CryptoCurrency.Bitcoin);
            TotalBitcoin = new Amount
            {
                Value = bitcoinWallets == null || bitcoinWallets.Count() == 0 ? 0 : bitcoinWallets.Select(x => x.Token.Value).Aggregate((x, y) => x + y),
                Currency = "BTC"
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
        private void OnOpenWallet(Wallet item)
        {
            if (_isEditing)
                return;

            _isEditing = true;
            App.Current.MainPage.Navigation.PushAsync(new AddressInfoPage(item));
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

        private void OnRemove(Wallet wallet)
        {
            if (wallet == null)
                return;

            if (WalletService.RemoveWallet(wallet.Id))
                Wallets.Remove(wallet);
        }

        private void OnNewWallet(AddOrUpdateWalletViewModel sender, WalletEntity args)
        {
            if (args != null)
                Initialize();
        }
    }
}
