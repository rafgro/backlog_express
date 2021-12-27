using Godot;
using System.IO;
using System.Collections.Generic;
using System;
using System.Linq;

public class Feature
{
	public string Module;
	public string Content;
	public int Comparisons;

	public string ToCSV()
	{
		return Module+";"+Content+";"+Convert.ToString(Comparisons);
	}
}

public enum States { MAIN, ADD, COMPARE, BENEFIT, MODULE };

public class Main : Control
{
	public static Random seed;
	private List<Feature> Features = new List<Feature>();
	private States CurrentState = States.MAIN;  // used to interpret keyboard input
	private int Compared1 = 0;  // pos in features
	private int Compared2 = 0;  // -||-
	private string CurrentModule = null;

	public override void _Ready()
	{
		seed = new System.Random();
		LoadCSV(@"C:/Users/Rafał Grochala/Desktop/espio/list.csv");
		DisplayDescription();
	}

	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Naive loader, doesn't work with " or ; inside columns
	public void LoadCSV(string path)
	{
		using(var reader = new StreamReader(path))
		{
			int row = -1;
			while (!reader.EndOfStream)
			{
				row += 1;
				var line = reader.ReadLine();
				if (row == 0)
					continue;
				var values = line.Split(';');
				Features.Add(new Feature { Module = values[0], Content = values[1], Comparisons = Convert.ToInt32(values[2]) });
			}
		}
	}
	public void Save()
	{
		System.IO.File.Move(@"C:/Users/Rafał Grochala/Desktop/espio/list.csv", @"C:/Users/Rafał Grochala/Desktop/espio/list_backup_" + Convert.ToString((int)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds) + ".csv");
		using(var w = new StreamWriter(@"C:/Users/Rafał Grochala/Desktop/espio/list.csv"))
		{
			w.WriteLine("Module;Feature;Comparisons");
			w.Flush();
			foreach(var f in Features)
			{
				w.WriteLine(string.Format("{0};{1};{2}", f.Module, f.Content, Convert.ToString(f.Comparisons)));
				w.Flush();
			}
		}
		var src = DateTime.Now;
		GetNode<Button>("P/M/R/Buttons/B4").Text = "Saved at " + Convert.ToString(src.Hour) + ":" + Convert.ToString(src.Minute);
	}

	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// UI control
	public void DisplayDescription()
	{
		CurrentState = States.MAIN;
		int noOfFeatures = Features.Count;
		int noOfComparisons = 0;
		float avgOfComparisons = 0f;

		foreach (var f in Features)
		{
			noOfComparisons += f.Comparisons;
			avgOfComparisons += f.Comparisons;
		}
		avgOfComparisons /= Features.Count*1.0f;

		for (int i = 0; i < 10; i++)
			GetNode<HBoxContainer>("P/M/R/TheList/List/Item" + Convert.ToString(i)).Visible = false;
		for (int i = 0; i < Math.Min(10, Features.Count); i++)
		{
			GetNode<Label>("P/M/R/TheList/List/Item" + Convert.ToString(i) + "/Label").Text = Convert.ToString(i+1) + ". " + Features[i].Content;
			GetNode<HBoxContainer>("P/M/R/TheList/List/Item" + Convert.ToString(i)).Visible = true;
		}

		GetNode<Label>("P/M/R/TheList/Description").Text = Convert.ToString(noOfFeatures) + " ideas\n" + Convert.ToString(noOfComparisons) + " comparisons\n" + Convert.ToString(Math.Round(avgOfComparisons, 1)) + " average of comparisons\n\ncurrent top10:";

		GetNode<VBoxContainer>("P/M/R/Add").Visible = false;
		GetNode<VBoxContainer>("P/M/R/Compare").Visible = false;
		GetNode<VBoxContainer>("P/M/R/Module").Visible = false;
		GetNode<VBoxContainer>("P/M/R/TheList").Visible = true;
	}
	public void DisplayAdd()
	{
		CurrentState = States.ADD;
		GetNode<LineEdit>("P/M/R/Add/Module").Text = "Module";
		GetNode<Label>("P/M/R/Add/KnownModules").Text = "Known Modules: " + ReturnKnownModules();
		GetNode<LineEdit>("P/M/R/Add/Feature").Text = "Feature";
		GetNode<Label>("P/M/R/Add/Message").Text = "";

		GetNode<VBoxContainer>("P/M/R/Compare").Visible = false;
		GetNode<VBoxContainer>("P/M/R/TheList").Visible = false;
		GetNode<VBoxContainer>("P/M/R/Module").Visible = false;
		GetNode<VBoxContainer>("P/M/R/Add").Visible = true;
	}
	public void DisplayCompare()
	{
		// always proposes new comparison on call
		CurrentState = States.COMPARE;

		// normal random choice
		if (RandInt(0,2,-1) == 0)
			Compared1 = RandInt(0, Features.Count, -1);
		// choosing item with lowest comparisons, ironing out distribution differences
		else
		{
			var lowest = 1000;
			for (int i = 0; i < Features.Count; i++)
			{
				if (Features[i].Comparisons < lowest)
				{
					lowest = Features[i].Comparisons;
					Compared1 = i;
				}
			}
		}

		var min = Compared1-10;  // 20pos bubble size
		var max = Compared1+10;
		if (min < 0)
			min = 0;
		if (max > Features.Count)
			max = Features.Count;
		Compared2 = RandInt(0, Features.Count, Compared1);

		GetNode<Label>("P/M/R/Compare/Descs/Feature1").Text = Features[Compared1].Content;
		GetNode<Label>("P/M/R/Compare/Descs/Feature2").Text = Features[Compared2].Content;

		int allComparisons = 0;
		foreach (var f in Features)
			allComparisons += f.Comparisons;
		float intendedComparisons = Features.Count * 5;
		GetNode<Label>("P/M/R/Compare/Goal").Text = Convert.ToString(allComparisons) + "/" + Convert.ToString(intendedComparisons) + " comparisons to reach avg of 5.0";

		GetNode<VBoxContainer>("P/M/R/Compare/Benefit").Visible = false;
		GetNode<HBoxContainer>("P/M/R/Compare/Choices").Visible = true;

		GetNode<VBoxContainer>("P/M/R/TheList").Visible = false;
		GetNode<VBoxContainer>("P/M/R/Add").Visible = false;
		GetNode<VBoxContainer>("P/M/R/Module").Visible = false;
		GetNode<VBoxContainer>("P/M/R/Compare").Visible = true;
		GetNode<Button>("P/M/R/Buttons/B4").Text = "Save";
	}
	public void DisplayBenefit()
	{
		DisplayCompare();

		// overriding essentially
		CurrentState = States.BENEFIT;
		GetNode<VBoxContainer>("P/M/R/Compare/Benefit").Visible = true;
		GetNode<HBoxContainer>("P/M/R/Compare/Choices").Visible = false;
		GetNode<HSlider>("P/M/R/Compare/Benefit/BSlider").Value = 50;
		GetNode<HSlider>("P/M/R/Compare/Benefit/CSlider").Value = 50;
	}
	public void DisplayModule()
	{
		CurrentState = States.MODULE;
		
		Dictionary<string,bool> moduleDict = new Dictionary<string, bool>();
		foreach (var f in Features)
		{
			if (moduleDict.ContainsKey(f.Module) == false)
				moduleDict.Add(f.Module, true);
		}
		var options = GetNode<OptionButton>("P/M/R/Module/ModChoice");
		if (CurrentModule == null)
		{
			CurrentModule = moduleDict.Keys.ToList()[0];
			options.Clear();
			foreach (var k in moduleDict.Keys.ToList())
			{
				options.AddItem(k);
				if (k == CurrentModule)
					options.Selected = options.Items.Count-1;
			}
		}
		else
		{
			CurrentModule = moduleDict.Keys.ToList()[options.Selected];
		}

		for (int i = 0; i < 10; i++)
			GetNode<HBoxContainer>("P/M/R/Module/List/Item" + Convert.ToString(i)).Visible = false;
		int box = 0;
		for (int i = 0; i < Features.Count; i++)
		{
			if (Features[i].Module != CurrentModule)
				continue;
			GetNode<Label>("P/M/R/Module/List/Item" + Convert.ToString(box) + "/Label").Text = Convert.ToString(i+1) + ". " + Features[i].Content;
			GetNode<HBoxContainer>("P/M/R/Module/List/Item" + Convert.ToString(box)).Visible = true;
			box += 1;
			if (box > 9)
				break;
		}

		GetNode<VBoxContainer>("P/M/R/Add").Visible = false;
		GetNode<VBoxContainer>("P/M/R/Compare").Visible = false;
		GetNode<VBoxContainer>("P/M/R/TheList").Visible = false;
		GetNode<VBoxContainer>("P/M/R/Module").Visible = true;
	}

	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Adding control
	public void Add()
	{
		var module = GetNode<LineEdit>("P/M/R/Add/Module").Text;
		var content = GetNode<LineEdit>("P/M/R/Add/Feature").Text;
		var pos = GetNode<HSlider>("P/M/R/Add/InitialPos").Value;  // from 0 to 10
		int actualPos = (int)Math.Round(Features.Count * (pos*0.1f));
		if (actualPos >= Features.Count && actualPos > 0)
			actualPos = Features.Count-1;
		
		Features.Insert(actualPos, new Feature { Module = module, Content = content, Comparisons = 0 });

		GetNode<Label>("P/M/R/Add/Message").Text = "Added '" + content + "' at position " + Convert.ToInt16(actualPos+1) + ".";
		GetNode<Label>("P/M/R/Add/KnownModules").Text = "Known Modules: " + ReturnKnownModules();
		GetNode<Button>("P/M/R/Buttons/B4").Text = "Save";

		CurrentModule = null;
	}

	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Comparing control
	public override void _Input(InputEvent inputEvent)
	{
		if (CurrentState == States.COMPARE)
		{
			if (inputEvent.IsActionPressed("compare_left"))
				CompareDecided(Left:true);
			else if (inputEvent.IsActionPressed("compare_right"))
				CompareDecided(Left:false);
		}
	}
	public void CompareDecided(bool Left)
	{
		Features[Compared1].Comparisons += 1;
		Features[Compared2].Comparisons += 1;

		if (GetNode<CheckBox>("P/M/R/Compare/Config/Swap").Pressed == true)
		{
			if (Left == true && Compared1 < Compared2)
			{
				GetNode<Label>("P/M/R/Compare/Message").Text = "Positions stay the same\n";
			}
			else if (Left == false && Compared1 > Compared2)
			{
				GetNode<Label>("P/M/R/Compare/Message").Text = "Positions stay the same\n";
			}
			else
			{
				GetNode<Label>("P/M/R/Compare/Message").Text = "'" + Features[Compared1].Content + "' moved to " + Convert.ToString(Compared2) + "\n'" + Features[Compared2].Content + "' moved to " + Convert.ToString(Compared1);
				var refToFirstInstance = Features[Compared1];
				Features[Compared1] = Features[Compared2];
				Features[Compared2] = refToFirstInstance;
			}
		}
		else if (GetNode<CheckBox>("P/M/R/Compare/Config/Promote").Pressed == true)
		{
			if (Left == true && Compared1 < Compared2)
			{
				GetNode<Label>("P/M/R/Compare/Message").Text = "Positions stay the same\n";
			}
			else if (Left == false && Compared1 > Compared2)
			{
				GetNode<Label>("P/M/R/Compare/Message").Text = "Positions stay the same\n";
			}
			else if (Left == true)
			{
				int newPos = (int)Math.Round((Compared1-Compared2) * 0.5f);
				if (newPos == Compared1 && newPos > 0)
					newPos -= 1;
				GetNode<Label>("P/M/R/Compare/Message").Text = "'" + Features[Compared1].Content + "' moved to " + Convert.ToString(newPos) + "\n";
				var refToFirstInstance = Features[Compared1];
				Features.RemoveAt(Compared1);
				if (newPos > Compared1)
					newPos -= 1;
				Features.Insert(newPos, refToFirstInstance);
			}
			else // right
			{
				int newPos = (int)Math.Round((Compared2-Compared1) * 0.5f);
				if (newPos == Compared2 && newPos > 0)
					newPos -= 1;
				GetNode<Label>("P/M/R/Compare/Message").Text = "'" + Features[Compared2].Content + "' moved to " + Convert.ToString(newPos) + "\n";
				var refToFirstInstance = Features[Compared2];
				Features.RemoveAt(Compared2);
				if (newPos > Compared2)
					newPos -= 1;
				Features.Insert(newPos, refToFirstInstance);
			}
		}
		else if (GetNode<CheckBox>("P/M/R/Compare/Config/Demote").Pressed == true)
		{
			if (Left == true && Compared1 < Compared2)
			{
				GetNode<Label>("P/M/R/Compare/Message").Text = "Positions stay the same\n";
			}
			else if (Left == false && Compared1 > Compared2)
			{
				GetNode<Label>("P/M/R/Compare/Message").Text = "Positions stay the same\n";
			}
			else if (Left == true)
			{
				int newPos = (int)Math.Round((Compared1-Compared2) * 0.5f);
				if (newPos == Compared1 && newPos > 0)
					newPos -= 1;
				GetNode<Label>("P/M/R/Compare/Message").Text = "'" + Features[Compared2].Content + "' moved to " + Convert.ToString(newPos) + "\n";
				var refToFirstInstance = Features[Compared2];
				Features.RemoveAt(Compared2);
				if (newPos > Compared2)
					newPos -= 1;
				Features.Insert(newPos, refToFirstInstance);
			}
			else // right
			{
				int newPos = (int)Math.Round((Compared2-Compared1) * 0.5f);
				if (newPos == Compared2 && newPos > 0)
					newPos -= 1;
				GetNode<Label>("P/M/R/Compare/Message").Text = "'" + Features[Compared1].Content + "' moved to " + Convert.ToString(newPos) + "\n";
				var refToFirstInstance = Features[Compared1];
				Features.RemoveAt(Compared1);
				if (newPos > Compared1)
					newPos -= 1;
				Features.Insert(newPos, refToFirstInstance);
			}
		}
		
		if (CurrentState == States.COMPARE)
			DisplayCompare();
		else if (CurrentState == States.BENEFIT)
			DisplayBenefit();
	}
	public void FinishBenefit()
	{
		// larger ratio wins
		var benefitVal = GetNode<HSlider>("P/M/R/Compare/Benefit/BSlider").Value;
		var costVal = GetNode<HSlider>("P/M/R/Compare/Benefit/BSlider").Value;

		var leftBenefit = 101f-benefitVal;
		var leftCost = costVal;
		var leftRatio = (leftBenefit*1.0f) /  (leftCost*1.0f);

		var rightBenefit = benefitVal;
		var rightCost = 101f-costVal;
		var rightRatio = (rightBenefit*1.0f) / (rightCost*1.0f);

		if (leftRatio > rightRatio)
			CompareDecided(Left: true);
		else
			CompareDecided(Left: false);
	}

	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Controlling sorting lists
	public void TopListUp(int Which)
	{
		// assummes 0-10 pos corresponding to the list
		if (Which == 0)
			return;
		var refToFirstInstance = Features[Which];
		Features.RemoveAt(Which);
		Features.Insert(Which-1, refToFirstInstance);
		DisplayDescription();
	}
	public void TopListDown(int Which)
	{
		// assummes 0-10 pos corresponding to the list
		if (Which == Features.Count-1)
			return;
		var refToFirstInstance = Features[Which];
		Features.RemoveAt(Which);
		Features.Insert(Which+1, refToFirstInstance);
		DisplayDescription();
	}
	public void _on_ModChoice_item_selected(int Which)
	{
		DisplayModule();
	}
	public void ModListUp(int Which)
	{
		// replaces higher and lower items
		if (Which == 0)
			return;
		
		List<int> positions = new List<int>();
		for (int i = 0; i < Features.Count; i++)
		{
			if (Features[i].Module != CurrentModule)
				continue;
			positions.Add(i);
		}

		var refToFirstInstance = Features[positions[Which]];
		Features[positions[Which]] = Features[positions[Which-1]];
		Features[positions[Which-1]] = refToFirstInstance;
		DisplayModule();
	}
	public void ModListDown(int Which)
	{
		// replaces higher and lower items
		List<int> positions = new List<int>();
		for (int i = 0; i < Features.Count; i++)
		{
			if (Features[i].Module != CurrentModule)
				continue;
			positions.Add(i);
		}
		
		if (Which == positions.Count-1)
			return;

		var refToFirstInstance = Features[positions[Which]];
		Features[positions[Which]] = Features[positions[Which+1]];
		Features[positions[Which+1]] = refToFirstInstance;
		DisplayModule();
	}

	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Helper funcs
	public string ReturnKnownModules()
	{
		Dictionary<string, bool> dict = new Dictionary<string, bool>();
		foreach (var f in Features)
		{
			if (dict.ContainsKey(f.Module) == false)
				dict.Add(f.Module, true);
		}

		return string.Join(", ", dict.Keys.ToList());
	}
	public int RandInt(int Min, int Max, int Exclude)
	{
		var found = Min;
		for (int i = 0; i < 10; i++)  // max ten approaches
		{
            if (Min <= Max)
                found = seed.Next(Min, Max);
            else
                found = seed.Next(Max, Min);
			if (found != Exclude)
				break;
		}
		return found;
	}
}
