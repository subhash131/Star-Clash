using System;
using System.Collections;
using System.Collections.Generic;
using Solana.Unity.Programs;
using Solana.Unity.Rpc;
using Solana.Unity.Rpc.Builders;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Rpc.Types;
using Solana.Unity.SDK;
using Solana.Unity.Wallet;
using Solana.Unity.Wallet.Bip39;
using Solana.Unity.Wallet.Utilities;
using SonicHunt;
using SonicHunt.Accounts;
using SonicHunt.Program;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class SolanaManager : MonoBehaviour{
    public static SolanaManager instance;
    [SerializeField] private TMP_InputField playerNameInputField;
    [SerializeField] private TMP_Text walletAddressText;
    [SerializeField] private TMP_Text walletBalanceText;
    public TMP_Text messageText;
    [SerializeField] private TMP_Text coinsText;
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_InputField coinsInputField;
    [SerializeField] private TMP_InputField withdrawInputField;


    public User userAccount;
    public string playerName;
    // public string message;
    public int playerCoins = 0;
    // public GameObject header;   
    public Master masterAccount;
    private readonly PublicKey masterAddress = new("6jttLttoDAbNgAzThJzcMf3spzPEQ8dJdh4yJ6JYA9dZ");

    public string programId = "9LGanVaCaTSmUFimYEskfRLtvBsUudAadp4tgAc4XLxm"; 

    private void Awake() {
        if (instance == null){
            instance = this;
            DontDestroyOnLoad(gameObject);            
            DontDestroyOnLoad(Web3.Instance.gameObject);
        }
        else Destroy(gameObject);
    }

    private void Start() {
        if (Web3.Instance == null) {
            Debug.Log("Web3.Instance is null. Reinitializing Web3...");
            Web3.Instance.Awake(); 
        } else {
            Debug.Log("Web3.Instance exists.");
        }
    }
    
  
    private void OnEnable(){ 
        Web3.OnBalanceChange += UpdateWalletBalance;
        Web3.OnLogin += OnLogin;
        Web3.OnLogout += OnLogout; 
        // messageText.text = message;
    }

    private void OnDisable(){ 
        Web3.OnBalanceChange -= UpdateWalletBalance;
        Web3.OnLogin -= OnLogin;
        Web3.OnLogout -= OnLogout; 
        // messageText.text = Web3.Account.PublicKey.ToString();   
    }

    private void UpdateWalletBalance(double balance){
        if (walletBalanceText == null) return;
        walletBalanceText.text = $"Wallet Balance: {balance:F2} SOL"; 
    }

      private void OnLogout() {
        walletAddressText.text = "";
        walletBalanceText.text = "";
        MenuManager.instance.OpenMenu("ConnectWalletMenu");       
        userAccount = null;
        playerNameText.text = null;
        coinsText.text = null;
        playerNameInputField.text = null;
        messageText.text = "Logged out!";
        // header.SetActive(false);
    }

    private void OnLogin(Account account){
        try{
            messageText.text = "Logging in...";
            walletBalanceText.text = "Loading...";
            walletAddressText.text = account.PublicKey.ToString()[..4] + "..." + account.PublicKey.ToString()[^4..];
            if (!string.IsNullOrEmpty(account.PublicKey)) {
                GetUserAccount();
                MenuManager.instance.OpenMenu("ContractMenu");
            }
            messageText.text = null;
        }catch(Exception e){
             walletAddressText.text = "Error in OnLogin: " + e.Message;
        }
    }

    public async void GetUserAccount(){
        StartCoroutine(WaitOneSecond());
        if (Web3.Instance == null || Web3.Wallet == null){
            Debug.LogError("Web3 or Wallet not initialized!");
            return;
        }
        try{
            var client = new SonicHuntClient(
                Web3.Wallet.ActiveRpcClient,
                Web3.Wallet.ActiveStreamingRpcClient,
                new PublicKey(programId)
            );
            var result = await client.GetUserAsync(GetUserPDA(), Commitment.Confirmed);

            if (result.WasSuccessful){
                userAccount = result.ParsedResult;
                playerNameText.text = $"Hi {result.ParsedResult.Username}";  
                playerName = result.ParsedResult.Username;
                playerCoins = SolToCoins(LamportsToSol(result.ParsedResult.Funds));
                coinsText.text = $"Coins: {playerCoins}";  
            }else{
                messageText.text = "Failed to fetch user account!";
            }
        }catch(Exception e){
            MenuManager.instance.OpenMenu("RegisterUserMenu");    
            messageText.text = "User not found!";
            Debug.LogError($"Error fetching user account: {e.Message}");
        }
    }

    
    public PublicKey GetUserPDA(){
        var seed = new byte[] { (byte)'u', (byte)'s', (byte)'e', (byte)'r'}; 
        var authorityKey = Web3.Wallet.Account.PublicKey.KeyBytes; 
        bool success = PublicKey.TryFindProgramAddress(
            new[] { seed, authorityKey },
            new PublicKey(programId),
            out PublicKey pda,
            out _
        );
        return success ? pda : null;
    }
   
    public async void RegisterUser(){
        var playerName = playerNameInputField.text;

         if (string.IsNullOrEmpty(playerName)){
            messageText.text = "Username is empty!";
            Debug.LogError("Username is empty!");
            return;
        }

        messageText.text = "Creating user account...";
        if (Web3.Instance == null || Web3.Wallet == null){
            Debug.LogError("Web3 or Wallet not initialized!");
            messageText.text = "Wallet not found!";
            return;
        }

        try{
            PublicKey userPDA = GetUserPDA();
            if (userPDA == null){
                messageText.text = "Failed to generate user PDA!";
                return;
            }
            messageText.text = "Confirm the transaction";
            var accounts = new AddUserAccounts{
                User = userPDA,
                Authority = Web3.Wallet.Account.PublicKey,
                SystemProgram = SystemProgram.ProgramIdKey
            };
            var instruction = SonicHuntProgram.AddUser(accounts, playerName, new PublicKey(programId));

            string latestBlockHash = await Web3.Wallet.GetBlockHash(); 

            byte[] tx = new TransactionBuilder()
                .SetRecentBlockHash(latestBlockHash)
                .SetFeePayer(Web3.Account)
                .AddInstruction(instruction)
                .Build(new List<Account> { Web3.Account });            

            var res = await Web3.Wallet.SignAndSendTransaction(Transaction.Deserialize(tx));
            
            if (res.WasSuccessful){
                Debug.Log($"User account created successfully! Transaction: {res.Result}");
                messageText.text = "User account created successfully!";
                playerNameText.text = $"Hi {playerName}";
                coinsText.text = $"Coins: {playerCoins}";

                GetUserAccount();
           
            }else{
                Debug.LogError($"Failed to create user account: {res.Reason}");
                messageText.text = $"Failed to create account: {res.Reason}";
            }
        }catch(Exception e){
            Debug.LogError($"Error creating user account: {e.Message}");
            messageText.text = $"Error: {e.Message}";
        }
    }

    public async void UpdateResult(){
        if (Web3.Instance == null || Web3.Wallet == null){
            Debug.LogError("Web3 or Wallet not initialized!");
            messageText.text = "Wallet not found!";
            return;
        }

        try{
            
            var mnemonic = new Mnemonic("excite remember smile decade advance dumb present method memory chair talent volcano");
            var seed = mnemonic.DeriveSeed();
            Wallet ownerWallet = new(seed);
            Account ownerAccount = ownerWallet.GetAccount(0); 

            messageText.text = ownerAccount.PublicKey.ToString();

            var accounts = new UpdateResultsAccounts{
                User = GetUserPDA(),
                Master = GetMasterPDA(),
                Authority = ownerAccount.PublicKey,
                SystemProgram = SystemProgram.ProgramIdKey
            };
            var instruction = SonicHuntProgram.UpdateResults(
                accounts, 
                -10000025, //TODO:: Update with actual result
                Web3.Wallet.Account.PublicKey, 
                new PublicKey(programId)
                );

            var rpcClient = ClientFactory.GetClient(Cluster.DevNet);
            var latestBlockHash = await rpcClient.GetLatestBlockHashAsync(); 

            byte[] tx = new TransactionBuilder()
                .SetRecentBlockHash(latestBlockHash.Result.Value.Blockhash)
                .SetFeePayer(ownerAccount)
                .AddInstruction(instruction)
                .Build(new List<Account> { ownerAccount });            

            var res = await rpcClient.SendTransactionAsync(tx);
            if (res.WasSuccessful){
                Debug.Log($"User account updated successfully! Transaction: {res.Result}");
                messageText.text = "Results updated successfully!";
                // GetUserAccount();
            }else{
                Debug.LogError($"Failed to update user account: {res.Reason}");
                messageText.text = $"Failed to update account: {res.Reason}";
            }
        }catch(Exception e){
            Debug.LogError($"Error updating user account: {e.Message}");
            messageText.text = $"Error: {e.Message}";
        }
    }

    public async void SendLamports(){
        try{
            messageText.text = "Sending lamports...";
            var mnemonic = new Mnemonic("excite remember smile decade advance dumb present method memory chair talent volcano");
            var seed = mnemonic.DeriveSeed();
            Wallet ownerWallet = new(seed);
            Account ownerAccount = ownerWallet.GetAccount(0); 
            var rpcClient = ClientFactory.GetClient(Cluster.DevNet);
            var latestBlockHash = await rpcClient.GetLatestBlockHashAsync();

            messageText.text = ownerAccount.PublicKey.ToString();

            var instruction = SystemProgram.Transfer(
                ownerAccount.PublicKey,
                new PublicKey("EhWt1H4gJk88mXtQL6VRG1bLgba1So747Z1P1a16PCC"),
                50000000
            );

            byte[] tx = new TransactionBuilder()
                .SetRecentBlockHash(latestBlockHash.Result.Value.Blockhash)
                .SetFeePayer(ownerAccount)
                .AddInstruction(instruction)
                .Build(ownerAccount);

            var res = await rpcClient.SendTransactionAsync(tx);

            if (res.WasSuccessful){
                Debug.Log($"Lamports sent successfully! Transaction: {res.Result}");
                messageText.text = $"Transaction Successful: {res.Reason}";
            } else{
                Debug.LogError($"Failed to send lamports: {res.Reason}");
                messageText.text = $"Failed to send lamports: {res.Reason}";
            }
        }
        catch (Exception e){
            Debug.LogError($"Error sending lamports: {e.Message}");
            messageText.text = $"Failed to send lamports: {e.Message}";
        }
    }

    public async void Withdraw(){
        var coinInput = withdrawInputField.text;
        if (string.IsNullOrEmpty(coinInput) || !double.TryParse(coinInput, out double coins) || coins <= 0){
            messageText.color = Color.red;
            messageText.text = "Invalid amount!";
            return;
        }
        if (coins > userAccount.Funds){
            messageText.color = Color.red;
            messageText.text = "Insufficient coins!";
            return;
        }

        messageText.text = "Withdrawing coins...";
        if (Web3.Instance == null || Web3.Wallet == null){
            Debug.LogError("Web3 or Wallet not initialized!");
            messageText.text = "Wallet not found!";
            return;
        }

        try{
            PublicKey userPDA = GetUserPDA();
            if (userPDA == null){
                messageText.text = "Failed to generate user PDA!";
                return;
            }

            var mnemonic = new Mnemonic("excite remember smile decade advance dumb present method memory chair talent volcano");

            var seed = mnemonic.DeriveSeed();

            Wallet ownerWallet = new(seed);
            Account ownerAccount = ownerWallet.GetAccount(0); 


            var accounts = new WithdrawFundsAccounts{
                User = userPDA,
                Master = GetMasterPDA(),
                Authority = ownerAccount.PublicKey,
                Withdrawer = Web3.Wallet.Account.PublicKey,
                SystemProgram = SystemProgram.ProgramIdKey
            };

            var lamportsToWithdraw = SolToLamports(CoinsToSol(coins));

            messageText.text = ownerAccount.PublicKey.ToString() ;

            var instruction = SonicHuntProgram.WithdrawFunds(
               accounts,
               lamportsToWithdraw,
               Web3.Wallet.Account.PublicKey,
               new PublicKey(programId)
              );
            string latestBlockHash = await Web3.Wallet.GetBlockHash(); 

            byte[] tx = new TransactionBuilder()
                .SetRecentBlockHash(latestBlockHash)
                .SetFeePayer(Web3.Wallet.Account)
                .AddInstruction(instruction)
                .Build(new List<Account> { Web3.Wallet.Account, ownerAccount });

            var res = await Web3.Wallet.SignAndSendTransaction(Transaction.Deserialize(tx));
            
            if (res.WasSuccessful){
                Debug.Log($"Coins withdrawn successfully! Transaction: {res.Result}");
                messageText.text = "Coins withdrawn successfully!";
                userAccount.Funds -= SolToLamports(CoinsToSol(coins));
                coinsText.text = $"Coins: {SolToCoins(LamportsToSol(userAccount.Funds))}";  
                withdrawInputField.text = null;
            }else{
                Debug.LogError($"Failed to withdraw coins: {res.Reason}");
                messageText.text = $"Failed to withdraw coins: {res.Reason}";
            }

        }catch(Exception e){
            Debug.LogError($"Error withdrawing coins: {e.Message}");
            messageText.text = $"Error withdrawing coins: {e.Message}";
        }
    }


    public async void BuyCoins(){
        var coinInput = coinsInputField.text;

         if (string.IsNullOrEmpty(coinInput) || !double.TryParse(coinInput, out double coins) || coins <= 0){
            messageText.text = "Invalid amount!";
            return;
        }

        messageText.text = "Adding Coins...";
        if (Web3.Instance == null || Web3.Wallet == null){
            Debug.LogError("Web3 or Wallet not initialized!");
            messageText.text = "Wallet not found!";
            return;
        }

        try{
            PublicKey userPDA = GetUserPDA();
            if (userPDA == null){
                messageText.text = "Failed to generate user PDA!";
                return;
            }
          
            messageText.text = "Confirm the transaction";
            var accounts = new AddFundsAccounts{
                User = userPDA,
                Master= GetMasterPDA(),
                Authority = Web3.Wallet.Account.PublicKey,
                Owner = masterAddress,
                SystemProgram = SystemProgram.ProgramIdKey
            };
            var fundAmount = CoinsToSol(coins);

            var instruction = SonicHuntProgram.AddFunds(
                    accounts, 
                    SolToLamports((decimal)fundAmount), 
                    new PublicKey(programId)
                    );

            string latestBlockHash = await Web3.Wallet.GetBlockHash(); 

            byte[] tx = new TransactionBuilder()
                .SetRecentBlockHash(latestBlockHash)
                .SetFeePayer(Web3.Account)
                .AddInstruction(instruction)
                .Build(new List<Account> { Web3.Account });            

            var res = await Web3.Wallet.SignAndSendTransaction(Transaction.Deserialize(tx));
            
            if (res.WasSuccessful){
                Debug.Log($"Coins bought successfully! Transaction: {res.Result}");
                messageText.text = "Coins added successfully!";
                userAccount.Funds += SolToLamports(fundAmount);
                coinsText.text = $"Coins: {SolToCoins(LamportsToSol(userAccount.Funds))}";  
                coinsInputField.text = null;
            }else{
                Debug.LogError($"Failed to buy coins: {res.Reason}");
                messageText.text = $"Failed to buy coins: {res.Reason}";
            }
        }catch(Exception e){
            Debug.LogError($"Error buying coins: {e.Message}");
            messageText.text = $"Error buying coins: {e.Message}";
        }
    }


      public async void GetMaster(){
        try{
            var client = new SonicHuntClient(
                Web3.Wallet.ActiveRpcClient,
                Web3.Wallet.ActiveStreamingRpcClient,
                new PublicKey(programId)
            );
            var result = await client.GetMasterAsync(GetMasterPDA(), Commitment.Confirmed);

            if (result.WasSuccessful && result.WasSuccessful){
                masterAccount = result.ParsedResult;
                Debug.Log($"Master Account Fetched! Owner: {masterAccount.Owner}");
            }
            else{
                Debug.LogError($"Failed to fetch Master account: {result.OriginalRequest}");
            }
        }
        catch (Exception ex){
            Debug.LogError($"Error fetching Master account: {ex.Message}");
        }
    }

    private PublicKey GetMasterPDA(){
        var seed = new byte[] { (byte)'m', (byte)'a', (byte)'s', (byte)'t', (byte)'e', (byte)'r' }; 
        bool success = PublicKey.TryFindProgramAddress(
            new[] { seed },
            new PublicKey(programId),
            out PublicKey pda,
            out _
        );
        return success ? pda : null;
    }

    public ulong SolToLamports(decimal sol) {
        return (ulong)(sol * 1_000_000_000m);
    }

    public decimal LamportsToSol(ulong lamports) {
        return lamports / 1_000_000_000m;
    }
    
    public int SolToCoins(decimal sol) {
        return (int)(sol * 100);
    }

    public decimal CoinsToSol(double coins){
        return (decimal)coins / 100m;
    }


    IEnumerator WaitOneSecond(){
        Debug.Log("Waiting...");
        yield return new WaitForSeconds(2f);  // Wait for 1 second
        Debug.Log("Done waiting!");
    }
}
