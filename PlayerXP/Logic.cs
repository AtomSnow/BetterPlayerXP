using Exiled.API.Features;
using Hints;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandSystem;
using Exiled.Events.EventArgs;

namespace PlayerXP
{
	partial class EventHandler
	{
		private const int baseXP = 1000;
		private System.Random rand = new System.Random();

		private void SendHint(Player player, string msg, float time = 3f)
		{
			player.HintDisplay.Show(new TextHint(msg, new HintParameter[] { new StringHintParameter("") }, HintEffectPresets.FadeInAndOut(0.25f, 1f, 0f), time));
		}

		internal void AddXP(string userid, int xp, string msg = null, float karmaOverride = -1f)
		{
			if (pInfoDict.ContainsKey(userid))
			{
				PlayerInfo info = pInfoDict[userid];
				Player player = Player.Get(userid);
				AdjustKarma(player, karmaOverride == -1f ? PlayerXP.instance.Config.KarmaGainedOnGoodDeed : karmaOverride);
				info.xp += (int)(xp * PlayerXP.instance.Config.XpScale * (PlayerXP.instance.Config.KarmaEnabled ? info.karma : PlayerXP.instance.Config.KarmaInitial));
				if (msg != null) SendHint(player, $"<color=\"yellow\">{msg}</color>");
				int calc = (info.level - 1) * PlayerXP.instance.Config.XpIncrement + baseXP;
				if (info.xp >= calc)
				{
					info.xp -= calc;
					info.level++;
					SendHint(player, $"<color=\"yellow\"><b>You've leveled up to level {info.level}! You need {calc + PlayerXP.instance.Config.XpIncrement - info.xp} xp for your next level.</b></color>", 4f);
				}
				pInfoDict[userid] = info;
			}
			if (PlayerXP.instance.Config.IsDebug) Log.Info($"Giving {xp}xp to {Player.Get(userid).Nickname} ({userid}).");
		}

		internal void RemoveXP(string userid, int xp, string msg = null)
		{
			if (pInfoDict.ContainsKey(userid))
			{
				PlayerInfo info = pInfoDict[userid];
				Player player = Player.Get(userid);
				info.xp -= xp;
				if (msg != null) SendHint(player, $"<color=\"yellow\">{msg}</color>", 2f);
				if (info.xp <= 0)
				{
					if (info.level > 1)
					{
						info.level--;
						info.xp = info.level * PlayerXP.instance.Config.XpIncrement + baseXP - Math.Abs(info.xp);
					}
					else
					{
						info.xp = 0;
					}
				}
				pInfoDict[userid] = info;
			}

			if (PlayerXP.instance.Config.IsDebug) Log.Info($"Removing {xp}xp from {Player.Get(userid).Nickname} ({userid}).");
		}

		internal void AdjustKarma(Player player, float amount, bool canOverflow = false)
		{
			if (PlayerXP.instance.Config.KarmaEnabled && pInfoDict.ContainsKey(player.UserId))
			{
				float final = pInfoDict[player.UserId].karma += amount;
				if (final > PlayerXP.instance.Config.KarmaMaximum)
				{
					if (canOverflow)
					{
						if (final > PlayerXP.instance.Config.KarmaMaximumOverflow)
						{
							pInfoDict[player.UserId].karma = PlayerXP.instance.Config.KarmaMaximumOverflow;
						}
						else
						{
							pInfoDict[player.UserId].karma = final;
						}
					}
					else
					{
						pInfoDict[player.UserId].karma = PlayerXP.instance.Config.KarmaMaximum;
					}
				}
				else if (final < PlayerXP.instance.Config.KarmaMinimum)
				{
					pInfoDict[player.UserId].karma = PlayerXP.instance.Config.KarmaMinimum;
				}
				else
				{
					pInfoDict[player.UserId].karma = final;
				}
			}
	        }
		internal int GetLevel(string userid)
		{
			if (pInfoDict.ContainsKey(userid))
			{
				return pInfoDict[userid].level;
			}
			else return -1;
		}

