using Exiled.API.Features;
using Hints;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandSystem;
using Exiled.API.Features.Items;
using RemoteAdmin;
using Exiled.Events.EventArgs;

namespace PlayerXP
{
	partial class EventHandler
	{
		public static Dictionary<string, PlayerInfo> pInfoDict = new Dictionary<string, PlayerInfo>();
		public static Dictionary<Player, Player> pCuffedDict = new Dictionary<Player, Player>();

		private bool isToggled = true;

		public void OnVerified(VerifiedEventArgs ev)
		{
			if (!File.Exists(Path.Combine(PlayerXP.XPPath, $"{ev.Player.UserId}.json"))) pInfoDict.Add(ev.Player.UserId, new PlayerInfo(ev.Player.Nickname));
			else if (ev.Player.UserId == null) Log.Debug($"Verified Player returned as null.", PlayerXP.instance.Config.IsDebug);
		}

		public void OnWaitingForPlayers()
		{
			pCuffedDict.Clear();
			pInfoDict.Clear();
			UpdateCache();
		}

		public void OnRoundStart()
		{
			foreach (Player player in Player.List.Where(x => x.Team == Team.SCP))
			{
				if (pInfoDict.ContainsKey(player.UserId))
				{
					if (pInfoDict[player.UserId].Karma < PlayerXP.instance.Config.KarmaLabeledBadActor)
					{
						Player swap = FindEligibleClassd();
						swap.SetRole(player.Role);
						player.SetRole(RoleType.ClassD);
						break;
					}
				}
			}
		}

		public void OnRoundEnd(RoundEndedEventArgs ev)
		{
			if (isToggled && PlayerXP.instance.Config.RoundWin > 0)
			{
				foreach (Player player in Player.List)
				{
					if (player.Team != Team.RIP && Round.IsStarted)
					{
						int xp = CalcXP(player, PlayerXP.instance.Config.RoundWin);
						AddXP(player.UserId, xp);
						//AddXP(player.UserId, PlayerXP.instance.Config.RoundWin, $"You have gained {PlayerXP.instance.Config.RoundWin} xp for winning the round!");
					}
				}
			}
		}

		public void OnRoundRestart()
		{
			SaveStats();
		}

