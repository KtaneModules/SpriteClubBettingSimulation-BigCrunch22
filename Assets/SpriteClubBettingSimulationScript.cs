using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

public class SpriteClubBettingSimulationScript : MonoBehaviour
{
	public KMBombInfo Bomb;
    public KMAudio Audio;
    public KMBombModule Module;
	
	public AudioClip[] SFX;
	public KMSelectable[] NumberButtons, SelectableButtons;
	public KMSelectable ResetButton, SolveButton;
	public MeshRenderer[] TwoButtons;
	public SpriteRenderer[] TwoButtonImages;
	public TextMesh Introdcution, Username, MoneyCount, ConnectionStatus, MatchIDTag, BetAmount;
	public TextAsset NameList;
	public GameObject TheAccount;

	//Logging
    static int moduleIdCounter = 1;
    int moduleId;
    private bool ModuleSolved;
	
	string MatchID = "", Comparer = "", Comparing = "", RedPot = "—", BluePot = "—", YouBet = "", BetAmountString = "0", Balance = "500",
	BluePlayer = "", RedPlayer = "";
	bool AbleToInput = false, Online = true, InspectingLoop = true, BetTransitionColor = false, AbleToTransition = false;
	bool IDInspection = false, BattleInspection = false;
	
	void Awake()
	{
		moduleId = moduleIdCounter++;
		for (int i = 0; i < NumberButtons.Length; i++)
		{
			int Press = i;
			NumberButtons[i].OnInteract += delegate ()
			{
				ButtonPress(Press);
				return false;
			};
		}
		for (int i = 0; i < SelectableButtons.Length; i++)
		{
			int Press = i;
			SelectableButtons[i].OnInteract += delegate ()
			{
				SelectablePress(Press);
				return false;
			};
		}
		ResetButton.OnInteract += delegate(){ResetBet(); return false;};
		SolveButton.OnInteract += delegate(){SolveTheModule(); return false;};
	}
	
	void ResetBet()
	{
		ResetButton.AddInteractionPunch(0.2f);
		if (AbleToInput && !BetTransitionColor)
		{
			if (YouBet != "")
			{
				BetTransitionColor = true;
				YouBet = "";
				StartCoroutine(ColorChangeText(BetAmount, BetAmount.color, new Color(255f/255f, 255f/255f, 255f/255f, 255f/255f)));
			}
			BetAmount.text = "$0";
			BetAmountString = "";
			YouBet = "";
		}
	}
	
	void SolveTheModule()
	{
		SolveButton.AddInteractionPunch(0.2f);
		if (!ModuleSolved)
		{
			Module.HandlePass();
			ModuleSolved = true;
		}
	}
	
	void SelectablePress(int Press)
	{
		SelectableButtons[Press].AddInteractionPunch(0.2f);
		if (AbleToInput && !BetTransitionColor)
		{
			if (Press == 0 && YouBet != "Blue" && BetAmountString != "0")
			{
				if (long.Parse(BetAmountString) <= long.Parse(Balance))
				{
					BetTransitionColor = true;
					YouBet = "Blue";
					StartCoroutine(ColorChangeText(BetAmount, BetAmount.color, new Color(34f/255f, 119f/255f, 255f/255f, 255f/255f)));
				}	
			}
			
			else if (Press == 1 && YouBet != "Red" && BetAmountString != "0")
			{
				if (long.Parse(BetAmountString) <= long.Parse(Balance))
				{
					BetTransitionColor = true;
					YouBet = "Red";
					StartCoroutine(ColorChangeText(BetAmount, BetAmount.color, new Color(221f/255f, 51f/255f, 51f/255f, 255f/255f)));
				}	
			}
		}
	}
	
	void ButtonPress(int Press)
	{
		NumberButtons[Press].AddInteractionPunch(0.2f);
		if (AbleToInput && !BetTransitionColor)
		{
			if (BetAmount.text.Length < 13 && YouBet == "")
			{
				if (BetAmount.text == "$0")
				{
					BetAmount.text = "$" + Press.ToString();
					BetAmountString = Press.ToString();
				}
			
				else
				{
					BetAmount.text += Press.ToString();
					BetAmountString += Press.ToString();
				}
			}
		}
	}
	
