using BBPAPI;
using BMSCommon;
using BMSCommon.Model;
using NBitcoin;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Util;
using Nethereum.Web3;
using System.Numerics;
using static BBPAPI.DB;
using static BMSCommon.Encryption;

namespace BBP.CORE.API.Utilities
{

    public class AttributeValue
	{
		public string trait_type { get; set; }
		public string value { get; set; }
		public AttributeValue()
		{
			trait_type = String.Empty;
			value = String.Empty;

		}
	}

	public class NFTPayload
	{
		public string name { get; set; }
		public string external_url { get; set; }
		public string description { get; set; }
		public List<AttributeValue> attributes { get; set; }
		public double royalty { get; set; }
		public List<string> creator { get; set; }
		public string image { get; set; }
		public string fileType { get; set; }
		public string preview { get; set; }
		public NFTPayload()
		{
			name = String.Empty;
			description = String.Empty;
			attributes = new List<AttributeValue>();
			royalty = 0;
			creator = new List<string>();
			image = String.Empty;	
			fileType = String.Empty;	
			preview = String.Empty;	
		}
	
	
		public static string CreateJsonPayload(RetiredNFT n, string sURL)
		{
			NFTPayload p = new NFTPayload();
			p.name = n.Name;
			p.external_url = sURL;
			p.description = n.Description;
			p.attributes.Add(new AttributeValue { trait_type = "Type", value = n.Type });
			if (n.TreasureChestURL != null && n.TreasureChestURL.Length > 2)
			{
				p.attributes.Add(new AttributeValue { trait_type = "Treasure Chest", value = "Contains Treasure Chest" });
				p.attributes.Add(new AttributeValue { trait_type = "Treasure Chest Description", value = n.TreasureChestDescription });
			}
			if (n.SoulBound==1)
			{
				p.attributes.Add(new AttributeValue { trait_type = "Soul Bound", value = "True" });
			}
			p.creator.Add("BiblePay User");
			p.fileType = BMSCommon.Encryption.GetContentType(n.AssetURL);
			p.image = GetImageEncodedURL(n.AssetURL);
			string data = Newtonsoft.Json.JsonConvert.SerializeObject(p, Newtonsoft.Json.Formatting.Indented);
			return data;
		}
		public static string CreateJsonPayloadTreasureChest(RetiredNFT n)
		{
			NFTPayload p = new NFTPayload();
			p.name = n.Name;
			p.description = n.TreasureChestDescription;
			p.attributes.Add(new AttributeValue { trait_type = "Type", value = n.Type });
			p.attributes.Add(new AttributeValue { trait_type = "Treasure Chest", value = "Contains Treasure Chest" });
			p.attributes.Add(new AttributeValue { trait_type = "Treasure Chest URL", value = GetImageEncodedURL(n.TreasureChestURL) });
			p.fileType = BMSCommon.Encryption.GetContentType(n.TreasureChestURL);
			if (p.fileType.Contains("image"))
			{
				p.image = n.TreasureChestURL;
			}
			string data = Newtonsoft.Json.JsonConvert.SerializeObject(p, Newtonsoft.Json.Formatting.Indented);
			return data;
		}

		public static string sCDN = "https://api.biblepay.org/api/pin/getdata/";
		public static string GetImageEncodedURL(string sURL)
		{
			sURL = sURL.ToStr();
			string sURLLow = sCDN + HexadecimalEncoding.StringToHex(sURL);
			return sURLLow;
		}

		public static async Task<bool> SetNFTJsonProperties(RetiredNFT n)
		{
			try
			{
				string sTempDir = System.IO.Path.GetTempPath();
				int nTokenID = -1;
				n.TokenID = nTokenID.ToString();
				string sPath = nTokenID.ToString() + ".json";
				string sFullPath = Path.Combine(sTempDir, sPath);
				string sFullURL = "https://api.biblepay.org/api/pin/getnft/" + nTokenID.ToString();

				string sData = CreateJsonPayload(n, sFullURL);
				System.IO.File.WriteAllText(sFullPath, sData);
				string sDest = "nft/" + nTokenID.ToString();
				string url = await StorjIO.StorjUpload(sFullPath, sDest, null);
				n.JSONURL = url;
				// TreasureChest
				int nTCID = BMSCommon.Common.UnixTimestamp();
				string sTCPath = nTCID.ToString() + ".json";
				string sTCFullPath = Path.Combine(sTempDir, sTCPath);
				string sDestTC = "nft/" + nTCID.ToString();
				string sDataTC = CreateJsonPayloadTreasureChest(n);
				System.IO.File.WriteAllText(sTCFullPath, sDataTC);
				string urlTC = await StorjIO.StorjUpload(sTCFullPath, sDestTC, null);
				n.TreasureChestJSONURL = urlTC;
				return true;
			}
			catch(Exception ex)
			{
				return false;
			}
		}
	}

