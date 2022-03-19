using Exiled.API.Features;
using Hints;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PlayerXP
{
	partial class EventHandler
	{
		private const int baseXP = 1000;
		private Random rand = new Random();

		private void SendHint(Player player, string msg, float time = 3f)
		{
			player.HintDisplay.Show(new TextHint(msg, new HintParameter[] { new StringHintParameter("") }, HintEffectPresets.FadeInAndOut(0.25f), time)); // Use basegame hint instead of ShowHint, no idea why.
		}

		/// <summary>
		/// Adds a specific amount of XP (<paramref name="xp"/>) to a <see cref="Player"/>'s UserID.
		/// </summary>
		/// <param name="userid">The <see cref="Player"/>'s UserID.</param>
		/// <param name="xp">How much XP to add.</param>
		/// <param name="msg">The message shown to the player</param>
		/// <param name="karmaOverride">If the karma to add will be able to overflow maybe? Idfk</param>
		internal void AddXP(string userid, int xp, string msg = null, float karmaOverride = -1f)
		{
			if (pInfoDict.TryGetValue(userid, out var info))
			{
				Player player = Player.Get(userid); // Gets the player

				AdjustKarma(player, karmaOverride == -1f ? PlayerXP.instance.Config.KarmaGainedOnGoodDeed : karmaOverride); // Adjusts the player's karma, for a good deed done.
				
				info.XP += (int)(xp * PlayerXP.instance.Config.XpScale * (PlayerXP.instance.Config.KarmaEnabled ? info.Karma : PlayerXP.instance.Config.KarmaInitial)); // Adjusts the player's karma.
				
				if (msg != null) SendHint(player, $"<color={PlayerXP.instance.Config.RemoveXPHintColor}>{msg}</color>"); // If the message is not null, then it will send it to the player.

				int calc = (info.Level - 1) * PlayerXP.instance.Config.XpIncrement + baseXP; // Calculates if the player can level up
				
				if (info.XP >= calc)
				{
					// Level up logic
					info.XP -= calc;
					info.Level++;
					SendHint(player, $"<color={PlayerXP.instance.Config.LevelUpHintColor}><b>You've leveled up to level {info.Level}! You need {calc + PlayerXP.instance.Config.XpIncrement - info.XP} xp for your next level.</b></color>", 4f);
				}
				
				// Assigns the dict to a new one.
				pInfoDict[userid] = info;
			}
			Log.Debug($"Giving {xp}xp to {Player.Get(userid).Nickname} ({userid}).", PlayerXP.instance.Config.IsDebug);
		}

		internal void RemoveXP(string userid, int xp, string msg = null)
		{
			if (pInfoDict.TryGetValue(userid, out var info))
			{
				Player player = Player.Get(userid);
				
				info.XP -= xp;
				if (msg != null) SendHint(player, $"<color={PlayerXP.instance.Config.RemoveXPHintColor}>{msg}</color>", 2f);
				
				if (info.XP <= 0)
				{
					if (info.Level > 1)
					{
						pInfoDict[userid].Level--;
						pInfoDict[userid].XP = info.Level * PlayerXP.instance.Config.XpIncrement + baseXP - Math.Abs(info.XP);
					}
					else
					{
						pInfoDict[userid].Level = 0;
					}
				}
				pInfoDict[userid] = info;
			}

			Log.Debug($"Removing {xp}XP from {Player.Get(userid).Nickname} ({userid}).", PlayerXP.instance.Config.IsDebug);
		}

		internal float CheckValues(float min, float max, float value)
		{
			if (value < min)
				return min;

			if (value > max)
				return max;
			
			return value;
		}

		internal void AdjustKarma(Player player, float amount, bool canOverflow = false)
		{
			if (PlayerXP.instance.Config.KarmaEnabled && pInfoDict.TryGetValue(player.UserId, out var info))
			{
				float final = info.Karma += amount;
				float calcValue = CheckValues(PlayerXP.instance.Config.KarmaMinimum,
					PlayerXP.instance.Config.KarmaMaximum, final);

				if (calcValue == PlayerXP.instance.Config.KarmaMaximum && canOverflow)
				{
					if (final > PlayerXP.instance.Config.KarmaMaximumOverflow)
					{
						pInfoDict[player.UserId].Karma = PlayerXP.instance.Config.KarmaMaximumOverflow;
						return;
					}

					pInfoDict[player.UserId].Karma = final;
					return;
				}

				pInfoDict[player.UserId].Karma = calcValue;

			}
		}

		internal int GetLevel(string userid)
		{
			if (pInfoDict.TryGetValue(userid, out var info))
			{
				return info.Level;
			}
			return -1;
		}

		internal int GetXP(string userid)
		{
			if (pInfoDict.ContainsKey(userid))
			{
				return pInfoDict[userid].XP;
			}
			return -1;
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
				return (info.Level - 1) * PlayerXP.instance.Config.XpIncrement + baseXP + PlayerXP.instance.Config.XpIncrement;
			}
			return -1;
		}

		private void UpdateCache()
		{
			foreach (FileInfo file in new DirectoryInfo(PlayerXP.XPPath).GetFiles())
			{
				PlayerInfo info = JsonConvert.DeserializeObject<PlayerInfo>(File.ReadAllText(file.FullName));
				if (info.Level == 1 && info.XP == 0)
				{
					File.Delete(file.FullName);
					continue;
				}
				string userid = file.Name.Replace(".json", "");
				
				Log.Debug($"Loading cached stats for {info.Name} ({userid})...", PlayerXP.instance.Config.IsDebug);
				pInfoDict.Add(userid, info);
			}
			pInfoDict = pInfoDict.OrderByDescending(x => x.Value.Level).ThenByDescending(x => x.Value.XP).ToDictionary(x => x.Key, x => x.Value);
		}

		//private bool IsUnarmed(Player player)
		//{
		//	if (!ev.Player.Items.Any(i => i.id.IsWeapon() || i.id.IsThrowable()))
		//		return true;
		//	else
		//		return false;
		//}

		private Player FindEligibleClassd()
		{
			Player bestPlayer = null;
			float highestKarma = PlayerXP.instance.Config.KarmaLabeledBadActor;
			foreach (Player player in Player.List.Where(x => x.Team == Team.CDP).OrderBy(c => rand.Next()))
			{
				if (pInfoDict.TryGetValue(player.UserId, out var playerInfo))
				{
					if (playerInfo.Karma >= PlayerXP.instance.Config.KarmaLabeledBadActor)
						return player;

					if (playerInfo.Karma > highestKarma)
					{
						bestPlayer = player;
						highestKarma = playerInfo.Karma;
					}
				}
			}
			return bestPlayer;
		}

		private int CalcXP(Player player, int xp)
		{
			return (int)(xp * PlayerXP.instance.Config.XpScale * (PlayerXP.instance.Config.KarmaEnabled ? pInfoDict.ContainsKey(player.UserId) ? pInfoDict[player.UserId].Karma : 1f : PlayerXP.instance.Config.KarmaInitial));
		}
	}
}