	void Start()
	{
		TheAccount.SetActive(false);
		Module.OnActivate += StartUp;
	}
	
	void StartUp()
	{
		StartCoroutine(GetText());
	}
	
	
	IEnumerator GetText()
	{
		string Generation = "GENERATING A\nNEW ACCOUNT";
		for (int b = 0; b < Generation.Length; b++)
		{
			Introdcution.text += Generation[b].ToString();
			yield return new WaitForSecondsRealtime(0.05f);
		}
		
		StartCoroutine(GatherLatestID());
		StartCoroutine(GatherLatestBattle());
		
		while (!BattleInspection && !IDInspection)
		{
			yield return null;
		}
		
		string[] AllUsernames = JsonConvert.DeserializeObject<string[]>(NameList.text);
        Username.text += AllUsernames[UnityEngine.Random.Range(0, AllUsernames.Length)];
		Introdcution.text = "";
		TheAccount.SetActive(true);
		StartCoroutine(InspectInfinitelyID());
		StartCoroutine(InspectInfinitelyBattle());
	}
	
	IEnumerator GatherLatestID()
	{
		UnityWebRequest www = UnityWebRequest.Get("https://mugen.spriteclub.tv/matches");
        yield return www.SendWebRequest();
 
        if (www.isNetworkError || www.isHttpError)
		{	
            ConnectionStatus.text = "Status: Offline";
			MatchIDTag.text = "??????";
			Online = false;
        }
		
        else
		{
			List<string> Test = new List<string>(www.downloadHandler.text.Split('\n'));
			Test.Reverse();
			for (int x = 0; x < Test.Count(); x++)
			{
				if (Regex.IsMatch(Test[x], "<div class='stat-elem'>"))
				{
					string Care = Regex.Replace(Test[x], "[<>]", "ξ");
					Care = Regex.Replace(Care, "ξξ", "ξ");
					List<string> Test2 = new List<string>(Care.Split('ξ'));
					for (int a = 0; a < Test2.Count(); a++)
					{
						if (!Regex.IsMatch(Test2[a], "[a-z]") && Regex.IsMatch(Test2[a], "[0-9]"))
						{
							MatchID = Test2[a];
							goto Continuation;
						}
					}
				}
			}
			
			Continuation:
			ConnectionStatus.text = "Status: Online";
			MatchIDTag.text = "Initializing";
			Comparer = (Int32.Parse(MatchID) + 1).ToString();
			Online = true;
        }
		
		IDInspection = true;
	}
	
	IEnumerator GatherLatestBattle()
	{
		UnityWebRequest www = UnityWebRequest.Get("https://mugen.spriteclub.tv");
        yield return www.SendWebRequest();
 
        if (www.isNetworkError || www.isHttpError)
		{	
            ConnectionStatus.text = "Status: Offline";
			MatchIDTag.text = "??????";
			Online = false;
        }
		
        else
		{
			List<string> Test = new List<string>(www.downloadHandler.text.Split('\n'));
			int Count2 = 0;
			for (int x = 0; x < Test.Count(); x++)
			{
				if (Regex.IsMatch(Test[x].ToUpper(), "BLUEPLAYER"))
				{
					string Care = Regex.Replace(Test[x], "</td>", "");
					Care = Regex.Replace(Care, ">", "ξ");
					string[] Split = Care.Split('ξ');
					BluePlayer = Split[1];
					Count2++;
				}
				
				if (Regex.IsMatch(Test[x].ToUpper(), "REDPLAYER"))
				{
					string Care = Regex.Replace(Test[x], "</td>", "");
					Care = Regex.Replace(Care, ">", "ξ");
					string[] Split = Care.Split('ξ');
					RedPlayer = Split[1];
					Count2++;
				}
				
				if (Count2 == 2)
				{
					break;
				}
			}
		}
		
		BattleInspection = true;
	}
	
