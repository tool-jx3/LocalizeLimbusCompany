using BattleUI;
using BattleUI.Typo;
using HarmonyLib;
using MainUI;
using TMPro;
using UnityEngine.UI;
using UnityEngine;
using Il2CppSystem.Collections.Generic;
using System.IO;
using System.Linq;

namespace LimbusLocalize.LLC;

public static class SpriteLoader
{
	public static Dictionary<string, Sprite> ReadmeSprites = new() ;
	static SpriteLoader()
	{
		InitReadmeSprites();
	}
	private static void InitReadmeSprites()
	{
		try{
			string spritepath = Path.Combine(LLCMod.ModPath,"Localize/Utils");
			LLCMod.LogInfo($"Loading Sprites from {LLCMod.ModPath}");
		
			foreach (var file in new DirectoryInfo(spritepath).GetFiles().Where(f => f.Extension is ".jpg" or ".png"))
			{
				Texture2D texture = new(2, 2);
				ImageConversion.LoadImage(texture, File.ReadAllBytes(file.FullName));
				var sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
					new Vector2(0.5f, 0.5f));
				string spriteName = Path.GetFileNameWithoutExtension(file.Name);
				texture.name = spriteName;
				sprite.name = spriteName;
				Object.DontDestroyOnLoad(sprite);
				sprite.hideFlags |= HideFlags.HideAndDontSave;
				ReadmeSprites[spriteName] = sprite;
			}	
		}catch(DirectoryNotFoundException ex){
			LLCMod.LogError($"DirectoryMissing: {ex.Message}");
		}catch(IOException ex){
			LLCMod.LogError($"FileRead Error: {ex.Message}");
		}
		
	}
}
public static class UIImproved
{
	[HarmonyPatch(typeof(ParryingTypoUI), nameof(ParryingTypoUI.SetParryingTypoData))]
	[HarmonyPrefix]
	private static void ParryingTypoUI_SetParryingTypoData(ParryingTypoUI __instance)
	{
		if (SpriteLoader.ReadmeSprites.TryGetValue("LLC_Combo",out Sprite combo))
		{
			__instance.img_parryingTypo.sprite = combo;
		}
	}

	[HarmonyPatch(typeof(ActBossBattleStartUI), nameof(ActBossBattleStartUI.Init))]
	[HarmonyPostfix]
	private static void BossBattleStartInit(ActBossBattleStartUI __instance)
	{
		var textGroup = __instance.transform.GetChild(2).GetChild(1);
		var tmp = textGroup.GetChild(1).GetComponentInChildren<TextMeshProUGUI>();
		if (!tmp.text.Equals("Proelium Fatale"))
			return;
		if (SpriteLoader.ReadmeSprites.TryGetValue("LLC_BossBattle",out Sprite BossBattle))
		{
			textGroup.GetChild(1).GetComponentInChildren<Image>().sprite = BossBattle;
		}
		tmp.font = ChineseFont.Tmpchinesefonts[1];
		tmp.text = "<b>命定之戰</b>";
		tmp = textGroup.GetChild(2).GetComponentInChildren<TextMeshProUGUI>();
		tmp.text = "凡跨入此門之人，當放棄一切希望";
		if (!tmp.font.fallbackFontAssetTable.Contains(ChineseFont.Tmpchinesefonts[1]))
			tmp.font.fallbackFontAssetTable.Add(ChineseFont.Tmpchinesefonts[1]);
	}

	[HarmonyPatch(typeof(StageChapterAreaSlot), "Init")]
	[HarmonyPostfix]
	private static void AreaSlotInit(StageChapterAreaSlot __instance)
	{
		var tmproArea = __instance.tmpro_area;
		if (!tmproArea.text.StartsWith("DISTRICT ")) return;
		if (!tmproArea.font.fallbackFontAssetTable.Contains(ChineseFont.Tmpchinesefonts[1]))
			tmproArea.font.fallbackFontAssetTable.Add(ChineseFont.Tmpchinesefonts[1]);
		tmproArea.text = tmproArea.text.Replace("DISTRICT ", "") + "<size=25>區";
	}

	[HarmonyPatch(typeof(FormationPersonalityUI_Label), "Reload")]
	[HarmonyPostfix]
	private static void PersonalityUILabel(FormationPersonalityUI_Label __instance)
	{
		switch (__instance._model._status)
		{
			case FormationPersonalityUI_LabelTypes.Changed:
				__instance.tmp_text.text = "<size=45>已更改";
				break;
		}
	}
}