		public void OnPlayerDying(DyingEventArgs ev)
		{
			if (!isToggled || !Round.IsStarted) return;

			if (ev.Killer.Team == ev.Target.Team && ev.Killer.UserId != ev.Target.UserId && PlayerXP.instance.Config.TeamKillPunishment > 0)
			{
				int xp = CalcXP(ev.Killer, PlayerXP.instance.Config.TeamKillPunishment);
				RemoveXP(ev.Killer.UserId, xp, PlayerXP.instance.Config.PlayerTeamkillMessage.Replace("{xp}", xp.ToString()).Replace("{target}", ev.Target.Nickname));
				// Player teamkilled.
			}

			
			
			if (ev.Killer.Team == Team.CDP)
			{
				int gainedXP = 0;
				if (ev.Target.Team == Team.RSC)
				{
					gainedXP = PlayerXP.instance.Config.DclassScientistKill;
					//isUnarmed = IsUnarmed(ev.Target);
				}
				if (ev.Target.Team == Team.MTF) gainedXP = PlayerXP.instance.Config.DclassMtfKill;
				if (ev.Target.Team == Team.SCP) gainedXP = PlayerXP.instance.Config.DclassScpKill;
				if (ev.Target.Team == Team.TUT) gainedXP = PlayerXP.instance.Config.DclassTutorialKill;

				if (gainedXP > 0 && ev.Target.UserId != ev.Killer.UserId)
				{
					int xp = CalcXP(ev.Killer, gainedXP);
					AddXP(ev.Killer.UserId, xp, PlayerXP.instance.Config.PlayerKillMessage.Replace("{xp}", xp.ToString()).Replace("{target}", ev.Target.Nickname));
					// , isUnarmed ? -PlayerXP.instance.Config.KarmaLostOnDefenselessKill : -1f
				}
			}
			else if (ev.Killer.Team == Team.RSC)
			{
				int gainedXP = 0;
				bool isUnarmed = false;
				if (ev.Target.Team == Team.CDP)
				{
					gainedXP = PlayerXP.instance.Config.ScientistDclassKill;
					//isUnarmed = IsUnarmed(ev.Target);
				}
				if (ev.Target.Team == Team.CHI) gainedXP = PlayerXP.instance.Config.ScientistChaosKill;
				if (ev.Target.Team == Team.SCP) gainedXP = PlayerXP.instance.Config.ScientistScpKill;
				if (ev.Target.Team == Team.TUT) gainedXP = PlayerXP.instance.Config.ScientistTutorialKill;

				if (gainedXP > 0 && ev.Target.UserId != ev.Killer.UserId)
				{
					int xp = CalcXP(ev.Killer, gainedXP);
					AddXP(ev.Killer.UserId, xp, PlayerXP.instance.Config.PlayerKillMessage.Replace("{xp}", xp.ToString()).Replace("{target}", ev.Target.Nickname), isUnarmed ? -PlayerXP.instance.Config.KarmaLostOnDefenselessKill : -1f);
				}
			}
			else if (ev.Killer.Team == Team.MTF)
			{
				int gainedXP = 0;
				if (ev.Target.Team == Team.CDP) gainedXP = PlayerXP.instance.Config.MtfDclassKill;
				if (ev.Target.Team == Team.CHI) gainedXP = PlayerXP.instance.Config.MtfChaosKill;
				if (ev.Target.Team == Team.SCP) gainedXP = PlayerXP.instance.Config.MtfScpKill;
				if (ev.Target.Team == Team.TUT) gainedXP = PlayerXP.instance.Config.MtfTutorialKill;

				if (gainedXP > 0 && ev.Target.UserId != ev.Killer.UserId)
				{
					int xp = CalcXP(ev.Killer, gainedXP);
					AddXP(ev.Killer.UserId, xp, PlayerXP.instance.Config.PlayerKillMessage.Replace("{xp}", xp.ToString()).Replace("{target}", ev.Target.Nickname));
				}
			}
			else if (ev.Killer.Team == Team.CHI)
			{
				int gainedXP = 0;
				if (ev.Target.Team == Team.RSC) gainedXP = PlayerXP.instance.Config.ChaosScientistKill;
				if (ev.Target.Team == Team.MTF) gainedXP = PlayerXP.instance.Config.ChaosMtfKill;
				if (ev.Target.Team == Team.SCP) gainedXP = PlayerXP.instance.Config.ChaosScpKill;
				if (ev.Target.Team == Team.TUT) gainedXP = PlayerXP.instance.Config.ChaosTutorialKill;

				if (gainedXP > 0 && ev.Target.UserId != ev.Killer.UserId)
				{
					int xp = CalcXP(ev.Killer, gainedXP);
					AddXP(ev.Killer.UserId, xp, PlayerXP.instance.Config.PlayerKillMessage.Replace("{xp}", xp.ToString()).Replace("{target}", ev.Target.Nickname));
				}
			}
			else if (ev.Killer.Team == Team.TUT)
			{
				int gainedXP = 0;
				if (ev.Target.Team == Team.CDP) gainedXP = PlayerXP.instance.Config.TutorialDclassKill;
				if (ev.Target.Team == Team.RSC) gainedXP = PlayerXP.instance.Config.TutorialScientistKill;
				if (ev.Target.Team == Team.MTF) gainedXP = PlayerXP.instance.Config.TutorialMtfKill;
				if (ev.Target.Team == Team.CHI) gainedXP = PlayerXP.instance.Config.TutorialChaosKill;

				if (gainedXP > 0 && ev.Target.UserId != ev.Killer.UserId)
				{
					int xp = CalcXP(ev.Killer, gainedXP);
					AddXP(ev.Killer.UserId, xp, PlayerXP.instance.Config.PlayerKillMessage.Replace("{xp}", xp.ToString()).Replace("{target}", ev.Target.Nickname));
				}
			}
			else if (ev.Killer.Team == Team.SCP)
			{
				int gainedXP = 0;
				if (ev.Target.UserId != ev.Killer.UserId)
				{
					if (ev.Killer.Role == RoleType.Scp049) gainedXP = PlayerXP.instance.Config.Scp049Kill;
					else if (ev.Killer.Role == RoleType.Scp0492) gainedXP = PlayerXP.instance.Config.Scp0492Kill;
					else if (ev.Killer.Role == RoleType.Scp096) gainedXP = PlayerXP.instance.Config.Scp096Kill;
					else if (ev.Killer.Role == RoleType.Scp106) gainedXP = PlayerXP.instance.Config.Scp106Kill;
					else if (ev.Killer.Role == RoleType.Scp173) gainedXP = PlayerXP.instance.Config.Scp173Kill;
					else if (ev.Killer.Role == RoleType.Scp93953 || ev.Killer.Role == RoleType.Scp93989) gainedXP = PlayerXP.instance.Config.Scp939Kill;

					if (gainedXP > 0)
					{
						int xp = CalcXP(ev.Killer, gainedXP);
						AddXP(ev.Killer.UserId, xp, PlayerXP.instance.Config.PlayerKillMessage.Replace("{xp}", xp.ToString()).Replace("{target}", ev.Target.Nickname));
					}
				}

			else if (ev.Killer == null) Log.Debug($"Killer Player returned as null.", PlayerXP.instance.Config.IsDebug);

				if (PlayerXP.instance.Config.TutorialScpKillsPlayer > 0 && ev.Target.Team != Team.TUT && ev.Target.UserId != ev.Killer.UserId)
				{
					foreach (Player player in Player.List)
					{
						if (player.Role == RoleType.Tutorial)
						{
							int xp = CalcXP(player, PlayerXP.instance.Config.TutorialScpKillsPlayer);
							if (PlayerXP.instance.Config.IsTutorialAnSCP) AddXP(player.UserId, xp, PlayerXP.instance.Config.TutorialScpKillsPlayerMessage.Replace("{xp}", xp.ToString()).Replace("{target}", ev.Target.Nickname));
						}
					}
				}

				if (PlayerXP.instance.Config.Scp079AssistedKill > 0 && ev.Target.UserId != ev.Killer.UserId && ev.Target.Team != Team.TUT)
				{
					foreach (Player player in Player.List)
					{
						if (player.Role == RoleType.Scp079)
						{
							int xp = CalcXP(player, PlayerXP.instance.Config.Scp079AssistedKill);
							if (PlayerXP.instance.Config.IsTutorialAnSCP) AddXP(player.UserId, xp, PlayerXP.instance.Config.Scp079AssistedKillMessage.Replace("{xp}", xp.ToString()).Replace("{target}", ev.Target.Nickname));
						}
					}
				}
			}

			if (ev.Killer.Id != ev.Target.Id)
			{
				SendHint(ev.Target, PlayerXP.instance.Config.PlayerDeathMessage.Replace("{xp}", GetXP(ev.Killer.UserId).ToString()).Replace("{level}", GetLevel(ev.Killer.UserId).ToString()).Replace("{killer}", ev.Killer.Nickname));
				ev.Target.SendConsoleMessage($"You have {GetXP(ev.Target.UserId)}/{XpToLevelUp(ev.Target.UserId)} xp until you reach level {GetLevel(ev.Target.UserId) + 1}.", "yellow");
			}
		}

