﻿using System.Collections.Generic;
using System.IO;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace LimbusLocalize.LLC;

public static class LoadingManager
{
	private const string Raw = "<bounce f=0.5>NOW LOADING...</bounce>";
	private static List<string> _loadingTexts = [];
	private static string _touhou;

	public static ConfigEntry<bool> RandomLoadText = LLCMod.LLCSettings.Bind("LLC Settings", "RandomLoadText",
		true, "是否隨機選擇載入標語,即右下角的[NOW LOADING...] ( true | false )");

	static LoadingManager()
	{
		InitLoadingTexts();
	}

	public static void InitLoadingTexts()
	{
		_loadingTexts = [.. File.ReadAllLines(LLCMod.ModPath + "/Localize/Utils/LoadingTexts.md")];
		for (var i = 0; i < _loadingTexts.Count; i++)
		{
			var loadingText = _loadingTexts[i];
			_loadingTexts[i] = "<bounce f=0.5>" + loadingText.Remove(0, 2) + "</bounce>";
		}

		_touhou = _loadingTexts[0];
		_loadingTexts.RemoveAt(0);
	}

	public static T SelectOne<T>(List<T> list)
	{
		return list.Count == 0 ? default : list[Random.Range(0, list.Count)];
	}

	[HarmonyPatch(typeof(LoadingSceneManager), nameof(LoadingSceneManager.Start))]
	[HarmonyPostfix]
	private static void LSM_Start(LoadingSceneManager __instance)
	{
		if (!RandomLoadText.Value)
			return;
		var loadingText = __instance._loadingText;
		loadingText.font = ChineseFont.Tmpchinesefonts[1];
		loadingText.fontSize = 45f;
		var random = Random.Range(0, 100);
		loadingText.text = random switch
		{
			< 25 => Raw,
			< 35 => _touhou,
			_ => SelectOne(_loadingTexts)
		};
	}
}