	IEnumerator InspectInfinitelyID()
	{
		while (true)
		{
			if (InspectingLoop)
			{
				UnityWebRequest www = UnityWebRequest.Get("https://mugen.spriteclub.tv/matches");
				yield return www.SendWebRequest();
				if(www.isNetworkError || www.isHttpError)
				{
					ConnectionStatus.text = "Status: Offline";
					MatchIDTag.text = "??????";
					Online = false;
				}
				
				else
				{
					List<string> Test = new List<string>(www.downloadHandler.text.Split('\n'));
					Test.Reverse();
					for (int x = 0; x < Test.Count(); x++)
					{
						if (Regex.IsMatch(Test[x], "<div class='stat-elem'>"))
						{
							string Care = Regex.Replace(Test[x], "[<>]", "ξ");
							Care = Regex.Replace(Care, "ξξ", "ξ");
							List<string> Test2 = new List<string>(Care.Split('ξ'));
							for (int a = 0; a < Test2.Count(); a++)
							{
								if (!Regex.IsMatch(Test2[a], "[a-z]") && Regex.IsMatch(Test2[a], "[0-9]"))
								{
									MatchID = Test2[a];
									goto Continuation;
								}
							}
						}
					}
					
					Continuation:
					ConnectionStatus.text = "Status: Online";
					Comparing = MatchID;
					Online = true;
					
					if (long.Parse(Comparing) >= long.Parse(Comparer))
					{
						StartCoroutine(GatherResult());
						InspectingLoop = false;
					}
				}
			}
			
			else
			{
				yield return null;
			}
		}
	}
	
	IEnumerator InspectInfinitelyBattle()
	{
		while (true)
		{
			string RedTeam = "", BlueTeam = "";
			if (InspectingLoop)
			{
				UnityWebRequest www = UnityWebRequest.Get("https://mugen.spriteclub.tv");
				yield return www.SendWebRequest();
				if(www.isNetworkError || www.isHttpError)
				{
					ConnectionStatus.text = "Status: Offline";
					MatchIDTag.text = "??????";
					Online = false;
				}
				
				else
				{
					List<string> Test = new List<string>(www.downloadHandler.text.Split('\n'));
					int Count2 = 0;
					for (int x = 0; x < Test.Count(); x++)
					{
						if (Regex.IsMatch(Test[x].ToUpper(), "BLUEPLAYER"))
						{
							string Care = Regex.Replace(Test[x], "</td>", "");
							Care = Regex.Replace(Care, ">", "ξ");
							string[] Split = Care.Split('ξ');
							BlueTeam = Split[1];
							Count2++;
						}
						
						if (Regex.IsMatch(Test[x].ToUpper(), "REDPLAYER"))
						{
							string Care = Regex.Replace(Test[x], "</td>", "");
							Care = Regex.Replace(Care, ">", "ξ");
							string[] Split = Care.Split('ξ');
							BluePlayer = Split[1];
							Count2++;
						}
						
						if (Count2 == 2)
						{
							break;
						}
					}
					
					if (BlueTeam != BluePlayer || RedTeam != RedPlayer)
					{
						BluePlayer = BlueTeam;
						RedPlayer = RedTeam;
						AbleToTransition = true;
					}
				}
			}
			
			else
			{
				yield return null;
			}
			
			
		}
	}
	