	public class Ethereum
	{

        public static async Task<double> GetAltcoinBalance(string sNetwork, string sAddress)
        {
            if (sNetwork.ToLower().StartsWith("doge"))
            {
                string sUTXOFoundation = DOGE.GetDogeUtxos(sAddress);
                double nFoundationBal = NBitcoin.Crypto.BBPTransaction.QueryAltcoinBalance(sUTXOFoundation);
                return nFoundationBal;
            }
            else if (sNetwork.ToLower().StartsWith("arb"))
            {
                double nAmt = await Ethereum.GetERC20Balance("ARB", sAddress);
                return nAmt;
            }
            else if (sNetwork.ToLower() == "stellar")
            {
                double nBal = await Stellar.GetAccountBalance(sAddress);
                return nBal;
            }
            else
            {
                throw new Exception("Unknown network");
            }
        }
        public static async Task<BBPKeyPair> DeriveAltcoinKey(string sNetwork, string sSha)
        {
            if (sNetwork.ToLower() == "stellar")
            {
                BBPKeyPair bbpkp = new BBPKeyPair();
                stellar_dotnet_sdk.KeyPair stellarPair = BBP.CORE.API.Utilities.Stellar.DeriveStellarKeypair(sSha);
                bbpkp.PubKey = stellarPair.AccountId;
                bbpkp.PrivKey = stellarPair.SecretSeed;
                return bbpkp;
            }
            else if (sNetwork.ToLower() == "doge")
            {
                NBitcoin.Mnemonic m = new NBitcoin.Mnemonic(sSha);
                NBitcoin.ExtKey k = m.DeriveExtKey(null);
                Network n = NBitcoin.Network.GetNetwork("dogenet");
                BBPKeyPair k1 = new BBPKeyPair
                {
                    PrivKey = k.PrivateKey.GetWif(n).ToWif().ToString(),
                    PubKey = k.ScriptPubKey.GetDestinationAddress(n).ToString()
                };
                return k1;
            }
			else if (sNetwork.ToLower()=="exp")
			{
                NBitcoin.Mnemonic m = new NBitcoin.Mnemonic(sSha);
                NBitcoin.ExtKey k = m.DeriveExtKey2(null);
                Network n = NBitcoin.Network.GetNetwork("dogenet");
				BBPKeyPair k1 = new BBPKeyPair
				{
					PrivKey = k.PrivateKey.GetWif(n).ToWif().ToString(),
					PubKey = k.ScriptPubKey.GetDestinationAddress(n).ToString()
					
				};
                return k1;
            }
            else if (sNetwork.ToLower() == "biblepay")
            {
                NBitcoin.Mnemonic m = new NBitcoin.Mnemonic(sSha);
                NBitcoin.ExtKey k = m.DeriveExtKey(null);
				Network n = Network.Main;
                BBPKeyPair k1 = new BBPKeyPair
                {
                    PrivKey = k.PrivateKey.GetWif(n).ToWif().ToString(),
                    PubKey = k.ScriptPubKey.GetDestinationAddress(n).ToString()
                };
                return k1;
            }

            else if (sNetwork.ToLower().StartsWith("arb"))
            {
                BBPKeyPair k1 = await Ethereum.DeriveEthereumKey(sSha);
                return k1;
            }
            else if (sNetwork.ToLower().StartsWith("bbp"))
            {
                BBPKeyPair k1 = new BBPKeyPair();
                k1.PrivKey = sSha;
                k1.PubKey = ERCUtilities.GetPubKeyFromPrivKey(false, k1.PrivKey);
                return k1;
            }
            else
            {
                return null;
            }
        }




        [FunctionOutput]
		public class PriceOut : IFunctionOutputDTO
		{
			[Parameter("uint80", "roundId", 1)]
			public BigInteger roundId { get; set; }

			[Parameter("int256", "answer", 1)]