		public void OnPocketDimensionDie(FailingEscapePocketDimensionEventArgs ev)
		{
			if (isToggled && Round.IsStarted && PlayerXP.instance.Config.Scp106PocketDeath > 0)
			{
				foreach (Player player in Player.List)
				{
					if (player.Role == RoleType.Scp106 && ev.Player.UserId != player.UserId && player.Team != Team.TUT && player != null && ev.Player != null && this != null)
					{
						int xp = CalcXP(player, PlayerXP.instance.Config.Scp106PocketDeath);
						SendHint(ev.Player, PlayerXP.instance.Config.PlayerDeathMessage.Replace("{xp}", GetXP(player.UserId).ToString()).Replace("{level}", GetLevel(player.UserId).ToString()));	
						AddXP(player.UserId, xp, PlayerXP.instance.Config.Scp106PocketDimensionDeathMessage.Replace("{xp}", xp.ToString()).Replace("{target}", ev.Player.Nickname));
					}
				}
			}
		}

		public void OnRecallZombie(FinishingRecallEventArgs ev)
		{
			if (isToggled && Round.IsStarted && PlayerXP.instance.Config.Scp049ZombieCreated > 0 && ev.Scp049.UserId != ev.Target.UserId)
			{
				int xp = CalcXP(ev.Scp049, PlayerXP.instance.Config.Scp049ZombieCreated);
				AddXP(ev.Scp049.UserId, xp, PlayerXP.instance.Config.Scp049CreateZombieMessage.Replace("{xp}", xp.ToString()).Replace("{target}", ev.Target.Nickname));
			}
		}