	IEnumerator GatherResult()
	{
		string TheWinner = "";
		int Iteration = 0;
		for (int x = 0; x < 50; x++)
		{
			Iteration++;
			string Request2 = "https://mugen.spriteclub.tv/match?id=" + Comparing;
			UnityWebRequest www3 = UnityWebRequest.Get(Request2);
			yield return www3.SendWebRequest();
			
			if(www3.isNetworkError || www3.isHttpError)
			{
				ConnectionStatus.text = "Status: Offline";
				MatchIDTag.text = "??????";
				Online = false;
			}
			
			else
			{
				Online = true;
				List<string> Test2 = new List<string>(www3.downloadHandler.text.Split('\n'));
				Test2.Reverse();
				int Count2 = 0;
				for (int b = 0; b < Test2.Count(); b++)
				{
					if (Regex.IsMatch(Test2[b].ToUpper(), "RED POT"))
					{
						string Care = Regex.Replace(Test2[b], "</span>", "");
						string[] Split = Care.Split(' ');
						RedPot = Split[2];
						RedPot = Regex.Replace(RedPot, "[$,]", "");
						Count2++;
					}
					
					else if (Regex.IsMatch(Test2[b].ToUpper(), "BLUE POT"))
					{
						string Care = Regex.Replace(Test2[b], "</span>", "");
						string[] Split = Care.Split(' ');
						BluePot = Split[2];
						BluePot = Regex.Replace(BluePot, "[$,]", "");
						Count2++;
					}
					
					else if (Regex.IsMatch(Test2[b].ToUpper(), "WINNER"))
					{
						string Care = Regex.Replace(Test2[b], ": ", "ξ");
						Care = Regex.Replace(Care, "\">", "ξ");
						string[] Split = Care.Split('ξ');
						TheWinner = Split[1];
						Count2++;
					}
						
					if (Count2 == 3)
					{
						break;
					}
				}	
			}
			
			int Out, Out2;
			if (Int32.TryParse(BluePot, out Out) && Int32.TryParse(RedPot, out Out2))
			{
				goto IterationLevel;
			}
		}
		
		IterationLevel:
		if (Iteration < 50)
		{
			if ((TheWinner != "#2277FF" && TheWinner != "#DD3333") || BetAmountString == "0" || YouBet == "")
			{
				StartCoroutine(ColorChangeText(BetAmount, BetAmount.color, new Color(128f/255f, 128f/255f, 128f/255f, 255f/255f)));
				MatchIDTag.text = "Result";
				BetAmount.text = "$" + BetAmountString + " -> $" + BetAmountString;
				yield return new WaitForSecondsRealtime(5f);
				while (!AbleToTransition)
				{
					yield return null;
				}
				AbleToTransition = false;
				GeneralWork();
			}
			
			else
			{
				bool Correct = false;
				decimal Gain = 0;
				if ((TheWinner == "#2277FF" && YouBet == "Blue") || (TheWinner == "#DD3333" && YouBet == "Red"))
				{
					Correct = true;
					StartCoroutine(ColorChangeText(BetAmount, BetAmount.color, Color.green));
					MatchIDTag.text = "Result";
					
					Gain = YouBet == "Blue" ? decimal.Parse(BetAmountString) * (decimal.Parse(RedPot) / decimal.Parse(BluePot)) : decimal.Parse(BetAmountString) * (decimal.Parse(BluePot) / decimal.Parse(RedPot));
					Gain = Math.Round(Gain, 0, MidpointRounding.AwayFromZero);
					if (Gain <= 0)
					{
						Gain = 1;
					}
					BetAmount.text = "$" + BetAmountString + " -> $" + Gain.ToString();
				}
				
				else
				{
					StartCoroutine(ColorChangeText(BetAmount, BetAmount.color, Color.red));
					MatchIDTag.text = "Result";
					BetAmount.text = "$" + BetAmountString + " -> $0";
				}
				
				yield return new WaitForSecondsRealtime(5f);
				while (!AbleToTransition)
				{
					yield return null;
				}
				
				if (Correct)
				{
					Balance = (decimal.Parse(Balance) + Gain).ToString();
				}
				
				else
				{
					Balance = (decimal.Parse(Balance) - decimal.Parse(BetAmountString)).ToString();
					if (decimal.Parse(Balance) < 500)
					{
						Balance = "500";
					}
				}
				
				MoneyCount.text = "Balance: $" + Balance.ToString();
				
				AbleToTransition = false;
				GeneralWork();
			}
		}
		
		else
		{
			MatchIDTag.text = "Result";
			BetAmount.text = "ERROR";
			StartCoroutine(ColorChangeText(BetAmount, BetAmount.color, new Color(128f/255f, 128f/255f, 128f/255f, 255f/255f)));
			yield return new WaitForSecondsRealtime(5f);
			while (!AbleToTransition)
			{
				yield return null;
			}
			AbleToTransition = false;
			GeneralWork();
		}
		
		
	}
	