			public BigInteger answer { get; set; }
			[Parameter("uint256", "startedAt", 1)]

			public BigInteger startedAt { get; set; }
			[Parameter("uint256", "startedAt", 1)]

			public BigInteger updatedAt { get; set; }
			[Parameter("uint80", "startedAt", 1)]

			public BigInteger answeredInRound { get; set; }

		}

		private static string ARB_RPC_ADDRESS = "https://arb1.arbitrum.io/rpc";
		private static string PRIV_ARB_KEY =             "";
		private static string PUB_ARB_KEY =              "0xaFe8C2709541E72F245e0DA0035f52DE5bdF3ee5";
		private static string FOUNDATION_ETHER_ADDRESS = "0xaFe8C2709541E72F245e0DA0035f52DE5bdF3ee5";

		public static async Task<BBPKeyPair> GetFDKeyPairEthereum()
		{
			BBPKeyPair b = new BBPKeyPair();
			b.PrivKey = PRIV_ARB_KEY;
			b.PubKey = PUB_ARB_KEY;
			return b;
		}

		public static async Task<BBPKeyPair> DeriveEthereumKey(string sBBPPrivKey)
		{
			BBPKeyPair b = new BBPKeyPair();
			string s1 = HexadecimalEncoding.StringToHex(sBBPPrivKey);
			string s2 = "0x" + s1.Substring(0, 64);
			var account = new Nethereum.Web3.Accounts.Account(s2, Nethereum.Signer.Chain.Arbitrum);
			b.PrivKey = account.PrivateKey;
			b.PubKey = account.Address;
			return b;
		}
		public static async Task<double> GetChainlinkPrice(string sSymbol)
		{
			try
			{
				Web3 web3 = await GetWeb3Connection(await GetEthKeyPairFoundation());
				var c = await GetChainLinkContract(web3, sSymbol);
				var aggFunction = c.GetFunction("latestRoundData");
				PriceOut myPrice = await aggFunction.CallDeserializingToObjectAsync<PriceOut>();
				decimal mySmallPrice = (decimal)((double)myPrice.answer / (double)100000000);
				return (double)mySmallPrice;
			}
			catch(Exception ex)
			{
				return -1;
			}
		}

		private static async Task<BBPKeyPair> GetEthKeyPairFoundation()
		{
			BBPKeyPair bp = new BBPKeyPair();
			bp.PrivKey = PRIV_ARB_KEY;
			bp.PubKey = PUB_ARB_KEY;
			return bp;
		}
		private static async Task<Web3> GetWeb3Connection(BBPKeyPair ethKeyPair)
		{
			var account = new Nethereum.Web3.Accounts.Account(ethKeyPair.PrivKey, Nethereum.Signer.Chain.Arbitrum);
			var web3 = new Web3(account, ARB_RPC_ADDRESS);
			return web3;
		}
		public static async Task<double> GetEthereumArbBalance(string sAddress)
		{
			Web3 web3 = await GetWeb3Connection(await GetEthKeyPairFoundation());
			var myBalance = await web3.Eth.GetBalance.SendRequestAsync(sAddress);
			var amountInEther = Web3.Convert.FromWei(myBalance.Value);
			return (double)amountInEther;
		}
		private static async Task<Contract> GetERC20Contract(Web3 web, string sSymbol)
		{
			string contractAddress = EthereumProcs.GetContractConfig("ARB", sSymbol);
			string sJson = EthereumProcs.GetContractABI("ARB-ERC20");//The ARB contract erc-20
			var contract = web.Eth.GetContract(sJson, contractAddress);
			return contract;
		}

		private static async Task<Contract> GetNft712Contract(Web3 web, string sContractAddress)
		{
			string sJson = EthereumProcs.GetContractABI("NFT-GENERAL");
			var contract = web.Eth.GetContract(sJson, sContractAddress);
			return contract;
		}

		private static async Task<Contract> GetChainLinkContract(Web3 web3, string sSymbol)
		{
			string contractAddress = EthereumProcs.GetContractConfig("ARB", sSymbol);
			string sJson = EthereumProcs.GetContractABI("CHAINLINK-ORACLE");
			var contract = web3.Eth.GetContract(sJson, contractAddress);
			return contract;
		}

