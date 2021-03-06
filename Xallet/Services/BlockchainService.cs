﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xallet.Extensions;
using Xallet.Models;

namespace Xallet.Services
{
    public class BlockchainService : IBlockchainService
    {
        private const string API_Accounts = "https://blockchain.info/balance?active={0}";
        private const string API_Price = "https://blockchain.info/ticker";
        private const string API_Transactions = "https://blockchain.info/rawaddr/{0}";
        private const double TokenFactor = 100000000;
        public async Task<double[]> GetAccountBalanceAsync(params string[] addresses)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var aggregatedAddresses = addresses.Aggregate((x, y) => $"{x}|{y}");
                    var route = string.Format(API_Accounts, aggregatedAddresses);

                    var result = await client.GetAsync(route);
                    if (result.IsSuccessStatusCode)
                    {
                        var content = await result.Content.ReadAsStringAsync();
                        var json = JObject.Parse(content);

                        var blockchainBalances = new List<BlockchainBalance>();
                        for (int index = 0; index < addresses.Length; index++)
                        {
                            var findBalance = json.SelectToken(addresses[index]);
                            if (findBalance != null)
                                blockchainBalances.Add(findBalance.ToObject<BlockchainBalance>());
                        }

                        return blockchainBalances.Select(x => x.Balance / TokenFactor).ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
            }

            return new double[addresses.Length];
        }

        public async Task<double> GetCryptoValueAsync()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var result = await client.GetAsync(API_Price);
                    if (result.IsSuccessStatusCode)
                    {
                        var content = await result.Content.ReadAsStringAsync();
                        var json = JObject.Parse(content);

                        var findUSD = json.SelectToken("USD");
                        if (findUSD != null)
                            return findUSD.ToObject<BlockchainPrice>().LastPrice;
                    }
                }
            }
            catch
            {
            }
            
            return 0;
        }

        public async Task<Transaction[]> GetTransactionsAsync(string address)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var route = string.Format(API_Transactions, address);
                    var result = await client.GetAsync(route);
                    if (result.IsSuccessStatusCode)
                    {
                        var content = await result.Content.ReadAsStringAsync();
                        var model = JsonConvert.DeserializeObject<BlockchainRawAddress>(content);

                        return model.Transactions
                            .Select(x => new Transaction
                            {
                                Hash = x.Hash,
                                PublicAddress = address,
                                Timestamp = DateTimeExtensions.UnixTimeToDateTimeUtc(x.Time),
                                Tokens = x.Result / TokenFactor
                            })
                            .ToArray();
                    }
                }
            }
                catch
            {
            }

            return new Transaction[0];
        }
    }
}