	void GeneralWork()
	{
		BetAmount.text = "$0";
		YouBet = "";
		BetAmountString = "0";
		Audio.PlaySoundAtTransform(SFX[0].name, transform);
		MatchIDTag.text = "Input Pending";
		Comparer = (Int32.Parse(MatchID) + 1).ToString();
		BetAmount.color = new Color(255/255f, 255/255f, 255/255f, 0f/255f);
		StartCoroutine(ColorChangeRenderer(TwoButtons[0], TwoButtons[0].material.color, new Color(34f/255f, 119f/255f, 255f/255f)));
		StartCoroutine(ColorChangeRenderer(TwoButtons[1], TwoButtons[1].material.color, new Color(221f/255f, 51f/255f, 51f/255f)));
		StartCoroutine(ColorChangeSprite(TwoButtonImages[0], TwoButtons[1].material.color, new Color(255f/255f, 255f/255f, 255f/255f, 255f/255f)));
		StartCoroutine(ColorChangeSprite(TwoButtonImages[1], TwoButtons[1].material.color, new Color(255f/255f, 255f/255f, 255f/255f, 255f/255f)));
		StartCoroutine(ColorChangeText(BetAmount, BetAmount.color, new Color(255f/255f, 255f/255f, 255f/255f, 255f/255f)));
		AbleToInput = true;
		StartCoroutine(Timer());
	}
	
	IEnumerator Timer()
    {		
		int Timer = 30;
		while (Timer != 0)
		{
			Timer--;
			yield return new WaitForSecondsRealtime(1f);
		}
		
		if (Timer == 0)
		{
			AbleToInput = false;
			InspectingLoop = true;
			MatchIDTag.text = "Battle Result Pending";
			StartCoroutine(ColorChangeRenderer(TwoButtons[0], TwoButtons[0].material.color, new Color(128f/255f, 128f/255f, 128f/255f)));
			StartCoroutine(ColorChangeRenderer(TwoButtons[1], TwoButtons[1].material.color, new Color(128f/255f, 128f/255f, 128f/255f)));
			StartCoroutine(ColorChangeSprite(TwoButtonImages[0], TwoButtons[1].material.color, new Color(255f/255f, 255f/255f, 255f/255f, 0f/255f)));
			StartCoroutine(ColorChangeSprite(TwoButtonImages[1], TwoButtons[1].material.color, new Color(255f/255f, 255f/255f, 255f/255f, 0f/255f)));
			StartCoroutine(ColorChangeText(BetAmount, BetAmount.color, new Color(255f/255f, 255f/255f, 255f/255f, 0f/255f)));
		}
	}
	
	IEnumerator ColorChangeText(TextMesh display, Color startColor, Color endColor)
    {
		var elapsed = 0f;
        var duration = 1f;
        while (elapsed < duration)
        {
            display.color = Color.Lerp(startColor, endColor, elapsed / duration);
            yield return null;
            elapsed += Time.deltaTime;
        }
        display.color = endColor;
		BetTransitionColor = false;
	}
	
	IEnumerator ColorChangeSprite(SpriteRenderer display, Color startColor, Color endColor)
    {
		var elapsed = 0f;
        var duration = 1f;
        while (elapsed < duration)
        {
            display.material.color = Color.Lerp(startColor, endColor, elapsed / duration);
            yield return null;
            elapsed += Time.deltaTime;
        }
        display.material.color = endColor;
	}
	