		public static async Task<double> GetNativeEthereumBalance(string sAddress)
		{
			Web3 web3 = await GetWeb3Connection(await GetEthKeyPairFoundation());
			var balance = await web3.Eth.GetBalance.SendRequestAsync(sAddress);
			double etherAmount = (double)Web3.Convert.FromWei(balance.Value);
			return etherAmount;
		}
		public static async Task<double> GetERC20Balance(string sSymbol, string sAddress)
		{
			Web3 web3 = await GetWeb3Connection(await GetEthKeyPairFoundation());
			Contract c = await GetERC20Contract(web3, sSymbol);
			var balanceFunction = c.GetFunction("balanceOf");
			var mybal = await balanceFunction.CallAsync<BigInteger>(sAddress);
			var amt = Web3.Convert.FromWei(mybal);
			return (double)amt;
		}

		public static async Task<string> SendERC20Tokens(string sFromERCAddress, string sERCPrivateKey, string sSymbol, string sToAddress, double nAmount)
		{
			try
			{
				BBPKeyPair b = new BBPKeyPair();
				b.PrivKey = sERCPrivateKey;

				Web3 web3 = await GetWeb3Connection(b);
				var contract = await GetERC20Contract(web3, sSymbol);
				var transferFunction = contract.GetFunction("transfer");
				BigInteger amountToSend = ToWei(nAmount);
				var gasPrice2 = await transferFunction.EstimateGasAsync(sFromERCAddress, null, null, sToAddress, amountToSend);
				var receipt = await transferFunction.SendTransactionAndWaitForReceiptAsync(sFromERCAddress, gasPrice2, null, null, sToAddress, amountToSend) ;
				string stxid = receipt.TransactionHash.ToString();
				return stxid;
			}
			catch(Exception ex)
			{
				return String.Empty;
			}
		}


		public static async Task<string> SendNativeEthereumGas(string sToEtherPubKey)
		{
			try
			{
				double nCurBal = await GetNativeEthereumBalance(sToEtherPubKey);
				double nThresh = .00050;
				if (nCurBal > nThresh*.90)
				{
					return "";
				}
				Web3 web3 = await GetWeb3Connection(await GetEthKeyPairFoundation());
				double nAmount = nThresh;
				BigInteger amountToSend = new BigInteger(nAmount * (1000000000000000000));
				var contract = await GetERC20Contract(web3, "ARB");
				var transferFunction = contract.GetFunction("transfer");
				var gas = await transferFunction.EstimateGasAsync(FOUNDATION_ETHER_ADDRESS, null, null, sToEtherPubKey, amountToSend);
				var transaction = await web3.Eth.GetEtherTransferService()
				.TransferEtherAndWaitForReceiptAsync(sToEtherPubKey, (decimal)nAmount, null, gas);
				string stxid = transaction.TransactionHash.ToString();
				return stxid;
			}
			catch (Exception ex)
			{
				return String.Empty;
			}
		}


		[Function("withdraw", "bool")]
		public class WithdrawFunction : FunctionMessage
		{
			[Parameter("address", "_to", 1)]
			public string To { get; set; }

			[Parameter("uint256", "withdrawAmount", 2)]
			public BigInteger WithdrawAmount { get; set; }
		}

		[Function("MakeDeposit", "uint256")]
		public class DepositFunction : FunctionMessage
		{
			[Parameter("address", "_from", 1)]
			public string From { get; set; }

			[Parameter("uint", "AmountIn", 2)]
			public BigInteger Amount { get; set; }

		}

		[Function("BuyItNow")]
		public class BuyItNowFunction : FunctionMessage
		{
			[Parameter("address", "from", 1)]
			public string From { get; set; }

			[Parameter("address", "to", 2)]
			public string To { get; set; }

			[Parameter("uint256", "tokenId", 3)]
			public BigInteger TokenID { get; set; }

		}


		[Function("setIsMarketable","bool")]
		public class SetIsMarketableFunction : FunctionMessage
		{

			[Parameter("uint256", "tokenId", 1)]
			public BigInteger TokenID { get; set; }

			[Parameter("bool", "isMarketable", 2)]
			public bool isMarketable { get; set; }

		}

		[Function("setIsDeleted", "bool")]
		public class SetIsDeletedFunction : FunctionMessage
		{

			[Parameter("uint256", "tokenId", 1)]
			public BigInteger TokenID { get; set; }

			[Parameter("bool", "isDeleted", 2)]
			public bool isDeleted { get; set; }
		}