		public void OnCheckEscape(EscapingEventArgs ev)
		{
			if (!isToggled || !Round.IsStarted) return;

			if (ev.Player.IsCuffed)
			{
				Player cuffer = ev.Player.Cuffer;
				if (cuffer != null) AdjustKarma(cuffer, PlayerXP.instance.Config.KarmaGainedOnDisarmedEscape, true);
			}

			if (ev.Player.Role == RoleType.ClassD)
			{
				if (PlayerXP.instance.Config.DclassEscape > 0)
				{
					int xp = CalcXP(ev.Player, PlayerXP.instance.Config.DclassEscape);
					AddXP(ev.Player.UserId, xp, PlayerXP.instance.Config.DclassEscapeMessage.Replace("{xp}", xp.ToString()));
				}

				if (PlayerXP.instance.Config.ChaosDclassEscape > 0 && !ev.Player.IsCuffed)
				{
					foreach (Player player in Player.List)
					{
						if (player.Team == Team.CHI)
						{
							int xp = CalcXP(ev.Player, PlayerXP.instance.Config.ChaosDclassEscape);
							AddXP(player.UserId, xp, PlayerXP.instance.Config.ChaosDclassEscapeMessage.Replace("{xp}", xp.ToString()).Replace("{target}", ev.Player.Nickname));
						}
					}
				}
			}

			if (ev.Player.Role == RoleType.Scientist)
			{
				if (PlayerXP.instance.Config.ScientistEscape > 0)
				{
					int xp = CalcXP(ev.Player, PlayerXP.instance.Config.ScientistEscape);
					AddXP(ev.Player.UserId, xp, PlayerXP.instance.Config.ScientistEscapeMessage.Replace("{xp}", xp.ToString()).Replace("{target}", ev.Player.Nickname));
				}

				if (PlayerXP.instance.Config.MtfScientistEscape > 0 && !ev.Player.IsCuffed)
				{
					foreach (Player player in Player.List)
					{
						if (player.Team == Team.MTF)
						{
							int xp = CalcXP(ev.Player, PlayerXP.instance.Config.MtfScientistEscape);
							AddXP(player.UserId, xp, PlayerXP.instance.Config.MtfScientistEscapeMessage.Replace("{xp}", xp.ToString()).Replace("{target}", ev.Player.Nickname));
						}
					}
				}
			}
		}

		public void OnHandcuff(HandcuffingEventArgs ev)
		{
			if (!pCuffedDict.ContainsKey(ev.Cuffer)) pCuffedDict.Add(ev.Cuffer, ev.Target);
			else pCuffedDict[ev.Cuffer] = ev.Target;
		}

		public void OnRemovingHandcuff(RemovingHandcuffsEventArgs ev)
		{
			if (pCuffedDict.ContainsKey(ev.Cuffer) && pCuffedDict[ev.Cuffer] != null) pCuffedDict[ev.Cuffer] = null;
		}
	}
}