	IEnumerator ColorChangeRenderer(MeshRenderer display, Color startColor, Color endColor)
    {
        var elapsed = 0f;
        var duration = 1f;
        while (elapsed < duration)
        {
            display.material.color = Color.Lerp(startColor, endColor, elapsed / duration);
            yield return null;
            elapsed += Time.deltaTime;
        }
        display.material.color = endColor;
    }

	//twitch plays
	#pragma warning disable 414
	private readonly string TwitchHelpMessage = @"!{0} press <solve/reset> [Presses the ""Solve"" or ""Reset"" button] | !{0} bet <amt> [Type the specified bet amount] | !{0} team <red/blue> [Bets on the specified team]";
	#pragma warning restore 414
	IEnumerator ProcessTwitchCommand(string command)
	{
		string[] parameters = command.Split(' ');
		if (Regex.IsMatch(parameters[0], @"^\s*press\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
		{
			yield return null;
			if (parameters.Length > 2)
			{
				yield return "sendtochaterror Too many parameters!";
			}
			else if (parameters.Length == 2)
			{
				if (parameters[1].EqualsIgnoreCase("solve"))
				{
					if (!TheAccount.activeSelf)
						yield return "sendtochaterror The module must finish startup first!";
					else
						SolveButton.OnInteract();
				}
				else if (parameters[1].EqualsIgnoreCase("reset"))
                {
					if (!AbleToInput)
						yield return "sendtochaterror The module is not in \"Input Pending\" mode!";
					else
						ResetButton.OnInteract();
                }
                else
                {
					yield return "sendtochaterror!f The specified button '" + parameters[1] + "' is invalid!";
				}
			}
			else if (parameters.Length == 1)
			{
				yield return "sendtochaterror Please specify a button to press!";
			}
			yield break;
		}
		if (Regex.IsMatch(parameters[0], @"^\s*bet\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
		{
			yield return null;
			if (parameters.Length > 2)
			{
				yield return "sendtochaterror Too many parameters!";
			}
			else if (parameters.Length == 2)
			{
				string bet = parameters[1];
				if (parameters[1].StartsWith("$") && parameters[1].Count(x => x == '$') == 1)
					bet = parameters[1].Replace("$", "");
				char[] nums = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
				for (int i = 0; i < bet.Length; i++)
                {
					if (!nums.Contains(bet[i]))
                    {
						yield return "sendtochaterror!f The specified bet amount '" + parameters[1] + "' is invalid!";
						yield break;
                    }
                }
				if (!AbleToInput)
					yield return "sendtochaterror The module is not in \"Input Pending\" mode!";
                else
                {
					for (int i = 0; i < bet.Length; i++)
					{
						NumberButtons[int.Parse(bet[i].ToString())].OnInteract();
						yield return new WaitForSecondsRealtime(.1f);
					}
				}
			}
			else if (parameters.Length == 1)
			{
				yield return "sendtochaterror Please specify a bet amount to type!";
			}
			yield break;
		}
		if (Regex.IsMatch(parameters[0], @"^\s*team\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
		{
			yield return null;
			if (parameters.Length > 2)
			{
				yield return "sendtochaterror Too many parameters!";
			}
			else if (parameters.Length == 2)
			{
				if (!parameters[1].ToLowerInvariant().EqualsAny("red", "blue"))
					yield return "sendtochaterror!f The specified team '" + parameters[1] + "' is invalid!";
				else
				{
					if (!AbleToInput)
						yield return "sendtochaterror The module is not in \"Input Pending\" mode!";
					else
						SelectableButtons[parameters[1].ToLowerInvariant().Equals("blue") ? 0 : 1].OnInteract();
				}
			}
			else if (parameters.Length == 1)
			{
				yield return "sendtochaterror Please specify a team to bet on!";
			}
		}
	}

	IEnumerator TwitchHandleForcedSolve()
    {
		while (!TheAccount.activeSelf) yield return true;
		SolveButton.OnInteract();
		yield return new WaitForSeconds(0.1f);
	}
}