		[Function("setIsSoulBound", "bool")]
		public class SetIsSoulBoundFunction : FunctionMessage
		{
			[Parameter("uint256", "tokenId", 1)]
			public BigInteger TokenID { get; set; }

			[Parameter("bool", "isSoulBound", 2)]
			public bool isSoulBound { get; set; }
		}

		[Function("setBuyItNowAmount", "uint256")]
		public class SetBuyItNowAmountFunction : FunctionMessage
		{
			[Parameter("uint256", "tokenId", 1)]
			public BigInteger TokenID { get; set; }

			[Parameter("uint256", "buyItNowAmount", 2)]
			public BigInteger buyItNowAmount { get; set; }
		}



		[Function("approve", "uint256")]
		public class ApproveFunction : FunctionMessage
		{
			[Parameter("address", "to", 1)]
			public string to { get; set; }

			[Parameter("uint256", "tokenId", 2)]
			public BigInteger TokenID { get; set; }

		}



		[Function("mint","bool")]
		public class MintFunction : FunctionMessage
		{
			[Parameter("address", "to", 1)]
			public string to { get; set; }

			[Parameter("uint256", "tokenId", 2)]
			public BigInteger tokenId { get; set; }
		}



		[Function("setTreasureChest", "uint256")]
		public class SetTreasureChestFunction : FunctionMessage
		{
			[Parameter("uint256", "tokenId", 1)]
			public BigInteger TokenID { get; set; }

			[Parameter("uint256", "treasureChestID", 2)]
			public BigInteger TreasureChestID { get; set; }
		}

		public static double FromWei(BigInteger b)
		{
			BigInteger c = BigInteger.Parse("100000000000000000");
			double db = (double)b;
			double dc = (double)c;
			double result = db / dc;
			return result;
		}
		public static BigInteger ToWei(double n)
		{
			double e1 = n * 1000000000000000000;
			BigInteger e2 = (BigInteger)e1;
			return e2;
		}
		public static async Task<RetiredNFT> GetArbitrumNFT(string sContractAddress, BigInteger nTokenID)
		{
			RetiredNFT n = new RetiredNFT();

			try
			{
				Web3 web3 = await GetWeb3Connection(await GetEthKeyPairFoundation());
				var contract = await GetNft712Contract(web3, sContractAddress);
				var IsMarketableFunction = contract.GetFunction("getIsMarketable");
				var BuyItNowFunction = contract.GetFunction("getBuyItNowAmount");
				var IsDeletedFunction = contract.GetFunction("getIsDeleted");
				var IsSoulBoundFunction = contract.GetFunction("getIsSoulBound");
				bool bMarketable = await IsMarketableFunction.CallAsync<bool>(nTokenID);
				n.Marketable = bMarketable.ToInt32();
				BigInteger bin = await BuyItNowFunction.CallAsync<BigInteger>(nTokenID);
				n.BuyItNowAmount = FromWei(bin);
				bool bIsDeleted = await IsDeletedFunction.CallAsync<bool>(nTokenID);
				n.Deleted = bIsDeleted.ToInt32();
				bool bIsSoulBound = await IsSoulBoundFunction.CallAsync<bool>(nTokenID);
				n.SoulBound = bIsSoulBound.ToInt32();
			}
			catch (Exception ex)
			{
				string sFool = "";
			}
			return n;
		}

		public static string GetNFTContractAddress(RetiredNFT n)
		{
			string sContract = EthereumProcs.GetContractConfig("ARB", "NFT-" + n.Type);
			return sContract;
		}
		public static async Task<string> MintNFT(string sBBPPrivKey, RetiredNFT n)
		{
			string sContract = GetNFTContractAddress(n);
			BBPKeyPair k = await Ethereum.DeriveEthereumKey(sBBPPrivKey);
			string txid = await MintNFTInternal(sContract, k.PubKey, n, n.TokenID, k);
			return txid;
		}
		private static async Task<string> MintNFTInternal(string sContractAddress, string sToAddress, 
			RetiredNFT n, string nTokenID, BBPKeyPair kpMinter)
		{
			string sTXID = string.Empty;
			try
			{
				Web3 web3 = await GetWeb3Connection(await GetEthKeyPairFoundation());
				var contract = await GetNft712Contract(web3, sContractAddress);
				BigInteger tin = new BigInteger(nTokenID.ToInt32());

				var mintHandler = web3.Eth.GetContractTransactionHandler<MintFunction>();
				var mObj = new MintFunction()
				{
					to = kpMinter.PubKey,
					tokenId = tin
				};

				mObj.GasPrice = Web3.Convert.ToWei(25, UnitConversion.EthUnit.Gwei);
				var estimate = await mintHandler.EstimateGasAsync(sContractAddress, mObj);
				var transactionReceipt = await mintHandler.SendRequestAndWaitForReceiptAsync(sContractAddress, mObj);
				var transferValidityStatus = transactionReceipt.Status.Value;
				sTXID = transactionReceipt.TransactionHash.ToString();
				await SetApproveNFT(sContractAddress, FOUNDATION_ETHER_ADDRESS, nTokenID.ToInt32(), kpMinter);
				await SetArbitrumNFT(sContractAddress, n, nTokenID.ToInt32(), kpMinter);
				return sTXID;
			}
			catch (Exception ex)
			{
				return "";
			}
		}