		internal int GetXP(string userid)
		{
			if (pInfoDict.ContainsKey(userid))
			{
				return pInfoDict[userid].xp;
			}
			else return -1;
		}

		private void SaveStats()
		{
			if (PlayerXP.instance.Config.IsDebug) Log.Info($"Saving stats for a total of {pInfoDict.Count} players.");
			foreach (KeyValuePair<string, PlayerInfo> info in pInfoDict)
			{
				if (PlayerXP.instance.Config.IsDebug) Log.Info($"Saving stats for {info.Key}...");
				File.WriteAllText(Path.Combine(PlayerXP.XPPath, $"{info.Key}.json"), JsonConvert.SerializeObject(info.Value, Formatting.Indented));
			}
		}

		internal int XpToLevelUp(string userid)
		{
			if (pInfoDict.ContainsKey(userid))
			{
				PlayerInfo info = pInfoDict[userid];
				return (info.level - 1) * PlayerXP.instance.Config.XpIncrement + baseXP + PlayerXP.instance.Config.XpIncrement;
			}
			else return -1;
		}

		private void UpdateCache()
		{
			foreach (FileInfo file in new DirectoryInfo(PlayerXP.XPPath).GetFiles())
			{
				PlayerInfo info = JsonConvert.DeserializeObject<PlayerInfo>(File.ReadAllText(file.FullName));
				if (info.level == 1 && info.xp == 0)
				{
					File.Delete(file.FullName);
					continue;
				}
				string userid = file.Name.Replace(".json", "");
				if (PlayerXP.instance.Config.IsDebug) Log.Info($"Loading cached stats for {info.name} ({userid})...");
				pInfoDict.Add(userid, info);
			}
			pInfoDict = pInfoDict.OrderByDescending(x => x.Value.level).ThenByDescending(x => x.Value.xp).ToDictionary(x => x.Key, x => x.Value);
		}

		//private bool IsUnarmed(Player player)
		//{
		//	foreach (var fuck in Item.IsWeapon)
		//	{
		//		if (fuck.id == ItemType.GunCrossvec || Item .IsWeapon.id == ItemType.GunCOM15 ||
		//			fuck.id == ItemType.GunE11SR || fuck.id == ItemType.GunLogicer ||
		//			fuck.id == ItemType.GunRevolver || fuck.id == ItemType.GunAK ||
		//			fuck.id == ItemType.GunShotgun || fuck.id == ItemType.MicroHID ||
		//			fuck.id == ItemType.GrenadeHE || fuck.id == ItemType.GrenadeFlash ||
		//			fuck.id == ItemType.SCP018) return false;
		//	}
		//	return true;
		//}

		private Player FindEligibleClassd()
		{
			Player bestPlayer = null;
			float highestKarma = PlayerXP.instance.Config.KarmaLabeledBadActor;
			foreach (Player player in Player.List.Where(x => x.Team == Team.CDP).OrderBy(c => rand.Next()))
			{
				if (pInfoDict.ContainsKey(player.UserId))
				{
					if (pInfoDict[player.UserId].karma >= PlayerXP.instance.Config.KarmaLabeledBadActor)
					{
						return player;
					}
					else if (pInfoDict[player.UserId].karma > highestKarma)
					{
						bestPlayer = player;
						highestKarma = pInfoDict[player.UserId].karma;
					}
				}
			}
			return bestPlayer;
		}

		private int CalcXP(Player player, int xp)
		{
			return (int)(xp * PlayerXP.instance.Config.XpScale * (PlayerXP.instance.Config.KarmaEnabled ? pInfoDict.ContainsKey(player.UserId) ? pInfoDict[player.UserId].karma : 1f : PlayerXP.instance.Config.KarmaInitial));
		}
	}
}
