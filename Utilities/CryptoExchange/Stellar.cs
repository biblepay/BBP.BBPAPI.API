using BMSCommon;
using stellar_dotnet_sdk;
using stellar_dotnet_sdk.responses;
using Network = stellar_dotnet_sdk.Network;

namespace BBP.CORE.API.Utilities
{
    public static class Stellar
	{
		public static async Task<double> GetAccountBalance(string sAccountID)
		{
			try
			{
				double nTotal = 0;
				Network network = new Network("Public Global Stellar Network ; September 2015");
				Server server = new Server("https://horizon.stellar.org");
				KeyPair keypair = KeyPair.FromAccountId(sAccountID);
				AccountResponse accountResponse = await server.Accounts.Account(keypair.AccountId);
				Balance[] balances = accountResponse.Balances;
				for (int i = 0; i < balances.Length; i++)
				{
					Balance asset = balances[i];
					nTotal += asset.BalanceString.ToDouble();
				}
				return nTotal;
			}
			catch(Exception ex)
			{
				return 0;
			}
		}

		public static stellar_dotnet_sdk.KeyPair DeriveStellarKeypair(string bbpPrivKey)
		{
			string s1 = HexadecimalEncoding.StringToHex(bbpPrivKey);
			byte[] b1 = HexadecimalEncoding.StringToByteArrayFastest(s1.Substring(0, 64));
			stellar_dotnet_sdk.KeyPair stellarKeyPair = stellar_dotnet_sdk.KeyPair.FromSecretSeed(b1);
			return stellarKeyPair;
		}

		public static async Task<string> SendMoney(string sPrivKey, string sToAccount, double nAmount)
		{
			nAmount = Math.Round(nAmount, 3);
			double nToBal = await GetAccountBalance(sToAccount);
			stellar_dotnet_sdk.KeyPair sourceKeypair = stellar_dotnet_sdk.KeyPair.FromSecretSeed(sPrivKey);
			stellar_dotnet_sdk.Network network = new stellar_dotnet_sdk.Network("Public Global Stellar Network ; September 2015");
			Server server = new Server("https://horizon.stellar.org");
			stellar_dotnet_sdk.KeyPair destinationKeyPair = stellar_dotnet_sdk.KeyPair.FromAccountId(sToAccount);
			AccountResponse sourceAccountResponse = await server.Accounts.Account(sourceKeypair.AccountId);
			stellar_dotnet_sdk.Account sourceAccount = new Account(sourceKeypair.AccountId, sourceAccountResponse.SequenceNumber);
			Asset asset = new AssetTypeNative();
			// if source account is null
			if (sourceAccount == null)
			{
				throw new Exception("no source account");
			}
			if (nToBal == 0 && nAmount >= 1)
			{
				// create the account
				CreateAccountOperation cao = new CreateAccountOperation(destinationKeyPair,  "1");
				cao.SourceAccount = sourceKeypair;
				bool f1 = false;
				Transaction trans0 = new TransactionBuilder(sourceAccount).AddOperation(cao).Build();
				trans0.Sign(sourceKeypair, network);
				var res0 = await server.SubmitTransaction(trans0);
				if (res0.Hash == null || res0.Hash.Length < 10)
				{
					throw new Exception("Unable to fund genesis tx.");
				}
			}

			PaymentOperation operation = new PaymentOperation.Builder(destinationKeyPair, asset, nAmount.ToString()).SetSourceAccount(sourceAccount.KeyPair).Build();
			Transaction transaction = new TransactionBuilder(sourceAccount).AddOperation(operation).SetFee(32800).Build();
			//Sign Transaction
			transaction.Sign(sourceKeypair, network);
			//Try to send the transaction
			try
			{
				Console.WriteLine("Sending Transaction");
				var s0 = await server.SubmitTransaction(transaction);
				Console.WriteLine("Success!");
				return s0.Hash.ToString() ?? String.Empty;
			}
			catch (Exception exception)
			{
				Console.WriteLine("Send Transaction Failed");
				Console.WriteLine("Exception: " + exception.Message);
				return "";
			}
		}
	}
}