		public static async Task<bool> SetApproveNFT(string sContractAddress, string sApproveAddress, int nTokenID, BBPKeyPair bpk)
		{
			try
			{
				// Allows the storefront to manipulate the properties of the NFT
				Web3 web3 = await GetWeb3Connection(bpk);
				BigInteger binTokenID = new BigInteger(nTokenID.ToInt32());
				var binHandler = web3.Eth.GetContractTransactionHandler<ApproveFunction>();
				var binObj = new ApproveFunction()
				{
					TokenID = binTokenID,
					to = sApproveAddress
				};
				var transactionReceipt = await binHandler.SendRequestAndWaitForReceiptAsync(sContractAddress, binObj);
				var transferValidityStatus = transactionReceipt.Status.Value;
				return true;
			}catch(Exception ex)
			{
				return false;
			}
		}

		public static async Task<bool> SetArbitrumNFT(string sContractAddress, RetiredNFT n, int nTokenIDIn, BBPKeyPair EthPair)
		{
			try
			{
				BigInteger nTokenID = new BigInteger(nTokenIDIn.ToInt32());
				RetiredNFT nOld = await GetArbitrumNFT(sContractAddress, nTokenID);
				Web3 web3 = await GetWeb3Connection(EthPair);
				var contract = await GetNft712Contract(web3, sContractAddress);
				if (nOld.BuyItNowAmount != n.BuyItNowAmount)
				{
					BigInteger bin = ToWei(n.BuyItNowAmount);
					var binHandler = web3.Eth.GetContractTransactionHandler<SetBuyItNowAmountFunction>();
					var binObj = new SetBuyItNowAmountFunction()
					{
						TokenID = nTokenID,
						buyItNowAmount = bin
					};
					var transactionReceipt = await binHandler.SendRequestAndWaitForReceiptAsync(sContractAddress, binObj);
					var transferValidityStatus = transactionReceipt.Status.Value;
					if (transferValidityStatus.ToString() != "1") 
						return false;
				}

				// Field 2
				if (nOld.Marketable != n.Marketable)
				{
					var marketableHandler = web3.Eth.GetContractTransactionHandler<SetIsMarketableFunction>();
					var marketableObj = new SetIsMarketableFunction()
					{
						TokenID = nTokenID,
						isMarketable = n.Marketable.IntToBoolean()
					};
					if (nOld.SoulBound==1 && n.Marketable==1)
					{
						// sorry you cannot update marketable on soulbound nft
						return false;
					}
					var f2 = await marketableHandler.SendRequestAndWaitForReceiptAsync(sContractAddress, marketableObj);
					var f3 = f2.Status.Value;
				}
				// Field 3
				if (nOld.Deleted != n.Deleted)
				{
					var deletedHandler = web3.Eth.GetContractTransactionHandler<SetIsDeletedFunction>();
					var deletedObj = new SetIsDeletedFunction()
					{
						TokenID = nTokenID,
						isDeleted = n.Deleted.IntToBoolean()
					};
					var f5 = await deletedHandler.SendRequestAndWaitForReceiptAsync(sContractAddress, deletedObj);
					var f6 = f5.Status.Value;
				}
				// Field 4
				if (nOld.SoulBound != n.SoulBound)
				{
					var soulBoundHandler = web3.Eth.GetContractTransactionHandler<SetIsSoulBoundFunction>();
					var soulBoundObj = new SetIsSoulBoundFunction()
					{
						TokenID = nTokenID,
						isSoulBound = n.SoulBound.IntToBoolean()
					};
					try
					{
						var f8 = await soulBoundHandler.SendRequestAndWaitForReceiptAsync(sContractAddress, soulBoundObj);
						var f9 = f8.Status.Value;
					}
					catch (Exception ex2)
					{
						// we cant set soulbound to 0 if its 1.
					}
				}

				if (nOld.TreasureChest != n.TreasureChest)
				{
					// field 5
					BigInteger tc = BigInteger.Parse(n.TreasureChest);
					var tcHandler = web3.Eth.GetContractTransactionHandler<SetTreasureChestFunction>();
					var tcObj = new SetTreasureChestFunction()
					{
						TokenID = nTokenID,
						TreasureChestID = tc
					};
					var f11 = await tcHandler.SendRequestAndWaitForReceiptAsync(sContractAddress, tcObj);
					var f12 = f11.Status.Value;
				}
			}
			catch (Exception ex)
			{
				string sFool = "";
				return false;
			}
			return true;

		}

