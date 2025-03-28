﻿using System;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using MainUI.Gacha;
using TMPro;
using UnityEngine;
using ILObject = Il2CppSystem.Object;
using UObject = UnityEngine.Object;
using ULogger = UnityEngine.Logger;

namespace LimbusLocalize.LLC;

public class Manager(IntPtr ptr) : MonoBehaviour(ptr)
{
	public static Manager Instance;
	public static Dictionary<string, string> Localizes = [];
	public static Action FatalErrorAction;
	public static string FatalErrorlog;

	static Manager()
	{
		ClassInjector.RegisterTypeInIl2Cpp<Manager>();
		GameObject obj = new(nameof(Manager));
		DontDestroyOnLoad(obj);
		obj.hideFlags |= HideFlags.HideAndDontSave;
		Instance = obj.AddComponent<Manager>();
	}

	private void OnApplicationQuit()
	{
		LLCMod.CopyLog();
	}

	public static void OpenGlobalPopup(string description, string title = null, string close = "取消",
		string confirm = "確認", Action confirmEvent = null, Action closeEvent = null)
	{
		if (!GlobalGameManager.Instance) return;
		var globalPopupUI = GlobalGameManager.Instance.globalPopupUI;
		TMP_FontAsset fontAsset = ChineseFont.Tmpchinesefonts[1];
		if (fontAsset)
		{
			var btnCanceltmp = globalPopupUI.btn_cancel.GetComponentInChildren<TextMeshProUGUI>(true);
			btnCanceltmp.font = fontAsset;
			btnCanceltmp.fontMaterial = fontAsset.material;
			var btnCanceltl = globalPopupUI.btn_cancel.GetComponentInChildren<UITextDataLoader>(true);
			btnCanceltl.enabled = false;
			btnCanceltmp.text = close;
			var btnOktmp = globalPopupUI.btn_ok.GetComponentInChildren<TextMeshProUGUI>(true);
			btnOktmp.font = fontAsset;
			btnOktmp.fontMaterial = fontAsset.material;
			var btnOktl = globalPopupUI.btn_ok.GetComponentInChildren<UITextDataLoader>(true);
			btnOktl.enabled = false;
			btnOktmp.text = confirm;
			globalPopupUI.tmp_title.font = fontAsset;
			globalPopupUI.tmp_title.fontMaterial = fontAsset.material;

			confirmEvent += TextLoaderEnabled;
			closeEvent += TextLoaderEnabled;

			void TextLoaderEnabled()
			{
				btnCanceltl.enabled = true;
				btnOktl.enabled = true;
			}
		}

		globalPopupUI._titleObject.SetActive(!string.IsNullOrEmpty(title));
		globalPopupUI.tmp_title.text = title;
		globalPopupUI.tmp_description.text = description;
		globalPopupUI._confirmEvent = confirmEvent;
		globalPopupUI._closeEvent = closeEvent;
		globalPopupUI.btn_cancel.gameObject.SetActive(!string.IsNullOrEmpty(close));
		globalPopupUI._gridLayoutGroup.cellSize = new Vector2(!string.IsNullOrEmpty(close) ? 500 : 700, 100f);
		globalPopupUI.Open();
	}

	public static void InitLocalizes(DirectoryInfo directory)
	{
		foreach (var fileInfo in directory.GetFiles())
		{
			var value = File.ReadAllText(fileInfo.FullName);
			var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileInfo.FullName);
			Localizes[fileNameWithoutExtension] = value;
		}

		foreach (var directoryInfo in directory.GetDirectories()) InitLocalizes(directoryInfo);
	}

	#region 修覆一些弱智東西

	[HarmonyPatch(typeof(GachaEffectEventSystem), nameof(GachaEffectEventSystem.LinkToCrackPosition))]
	[HarmonyPrefix]
	private static bool LinkToCrackPosition(GachaEffectEventSystem __instance)
	{
		return __instance._parent.EffectChainCamera;
	}

	#endregion

	[HarmonyPatch(typeof(LoginSceneManager), nameof(LoginSceneManager.SetLoginInfo))]
	[HarmonyPostfix]
	public static void CheckModActions()
	{
		if (FatalErrorAction != null)
		{
			OpenGlobalPopup(FatalErrorlog, "Mod Has Fatal Error!\n模組存在致命錯誤", null, "Open LLC URL", () =>
			{
				FatalErrorAction.Invoke();
				FatalErrorAction = null;
				FatalErrorlog = string.Empty;
			});
		}
		else if (File.Exists(LLCMod.ModPath + "/UPDATE_TEMP_INFO"))
		{
			var content = File.ReadAllText(LLCMod.ModPath + "/UPDATE_TEMP_INFO");
			OpenGlobalPopup(content, "模組更新完成", "忽略", "好的");
			File.Delete(LLCMod.ModPath + "/UPDATE_TEMP_INFO");
		}
	}

	#region 屏蔽沒有意義的Warning

	[HarmonyPatch(typeof(ULogger), nameof(ULogger.Log), typeof(LogType), typeof(ILObject))]
	[HarmonyPrefix]
	private static bool Log(ULogger __instance, LogType logType, ILObject message)
	{
		if (logType != LogType.Warning) return true;
		var logString = ULogger.GetString(message);
		if (!logString.StartsWith("<color=#0099bc><b>DOTWEEN"))
			__instance.logHandler.LogFormat(logType, null, "{0}", logString);
		return false;
	}

	[HarmonyPatch(typeof(ULogger), nameof(ULogger.Log), typeof(LogType), typeof(ILObject), typeof(UObject))]
	[HarmonyPrefix]
	private static bool Log(ULogger __instance, LogType logType, ILObject message, UObject context)
	{
		if (logType != LogType.Warning) return true;
		var logString = ULogger.GetString(message);
		if (!logString.StartsWith("Material"))
			__instance.logHandler.LogFormat(logType, context, "{0}", logString);
		return false;
	}

	#endregion
}