		public static async Task<string> DepositARBToNFTContract(BBPKeyPair ercbp, string sContractAddress, double nAmount)
		{
			try
			{
				Web3 web3 = await GetWeb3Connection(await GetEthKeyPairFoundation());
				var contract = await GetNft712Contract(web3, sContractAddress);
				var withdrawFunction = contract.GetFunction("MakeDeposit");
				BigInteger amountToTake = ToWei(nAmount);
				var transferHandler = web3.Eth.GetContractTransactionHandler<DepositFunction>();
				var transfer = new DepositFunction()
				{
					From = ercbp.PubKey,
					Amount = amountToTake
				};
				var transactionReceipt = await transferHandler.SendRequestAndWaitForReceiptAsync(sContractAddress, transfer);
				var transferValidityStatus = transactionReceipt.Status.Value;
				string sStatus = "1";

				return sStatus;
			}
			catch (Exception ex)
			{
				return String.Empty;
			}
		}

		public static async Task<string> BuyNFTNow(string sContractAddress, string from, string To, int nTokenID, 
			RetiredNFTBuy nb, BBPKeyPair kpERC)
		{
			try
			{
				Web3 web3 = await GetWeb3Connection(await GetEthKeyPairFoundation());
				var contract = await GetNft712Contract(web3, sContractAddress);
				var transferHandler = web3.Eth.GetContractTransactionHandler<BuyItNowFunction>();
				BigInteger tin = new BigInteger(nTokenID.ToInt32());
				var transfer = new BuyItNowFunction()
				{
					From = from,
					To = To,
					TokenID = tin
				};
				var transactionReceipt = await transferHandler.SendRequestAndWaitForReceiptAsync(sContractAddress, transfer);
				var transferValidityStatus = transactionReceipt.Status.Value;
				// get the updated nft and save the nft
				BigInteger nTID = new BigInteger(nTokenID.ToInt32());
				RetiredNFT nOldChain = await GetArbitrumNFT(sContractAddress, nTokenID);
				bool f = false;
				await SetApproveNFT(sContractAddress, FOUNDATION_ETHER_ADDRESS, nTokenID.ToInt32(), kpERC);
				return transactionReceipt.TransactionHash.ToString();
			}
			catch (Exception ex)
			{
				return String.Empty;
			}
		}

		public static async Task<string> WithdrawMoneyFromNFTContract(string sContractAddress, double nAmount)
		{
			try
			{
				Web3 web3 = await GetWeb3Connection(await GetEthKeyPairFoundation());
				var contract = await GetNft712Contract(web3, sContractAddress);
				var withdrawFunction = contract.GetFunction("withdraw");
				BigInteger amountToTake = new BigInteger(nAmount * (1000000000000000000));
				var transferHandler = web3.Eth.GetContractTransactionHandler<WithdrawFunction>();
				var transfer = new WithdrawFunction()
				{
					To = FOUNDATION_ETHER_ADDRESS,
					WithdrawAmount = amountToTake
				};
				var transactionReceipt = await transferHandler.SendRequestAndWaitForReceiptAsync(sContractAddress, transfer);
				var transferValidityStatus = transactionReceipt.Status.Value;
				return transferValidityStatus.ToString();
			}
			catch (Exception ex)
			{
				return String.Empty;
			}
		}
	}
}

